// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetStoredPayloadDataReferenceByProvidedIds.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Gets paylod's data references by provieded Ids.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

using FunctionExtensions.DependencyInjection;
using Juno.Common.DataReference;
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class GetStoredPayloadDataReferenceByProvidedIds
{
    private class PostParam
    {
        public Guid[] ProvidedIds { get; set; }
    }

    private const string OpId = nameof(Constants.GetStoredPayloadDataReferenceByProvidedIdsV1);
    private const string FnName = Constants.GetStoredPayloadDataReferenceByProvidedIdsV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/datarefs/GetByProvidedIds";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadDataReferences" },
        Description = $"{FnName}: Gets specified data references of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiRequestBody(
        MediaTypeNames.Application.Json,
        typeof(Guid[]),
        Required = true,
        Description = "The id of data references to retrieve")]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        MediaTypeNames.Application.Json,
        bodyType: typeof(IEnumerable<LocElementDataReferenceDescriptor>),
        Description = "Specified data references")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Specified data references of a specified payload are not found.")]
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
            Trace.TraceInformation($"{FnName}: HTTP trigger to get payload's data references started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            // TODO: Make use of consolidated input validation
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

                var postParam = JsonConvert.DeserializeObject<PostParam>(requestBody);

                if (!postParam.ProvidedIds.Any())
                {
                    return new BadRequestObjectResult($"Request content does not contain any provided Ids.");
                }

                log.LogInformation($"{FnName}: Getting existing data reference of payload id {payloadId} started at {DateTime.Now.ToUniversalTime()}");

                var ids = postParam.ProvidedIds.Select(i => i.ToString()).ToArray();

                var existingDescriptors = await payloadDataRefRepository.GetItemsAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(
                    i => i.PayloadId == payloadId.ToString() && ids.Contains(i.ProvidedId));

                return existingDescriptors != null
                    ? existingDescriptors.Where(d => d.Data != null).Select(d => d.Data).ToOkObjectResult(req, requireSign, signProvider)
                    : new NotFoundObjectResult($"The payload data references for payload id {payloadId} couldn't be found.");
            }
            catch (JsonSerializationException exception)
            {
                log.LogError($"{FnName}: Request contennt cannot be serialized.\r\n  {requestBody}\r\n {exception}");

                return new BadRequestObjectResult($"Request contennt cannot be serialized.");
            }
        }
    }
}
