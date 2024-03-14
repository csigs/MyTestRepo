// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadPayloadMetadataCollectionV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   TODO: Fix documentation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
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
using Newtonsoft.Json.Linq;

using FunctionExtensions.DependencyInjection;
using Juno.Payload.Contracts.Dto.Pagination;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Extensions;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Get payload's all metadata with HttpTrigger.
/// </summary>
public static class ReadPayloadMetadataCollectionV2
{
    private const string OpId = nameof(Constants.ReadPayloadMetadataCollectionV2);
    private const string FnName = Constants.ReadPayloadMetadataCollectionV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/metadata";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadMetadataCollection" },
    //    Description = "Gets a metadata collection of a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiParameter(Constants.ContinuationTokenParamName, In = ParameterLocation.Query, Required = false, Type = typeof(string))]
    //[OpenApiResponseWithBody(
    //    HttpStatusCode.OK,
    //    MediaTypeNames.Application.Json,
    //    bodyType: typeof(DataChunkDto<JObject>),
    //    Description = "Successfully retrieved metadata of a specified payload.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        string partitionKey,
        string id,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            log.LogInformation($"{FnName}: HTTP trigger to get payload metadata for payload with partition key {partitionKey} and payload id {id} started at {DateTime.Now.ToUniversalTime()}.");

            var continuationToken = req.Query[Constants.ContinuationTokenParamName];

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                continuationToken = Uri.UnescapeDataString(continuationToken);

                log.LogDebug($"Continuation token: {continuationToken}");
            }
            else
            {
                log.LogDebug("A continuation token must not be null, empty, or whitespace.");
            }

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            // TODO: Is the partition key always a payload ID, or should we pass in the partition key from the caller.
            var dataChunk = await payloadMetadataRepository.GetItemsChunkAsync<PayloadMetadata<JObject>>(
                m => m.PayloadId == payloadId,
                payloadId.ToString(),
                continuationToken);

            if (dataChunk == null)
            {
                throw new InvalidOperationException("DataChunk can't be null");
            }

            var requireSign = req.RequireSign();
            var chunkDto = new DataChunkDto<JObject>(dataChunk.ContinuationToken, dataChunk.Items.Select(m => m.Metadata));

            log.LogInformation($"{FnName}: HTTP trigger to get payload metadata finished at {DateTime.Now.ToUniversalTime()}.");

            return chunkDto.ToOkObjectResult(req, requireSign, signProvider);
        }
    }
}
