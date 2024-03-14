// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetPayloadMetadataById.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Gets a payload's metadata from Metadata collection.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;
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
/// Get payload's metadata by Id with HttpTrigger.
/// </summary>
public static class GetPayloadMetadataById
{
    private class PostParam
    {
        public List<Guid> ProvidedIds { get; set; }
    }

    private const string OpId = nameof(Constants.GetPayloadMetadataByIdV1);
    private const string FnName = Constants.GetPayloadMetadataByIdV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/metadataById";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadMetadataCollection" },
        Description = $"{FnName}: Gets specified a payload metadata collection",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiRequestBody(
        MediaTypeNames.Application.Json,
        typeof(List<Guid>),
        Required = true,
        Description = "The id of metadata to retrieve")]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        MediaTypeNames.Application.Json,
        bodyType: typeof(IEnumerable<JObject>),
        Description = "Specified metadata")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Specified metadata are not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
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

                if (req.RequireSign())
                {
                    req.VerifySignature(signProvider);
                }

                var postParam = JsonConvert.DeserializeObject<PostParam>(requestBody);

                if (!postParam.ProvidedIds.Any())
                {
                    return new BadRequestObjectResult("Request content does not contain any provided Ids.");
                }

                log.LogInformation($"{FnName}: Getting existing data reference of payload id {payloadId} started at {DateTime.Now.ToUniversalTime()}");

                var ids = postParam.ProvidedIds.Select(i => i.ToString());

                var payloadMetadatas = await payloadMetadataRepository.GetItemsAsync<PayloadMetadata<JObject>>(m => ids.Contains(m.Id));

                if (!payloadMetadatas.Any())
                {
                    log.LogInformation($"{FnName}: The metadata for {payloadId} is not found.");
                    log.LogInformation($"{FnName}: HTTP trigger to get payload metadata finished at {DateTime.Now.ToUniversalTime()}.");

                    return new NotFoundObjectResult("Failed to get the metadata with given Ids.");
                }

                log.LogInformation($"{FnName}: HTTP trigger to get payload metadata finished at {DateTime.Now.ToUniversalTime()}.");

                return payloadMetadatas.Select(m => m.Metadata).ToOkObjectResult(req, requireSign, signProvider);
            }
            catch (JsonSerializationException exception)
            {
                log.LogError($"{FnName}: Request contennt cannot be serialized.\r\n  {requestBody}\r\n {exception}");

                return new BadRequestObjectResult("Request contennt cannot be serialized.");
            }
        }
    }
}
