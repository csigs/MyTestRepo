// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadMetadataCollectionItemsV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's metadata to database in case metadata is a collection.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using FunctionExtensions.DependencyInjection;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Extensions;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Updates payload's metadata collection items
/// </summary>
public static class UpdatePayloadMetadataCollectionItemsV2
{
    private const string OpId = nameof(Constants.UpdatePayloadMetadataCollectionItemsV2);
    private const string FnName = Constants.UpdatePayloadMetadataCollectionItemsV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/metadata";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadMetadataCollection" },
    //    Description = "Updates a metadata collection of a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(JToken), Required = true)]
    //[OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Data of a specified payload are successfully updated.")]
    //[OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Required parameters or request body are not specified.")]
    //[OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route)] HttpRequest req,
        string id,
        string partitionkey,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] ISignProvider signProvider,
        [Inject] IPayloadMetricEmitter metricEmitter,
        ILogger log)
    {
        using var timedOperation = metricEmitter.BeginTimedOperation(FnName);

        log.LogInformation($"{FnName}: HTTP trigger to upload a payload's metadata for payload with partition key {partitionkey} and payload id {id} started at {DateTime.Now.ToUniversalTime()}.");

        if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
        {
            return badRequestErrorMessageResult;
        }

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            log.LogError($"{FnName}: Payload metadata is empty in request body.");

            return new BadRequestObjectResult("Please pass a payload metadata in the request body.");
        }

        try
        {
            if (req.RequireSign())
            {
                req.VerifySignature(signProvider, requestBody);
            }
        }
        catch (Exception e)
        {
            var errorMessage = $"Failed to create new payload.";
            log.LogError($"{FnName}: {errorMessage} {e.Message}");

            return new BadRequestObjectResult($"{errorMessage} {e.Message}");
        }

        var data = JsonConvert.DeserializeObject<JToken>(requestBody);

        if (data.GetType() == typeof(JArray))
        {
            var metadata = data.ToObject<JArray>();

            if (metadata.Any())
            {
                var ids = metadata
                    .Where(m => ((JObject)m).ContainsKey("Id"))
                    .Select(m => ((JObject)m)["Id"].ToString());

                var payloadMetadatas = await payloadMetadataRepository.GetItemsAsync<PayloadMetadataCollectionItem<JObject>>(
                        m => ids.Contains(m.Id), payloadId.ToString());

                var updateDocs = payloadMetadatas
                    .Join(
                        metadata,
                        pm => pm.Id,
                        m => ((JObject)m)["Id"].ToString(),
                        (pm, m) => new
                        {
                            PayloadMetadata = pm,
                            Metadata = (JObject)m
                        })
                    .ToList();

                foreach (var doc in updateDocs)
                {
                    doc.PayloadMetadata.Metadata = doc.Metadata;
                }

                var result = await payloadMetadataRepository.UpdateItemsAsync(
                    updateDocs.Select(d => d.PayloadMetadata)).ConfigureAwait(false);

                if (!result)
                {
                    log.LogError($"{FnName}: Failed to update metadata for payload {payloadId}.");
                }
                else
                {
                    log.LogInformation($"{FnName}: {updateDocs.Count()} metadata are updated for payload {payloadId}.");
                }

                log.LogInformation($"{FnName}: HTTP trigger to update metadata for payload with partition key {partitionkey} and payload id {payloadId} finished at {DateTime.Now.ToUniversalTime()}.");

                return result
                    ? new OkObjectResult($"The {updateDocs.Count()} metadata for {payloadId} was updated.")
                    : (ActionResult)new NotFoundObjectResult($"The metadata for {payloadId} clouldn't be updated.");
            }
            else
            {
                log.LogWarning($"{FnName}: There is no metadata to update for payload {payloadId}.");
                log.LogInformation($"{FnName}: HTTP trigger to update metadata for payload finished at {DateTime.Now.ToUniversalTime()}.");

                return new BadRequestObjectResult("There is no metadata to be updated.");
            }
        }
        else // data.GetType() == typeof(JObject)
        {
            var metadata = data.ToObject<JObject>();
            if (metadata.ContainsKey("Id"))
            {
                var metadataId = metadata["Id"].ToString();
                var payloadMetadata = await payloadMetadataRepository.GetItemAsync<PayloadMetadataCollectionItem<JObject>>(
                    metadataId,
                    payloadId.ToString());

                payloadMetadata.Metadata = metadata;

                var result = await payloadMetadataRepository.UpdateItemAsync(metadataId, payloadMetadata);

                if (result == null)
                {
                    log.LogError($"{FnName}: Failed to updated metadata for payload {payloadId}.");
                }
                else
                {
                    log.LogInformation($"{FnName}: Metadata for payload {payloadId} updated successfully.");
                }

                log.LogInformation($"{FnName}: HTTP trigger to update metadata for payload with partition key {partitionkey} and payload id {payloadId} finished at {DateTime.Now.ToUniversalTime()}.");

                return result != null
                    ? new OkObjectResult($"The payload with {payloadId} is updated.")
                    : (ActionResult)new NotFoundObjectResult($"The payload with {payloadId} couldn't be updated.");
            }
            else
            {
                return new BadRequestObjectResult($"No Id is found on the metadata object.");
            }
        }
    }
}
