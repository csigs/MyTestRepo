// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UploadPayloadDataReferences.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload Payload's DataReference to database.
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

using FunctionExtensions.DependencyInjection;
using Juno.Common.DataReference;
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Payload.Service.Model;
using Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class UploadPayloadDataReferences
{
    internal class PostParams
    {
        public List<LocElementDataReferenceDescriptor> DataReferences { get; set; }
    }

    private const string OpId = nameof(Constants.UploadPayloadDataReferencesV1);
    private const string FnName = Constants.UploadPayloadDataReferencesV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/datarefs";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadDataReferences" },
        Description = $"{FnName}: Uploads data references for a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(List<LocElementDataReferenceDescriptor>), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Data references of a specified payload are successfully uploaded.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadDataRefRepository payloadDataRefRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to delete a payload started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }
            var requireSign = req.RequireSign();

            var useTransaction = req.GetUseTransactionSafely();

            var postParams = await req.Body.TryParseRequestBodyAsync<PostParams>(
                log,
                FnName,
                $"{FnName}: Specify payload data references in request body.",
                "Specify data references in request body.",
                req,
                requireSign,
                signProvider).ConfigureAwait(false);

            if (!postParams.ParseResult)
            {
                return postParams.BadRequestErrorMessageResult;
            }

            if (!postParams.DeserializedRequestBody.DataReferences.Any())
            {
                log.LogError($"{FnName}: No DataReference are provided to upload, {DateTime.Now.ToUniversalTime()}");
            }

            log.LogInformation($"{FnName}: Getting existing  payload with {payloadId} started at {DateTime.Now.ToUniversalTime()}");

            if (postParams.DeserializedRequestBody.DataReferences.Any(d => d.LocElementMetadata == null || d.DataAccessDescriptor == null))
            {
                return new BadRequestObjectResult("Please pass a payload datareference with metadata descriptor and data access descriptor.");
            }

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

                return new OkObjectResult($"The payload data reference for {payloadId} is updated in transaction.");
            }
            else
            {
                var result = await payloadDataRefRepository.CreateItemsAsync(
                    postParams.DeserializedRequestBody.DataReferences
                        .Select(d => new PayloadDataReference<LocElementDataReferenceDescriptor>
                        {
                            Id = Guid.NewGuid().ToString(),
                            PayloadId = payloadId.ToString(),
                            ProvidedId = d.LocElementMetadata.GroupId.ToString(),
                            Data = d
                        })).ConfigureAwait(false);

                if (result == null)
                {
                    log.LogInformation($"{FnName}: HTTP trigger to updated a payload finished at {DateTime.Now.ToUniversalTime()}");
                }

                return result != null
                    ? new OkObjectResult($"The payload data reference for {payloadId} is updated.")
                    : (ActionResult)new NotFoundObjectResult($"The payload with {payloadId} clouldn't be updated. Check if it exists.");
            }
        }
    }
}
