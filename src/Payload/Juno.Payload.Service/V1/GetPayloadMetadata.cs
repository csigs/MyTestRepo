// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetPayloadMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Gets a payload's metadata from Metadata collection.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;

using System;
using System.Collections.Generic;
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
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Get payload's all metadata with HttpTrigger.
/// </summary>
public static class GetPayloadMetadata
{
    private const string OpId = nameof(Constants.GetPayloadMetadataV1);
    private const string FnName = Constants.GetPayloadMetadataV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/metadata";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadMetadataCollection" },
        Description = $"{FnName}: Gets a metadata collection of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        MediaTypeNames.Application.Json,
        bodyType: typeof(IEnumerable<JObject>),
        Description = "Metadata of a specified payload are successfully retrieved")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            log.LogInformation($"{FnName}: HTTP trigger to get payload metadata started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            var requireSign = req.RequireSign();

            var payloadMetadatas = await payloadMetadataRepository.GetItemsAsync<PayloadMetadata<JObject>>(m => m.PayloadId == payloadId);

            if (!payloadMetadatas.Any())
            {
                log.LogInformation($"{FnName}: The metadata for {payloadId} is not found.");
                log.LogInformation($"{FnName}: HTTP trigger to get payload metadata finished at {DateTime.Now.ToUniversalTime()}.");

                return new NotFoundObjectResult($"Failed to get the data with {payloadId}.");
            }

            log.LogInformation($"{FnName}: HTTP trigger to get payload metadata finished at {DateTime.Now.ToUniversalTime()}.");

            return payloadMetadatas.Select(m => m.Metadata).ToOkObjectResult(req, requireSign, signProvider);
        }
    }
}
