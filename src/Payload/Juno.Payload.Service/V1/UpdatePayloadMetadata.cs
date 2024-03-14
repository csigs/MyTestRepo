// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's metadata to database in case metadata is a collection.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;

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

public static class UpdatePayloadMetadata
{
    private const string OpId = nameof(Constants.UpdatePayloadMetadataV1);
    private const string FnName = Constants.UpdatePayloadMetadataV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/metadata";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadMetadataCollection" },
        Description = $"{FnName}: Updates a metadata collection of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(JToken), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Metadata of a specified payload are successfully updated.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            log.LogInformation($"{FnName}: HTTP trigger to update a payload's metadata started at {DateTime.Now.ToUniversalTime()}.");

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
                var requireSign = req.RequireSign();

                if (requireSign)
                {
                    req.VerifySignature(signProvider, requestBody);
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to process metadata for payload {payloadId}.";
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

                    var payloadMetadatas = await payloadMetadataRepository.GetItemsAsync<PayloadMetadata<JObject>>(
                            m => ids.Contains(m.Id));

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
                        log.LogInformation($"{FnName}: {metadata.Count} metadata are uploaded for payload {payloadId}.");
                    }

                    log.LogInformation($"{FnName}: HTTP trigger to update metadata for payload finished at {DateTime.Now.ToUniversalTime()}.");

                    return result
                        ? new OkObjectResult($"The metadata for {payloadId} is uploaded.")
                        : (ActionResult)new NotFoundObjectResult($"The metadata for {payloadId} couldn't be uploaded.");
                }
                else
                {
                    log.LogWarning($"{FnName}: There is no metadata to upload for paload {payloadId}.");
                    log.LogInformation($"{FnName}: HTTP trigger to upload metadata for payload finished at {DateTime.Now.ToUniversalTime()}.");

                    return new BadRequestObjectResult($"There is no metadata to upload.");
                }
            }
            else // data.GetType() == typeof(JObject)
            {
                var metadata = data.ToObject<JObject>();
                if (metadata.ContainsKey("Id"))
                {
                    var metadataId = metadata["Id"].ToString();

                    var payloadMetadata = await payloadMetadataRepository.GetItemAsync<PayloadMetadata<JObject>>(metadataId);

                    payloadMetadata.Metadata = metadata;

                    var result = await payloadMetadataRepository.UpdateItemAsync(metadataId, payloadMetadata);

                    if (result == null)
                    {
                        log.LogError($"{FnName}: Failed to update metadata for payload {payloadId}.");
                    }
                    else
                    {
                        log.LogInformation($"{FnName}: Metadata for payload {payloadId} updated successfully.");
                    }

                    log.LogInformation($"{FnName}: HTTP trigger to update metadata finished at {DateTime.Now.ToUniversalTime()}.");

                    return result != null
                        ? new OkObjectResult($"The payload with {payloadId} is updated.")
                        : (ActionResult)new NotFoundObjectResult($"The payload with {payloadId} couldn't be updated.");
                }
                else
                {
                    return new BadRequestObjectResult($"No id is found in object.");
                }
            }
        }
    }
}
