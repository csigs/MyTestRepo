// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetStoredPayloadDataReferences.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Gets a payload's all data referernce.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

using FunctionExtensions.DependencyInjection;
using Juno.Common.DataReference;
using Juno.Common.Metadata;
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Get payload data references for a payload with HttpTrigger.
/// </summary>
public static class GetStoredPayloadDataReferences
{
    private class PostParam
    {
        public DefaultLocElementMetadata[] LocElements { get; set; }
    }

    private const string OpId = nameof(Constants.GeStoredPayloadDataReferencesV1);
    private const string FnName = Constants.GeStoredPayloadDataReferencesV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/datarefs";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadDataReferences" },
        Description = $"{FnName}: Gets data references of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        MediaTypeNames.Application.Json,
        bodyType: typeof(IEnumerable<LocElementDataReferenceDescriptor>),
        Description = "Data references of a specified payload")]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Data references of a specified payload are not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadDataRefRepository payloadDataRefRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to get payload data references started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            try
            {
                log.LogInformation($"{FnName}: Getting existing payload with {id} started at {DateTime.Now.ToUniversalTime()}");

                var existingDescriptors = await payloadDataRefRepository.GetItemsAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(i => i.PayloadId == payloadId.ToString());
            
                var requireSign = req.RequireSign();

                return existingDescriptors != null
                    ? existingDescriptors.Where(d => d.Data != null).Select(d => d.Data).ToOkObjectResult(req, requireSign, signProvider)
                    : new NotFoundObjectResult($"The payload data references for payload id {payloadId} couldn't be found.");
            }
            catch (JsonSerializationException exception)
            {
                log.LogError($"{FnName}: Request or response content cannot be serialized.\r\n  \r\n {exception}");

                return new BadRequestObjectResult($"Content cannot be serialized/deserialized.");
            }
        }
    }
}
