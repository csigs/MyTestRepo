// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadPayloadMetadataCollectionItemsByIdV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   TODO: Fix documentation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

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
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Extensions;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Get payload's metadata by Id with HttpTrigger.
/// </summary>
public static class ReadPayloadMetadataCollectionItemsByIdV2
{
    private class PostParam
    {
        public List<Guid> MetadataIds { get; set; }
    }

    private const string OpId = nameof(Constants.ReadPayloadMetadataCollectionItemsByIdV2);
    private const string FnName = Constants.ReadPayloadMetadataCollectionItemsByIdV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/metadataById";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadMetadataCollection" },
    //    Description = "Gets a specified metadata collection",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(List<Guid>), Required = true)]
    //[OpenApiResponseWithBody(
    //    HttpStatusCode.OK,
    //    MediaTypeNames.Application.Json,
    //    bodyType: typeof(IEnumerable<JObject>),
    //    Description = "Successfully retrieved specified metadata of a specified payload.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Specified metadata are not found for a specified payload.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
        string partitionKey,
        string id,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            log.LogInformation($"{FnName}: HTTP trigger to get payload metadata collection for payload with partition key {partitionKey} and payload id {id} started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

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

                // Bug: postParam should filter out empty GUID
                // postParam.MetadataIds = postParam.MetadataIds.Where(m => m != Guid.Empty).ToList();

                if (!postParam.MetadataIds.Any())
                {
                    return new BadRequestObjectResult("Request content does not contain any metadata Ids.");
                }

                log.LogInformation($"{FnName}: Getting existing metadata of payload id {payloadId} started at {DateTime.Now.ToUniversalTime()}");

                var ids = postParam.MetadataIds.Select(i => i.ToString());

                //TODO: stream data to client
                var payloadMetadatas = await payloadMetadataRepository.GetItemsAsync<PayloadMetadataCollectionItem<JObject>>(
                    m => ids.Contains(m.Id), payloadId.ToString());

                if (!payloadMetadatas.Any())
                {
                    log.LogInformation($"{FnName}: The metadata for {payloadId} is not found.");
                    log.LogInformation($"{FnName}: HTTP trigger to get payload metadata finished at {DateTime.Now.ToUniversalTime()}.");

                    return new NotFoundObjectResult("Failed to get the metadata with given Ids.");
                }
                // BUG: Not handling else if payloadMetadatas.Count() < postParam.MetadataIds.Count()

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
