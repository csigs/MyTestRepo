// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddPayloadDataReferencesV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   TODO: Fix documentation
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

using FunctionExtensions.DependencyInjection;
using Juno.Common.DataReference;
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Microsoft.Localization.SignProviders;
using Payload.Service.Model;
using Payload.Service.Repository;
using static Juno.Payload.Service.V1.UploadPayloadDataReferences;
using Juno.Payload.Service.Extensions;

/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class AddPayloadDataReferencesV2
{
    private class PostParams
    {
        public List<LocElementDataReferenceDescriptor> DataReferences { get; set; }
    }

    private const string OpId = nameof(Constants.AddPayloadDataReferencesV2);
    private const string FnName = Constants.AddPayloadDataReferencesV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/datarefs";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadDataReferences" },
    //    Description = $"{FnName}: Adds data references in a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(List<LocElementDataReferenceDescriptor>), Required = true)]
    //[OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Successfully added data references in a specified payload")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Review logs for missing required parameters or request body")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Not found a specified payload")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
        string partitionkey,
        string id,
        [Inject] IPayloadDataRefRepository payloadDataRefRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to add data references for a payload with partition key {partitionkey} and payload id {id} started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            var requireSign = req.RequireSign();

            var postParams = await req.Body.TryParseRequestBodyAsync<PostParams>(
                log,
                FnName,
                logMessage: "Missing request body",
                resultMessage: "Specify payload data reference with metadata descriptor and data access descriptor",
                req,
                requireSign,
                signProvider);

            if (!postParams.ParseResult)
            {
                return postParams.BadRequestErrorMessageResult;
            }

            if (!postParams.DeserializedRequestBody.DataReferences.Any())
            {
                log.LogError($"{FnName}: No data references are provided to add, {DateTime.Now.ToUniversalTime()}");
            }

            log.LogInformation($"{FnName}: Getting existing  payload with {payloadId} started at {DateTime.Now.ToUniversalTime()}");

            if (postParams.DeserializedRequestBody.DataReferences.Any(d => d.LocElementMetadata == null || d.DataAccessDescriptor == null))
            {
                return new BadRequestObjectResult("Please pass a payload datareference with metadata descriptor and data access descriptor.");
            }

            bool useTransaction = req.GetUseTransactionSafely();

            if (useTransaction)
            {
                await payloadDataRefRepository.CreateItemsBatchAsync(
                   postParams.DeserializedRequestBody.DataReferences
                       .Select(d => new PayloadDataReference<LocElementDataReferenceDescriptor>
                       {
                           Id = Guid.NewGuid().ToString(),
                           PayloadId = payloadId.ToString(),
                           ProvidedId = d.LocElementMetadata.GroupId.ToString(),
                           Data = d
                       }),
                   payloadId.ToString()).ConfigureAwait(false);
                return new OkObjectResult($"The payload data reference for {payloadId} added in transaction.");
            }
            else
            {

                var result = await payloadDataRefRepository.CreateItemsAsync(
                postParams.DeserializedRequestBody.DataReferences.Select(d => new PayloadDataReference<LocElementDataReferenceDescriptor>
                {
                    Id = Guid.NewGuid().ToString(),
                    PayloadId = payloadId.ToString(),
                    ProvidedId = d.LocElementMetadata.GroupId.ToString(),
                    Data = d
                })).ConfigureAwait(false);

                if (result == null)
                {
                    log.LogInformation($"{FnName}: HTTP trigger to add payload data references for payload with partition key {partitionkey} and payload id {payloadId} finished at {DateTime.Now.ToUniversalTime()}");

                    return new NotFoundObjectResult($"The payload with {payloadId} couldn't be updated. Check if it exists.");
                }
                else
                {
                    return new OkObjectResult($"{result.Count()} payload data references for {payloadId} were added.");
                }
            }
        }
    }
}
