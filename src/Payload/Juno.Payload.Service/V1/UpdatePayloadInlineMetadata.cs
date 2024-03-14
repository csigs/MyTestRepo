// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadInlineMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's inline metadata to database. Use UpdatePayloadMetadata to upload a collection of metadata.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;
using System;
using System.Diagnostics;
using System.IO;
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

using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Update a payload's inline metadata with HttpTrigger. Inline metadata should be less than 2MB. 
/// If the metadata is a collection, consider use UploadPayloadMetadata.
/// </summary>
public static class UpdatePayloadInlineMetadata
{
    private const string OpId = nameof(Constants.UpdatePayloadInlineMetadataV1);
    private const string FnName = Constants.UpdatePayloadInlineMetadataV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/inlinemetadata";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadData" },
        Description = $"{FnName}: Updates inline metadata of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(JObject), Required = true)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Inline metadata of a specified payload are successfully updated.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadRepository payloadRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] IPayloadAttachmentRepository payloadAttachmentRepository,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to delete a payload started at {DateTime.Now.ToUniversalTime()}.");

            if (!Guid.TryParse(id, out var payloadId))
            {
                log.LogError($"{FnName}: Payload id could not be determined.");

                return new BadRequestObjectResult("Please pass a payload id with metadata in the request.");
            }

            if (payloadId == Guid.Empty)
            {
                return new BadRequestObjectResult("Payload id can't be zero.");
            }

            // TODO: validate and throw
            if (!req.ContentType.Contains("application/json"))
            {
                // TODO: handle case when content type is not json
                log.LogWarning($"{FnName}: Supplied ContentType is not application/json for payload id {id}");
            }

            //var requestBody = await (new StreamReader(req.Body)).ReadToEndAsync().ConfigureAwait(false);

            //if (string.IsNullOrWhiteSpace(requestBody))
            //{
            //    log.LogError($"{functionName}: Payload metadata is empty in request body.");
            //    return new BadRequestObjectResult("Please pass a payload metadata in the request body.");
            //}

            //var data = JsonConvert.DeserializeObject<JObject>(requestBody);

            log.LogInformation($"{FnName}: Update payload inline metadata for payload id {payloadId} started at {DateTime.Now.ToUniversalTime()}");

            //TODO: TMP fix for UAT with hardcoded partition key.
            var storedPayload = await payloadRepository.GetItemAsync<PayloadWithData>(payloadId.ToString(), PayloadWithData.DefaultPartitionKeyValue);

            PayloadWithData doc;
            if (storedPayload == null)
            {
                throw new InvalidOperationException($"Did not find stored payload {payloadId}.");
            }
            else
            {
                var blobAttachmentReference = new BlobAttachmentReference(BlobAttachmentId.CreateFor(storedPayload), new ContentType(req.ContentType));
                storedPayload.UpdatePayloadInlineMetadataWith(PayloadDataType.BlobAttachment, blobAttachmentReference);

                var dataStream = req.Body;

                try
                {
                    var requireSign = req.RequireSign();
                    if (requireSign)
                    {
                       dataStream = req.VerifySignature(signProvider);
                    }

                    await payloadAttachmentRepository.UploadAttachmentAsync(blobAttachmentReference, dataStream);
                    doc = await payloadRepository.UpdateItemAsync(payloadId.ToString(), storedPayload);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed to process metadata for payload {payloadId}.";
                    log.LogError($"{FnName}: {errorMessage} {e.Message}");

                    return new BadRequestObjectResult($"{errorMessage} {e.Message}");
                }
            }

            log.LogInformation($"{FnName}: HTTP trigger to updated a payload finished at {DateTime.Now.ToUniversalTime()}");

            return doc != null
                ? new OkObjectResult($"The payload with {payloadId} is updated.")
                : (ActionResult)new NotFoundObjectResult($"The payload with {payloadId} couldn't be updated. Check if it exists.");
        }
    }
}
