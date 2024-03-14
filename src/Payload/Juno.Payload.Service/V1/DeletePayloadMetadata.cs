// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeletePayloadMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Delete a payload metadata from database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;
using System;
using System.Diagnostics;
using System.Net;
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
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Metrics;

/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class DeletePayloadMetadata
{
    private const string OpId = nameof(Constants.DeletePayloadMetadataV1);
    private const string FnName = Constants.DeletePayloadMetadataV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/metadata";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadMetadataCollection" },
        Description = $"{FnName}: Deletes a metadata collection of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Metadata of a specified payload are successfully deleted.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to delete a payload metadata started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            log.LogInformation($"{FnName}: Deleting payload with {id} started at {DateTime.Now.ToUniversalTime()}");

            await payloadMetadataRepository.DeleteItemsAsync<PayloadMetadata<JObject>>(b => b.PayloadId.ToString() == id);

            log.LogInformation($"{FnName}: HTTP trigger to delete a payload finished at {DateTime.Now.ToUniversalTime()}");

            return new OkObjectResult($"The payload metadata with {id} is deleted.");
        }
    }
}
