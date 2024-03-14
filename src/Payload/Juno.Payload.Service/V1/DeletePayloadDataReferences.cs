// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeletePayloadDataReferences.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Delete payload references from database.
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

using FunctionExtensions.DependencyInjection;
using Juno.Common.DataReference;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;

/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class DeletePayloadDataReferences
{
    private const string OpId = nameof(Constants.DeletePayloadDataReferencesV1);
    private const string FnName = Constants.DeletePayloadDataReferencesV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/datarefs";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadDataReferences" },
        Description = $"{FnName}: Deletes data references of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Data references of a specified payload are successfully deleted.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadDataRefRepository payloadDataReferencesRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to delete a payload data references started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            log.LogInformation($"{FnName}: Deleting payload data references for payload id {id} started at {DateTime.Now.ToUniversalTime()}");

            await payloadDataReferencesRepository.DeleteItemsAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(b => b.PayloadId == id);

            log.LogInformation($"{FnName}: HTTP trigger to delete a payload data references finished at {DateTime.Now.ToUniversalTime()}");

            return new OkObjectResult($"The payload data references for payload id {id} is deleted.");
        }
    }
}
