// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddPayloadMetadataCollectionItemsV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   TODO: Fix documentation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
using System.Collections.Generic;
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
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Uploads a payload's metadata collection with Http trigger.
/// </summary>
public static class AddPayloadMetadataCollectionItemsV2
{
    private const string OpId = nameof(Constants.AddPayloadMetadataCollectionItemsV2);
    private const string FnName = Constants.AddPayloadMetadataCollectionItemsV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/metadata";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadMetadataCollection" },
    //    Description = $"{FnName}: Adds a metadata collection in a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(List<PayloadMetadataCollectionItem<JObject>>), Required = true)]
    //[OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Successfully added metadata in a specified payload")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Review logs for missing required parameters or request body")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Not found a specified payload")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
        string partitionKey,
        string id,
        // TODO: Use autofac injection or https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            log.LogInformation($"{FnName}: HTTP trigger to add a payload's metadata started at {DateTime.UtcNow}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            // TODO: Refactor conversion of a request body to internal model.
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                log.LogError($"{FnName}: Payload metadata is empty in request body.");

                return new BadRequestObjectResult("Please pass a payload metadata in the request body.");
            }

            var metadataToUpload = new List<PayloadMetadataCollectionItem<JObject>>();

            try
            {
                var requireSign = req.RequireSign();

                if (requireSign)
                {
                    req.VerifySignature(signProvider, requestBody);
                }

                metadataToUpload.AddRange(PreparePayloadMetadataToUpload(requestBody, payloadId));
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to process metadata for payload {payloadId}.";
                log.LogError($"{FnName}: {errorMessage} {e.Message}");

                return new BadRequestObjectResult($"{errorMessage} {e.Message}");
            }

            if (!metadataToUpload.Any())
            {
                log.LogWarning($"{FnName}: There is no metadata to add for payload {payloadId}.");
                log.LogInformation($"{FnName}: HTTP trigger to add metadata for payload finished at {DateTime.UtcNow}.");

                return new BadRequestObjectResult($"There is no metadata to add.");
            }

            log.LogInformation($"Uploading {metadataToUpload.Count()} metadata for payload {payloadId}...");

            var useTransaction = req.GetUseTransactionSafely();

            if (useTransaction)
            {
                await payloadMetadataRepository.CreateItemsBatchAsync(
                    metadataToUpload.Select(m => new PayloadMetadata<JObject>
                    {
                        Id = Guid.NewGuid().ToString(),
                        PayloadId = payloadId,
                        Metadata = m.Metadata,
                        MetadataType = m.Metadata.GetType().Name
                    }),
                    payloadId.ToString()).ConfigureAwait(false);
            }
            else
            {
                var result = await payloadMetadataRepository.CreateItemsAsync(metadataToUpload);

                if (result is null)
                {
                    var errorMessage = $"Failed to upload metadata for payload {payloadId}.";
                    log.LogError($"{FnName}: Failed to upload metadata for payload {payloadId}.");

                    return new NotFoundObjectResult(errorMessage);
                }
            }

            var successMessage = $"HTTP trigger to upload metadata for payload {payloadId} successfully finished at {DateTime.UtcNow}. UseTransaction: {useTransaction}";
            log.LogInformation($"{FnName}: {successMessage}");

            return new OkObjectResult(successMessage);
        }
    }

    internal static List<JObject> ProcessToken(JToken token)
    {
        var objects = new List<JObject>();

        if (token is JArray)
        {
            foreach (var tkn in token.Children())
            {
                objects.AddRange(ProcessToken(tkn));
            }
        }
        else if (token is JObject)
        {
            objects.Add(token.ToObject<JObject>());
        }
        else if (token is JValue)
        {
            var errorMessage = "Only a value is specified, which translates to a JValue object."
                + " Payload metadata must be one or more pairs of key and value.";
            throw new InvalidOperationException(errorMessage);
        }
        else
        {
            throw new NotSupportedException($"Unknown type specified: {token.GetType()}");
        }

        return objects;
    }

    internal static IEnumerable<PayloadMetadataCollectionItem<JObject>> PreparePayloadMetadataToUpload(
        string rawMetadata,
        Guid payloadId)
    {
        var data = JsonConvert.DeserializeObject<JToken>(rawMetadata);

        var metadata = ProcessToken(data);

        var payloadMetadata = metadata.Select(m => new PayloadMetadataCollectionItem<JObject>
        {
            Id = GetPayloadMetadataIdString(m),
            PayloadId = payloadId,
            Metadata = m,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        });

        return payloadMetadata;
    }

    internal static string GetPayloadMetadataIdString(JToken token)
    {
        var payloadMetadaId = Guid.NewGuid().ToString();

        if (token is JObject jObject && jObject.ContainsKey("Id"))
        {
            payloadMetadaId = jObject["Id"].ToString();
        }

        return payloadMetadaId;
    }
}
