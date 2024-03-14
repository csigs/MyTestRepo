// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadPayloadDataReferencesV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Gets a payload's all data referernce.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

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
using Juno.Payload.Contracts.Dto.Pagination;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Extensions;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Get payload data references for a payload with HttpTrigger.
/// </summary>
public static class ReadPayloadDataReferencesV2
{
    private class PostParam
    {
        public DefaultLocElementMetadata[] LocElements { get; set; }
    }

    private const string OpId = nameof(Constants.ReadPayloadDataReferencesV2);
    private const string FnName = Constants.ReadPayloadDataReferencesV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/datarefs";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadDataReferences" },
    //    Description = $"{FnName}: Gets data references of a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(
    //    Constants.PartitionKeyParamName,
    //    In = ParameterLocation.Path,
    //    Required = true,
    //    Type = typeof(string),
    //    Description = "Payload id is used internally as a partition key while a non-empty partition key must still be specified in the path.")]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiParameter(Constants.ContinuationTokenParamName, In = ParameterLocation.Query, Required = false, Type = typeof(string))]
    //[OpenApiResponseWithBody(
    //    HttpStatusCode.OK,
    //    MediaTypeNames.Application.Json,
    //    bodyType: typeof(IEnumerable<LocElementDataReferenceDescriptor>),
    //    Description = "Data references of a specified payload")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Data references of a specified payload are not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        string partitionkey,
        string id,
        [Inject] IPayloadDataRefRepository payloadDataRefRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to read payload data references for payload id {id} and partition key {partitionkey} started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            var continuationToken = req.Query["continuationToken"];

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                continuationToken = Uri.UnescapeDataString(continuationToken);
            }

            try
            {
                log.LogInformation($"{FnName}: Getting existing payload with {payloadId} started at {DateTime.Now.ToUniversalTime()}");

                var existingDescriptors = await payloadDataRefRepository.GetItemsChunkAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(
                    i => i.PayloadId == payloadId.ToString(),
                    payloadId.ToString(),
                    continuationToken); // data references uses payload id as partition key but from API perspective we need to keep it consistent from payload side

                var requireSign = req.RequireSign();
                var chunkDto = new DataChunkDto<LocElementDataReferenceDescriptor>(existingDescriptors.ContinuationToken, existingDescriptors.Items.Select(d => d.Data));

                return existingDescriptors != null
                    ? chunkDto.ToOkObjectResult(req, requireSign, signProvider)
                    : new NotFoundObjectResult($"The payload data references for payload id {payloadId} couldn't be found");
            }
            catch (JsonSerializationException exception)
            {
                log.LogError($"{FnName}: Request or response content cannot be serialized.\r\n  \r\n {exception}");

                return new BadRequestObjectResult("Content cannot be serialized/deserialized.");
            }
        }
    }
}
