// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetPayloadInlineMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Gets a payload's metadata.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Http;

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
/// Get payload's metadata by Id with HttpTrigger.
/// </summary>
public static class GetPayloadInlineMetadataById
{
    private const string OpId = nameof(Constants.GetPayloadInlineMetadataV1);
    private const string FnName = Constants.GetPayloadInlineMetadataV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}/inlinemetadata";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "PayloadData" },
        Description = $"{FnName}: Gets inline metadata of a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        MediaTypeNames.Application.Json,
        bodyType: typeof(JObject),
        Description = "Inline metadata of a specified payload are successfully retrieved")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadRepository payloadRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] IPayloadAttachmentRepository payloadAttachmentRepository,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to get a payload inline metadata.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            var requireSign = req.RequireSign();

            //TODO: TMP fix for UAT with hardcoded partition key.

            var newPayload = await payloadRepository.GetItemAsync<PayloadWithData>(payloadId.ToString(), PayloadWithData.DefaultPartitionKeyValue);

            if (newPayload == null)
            {
                log.LogInformation($"{FnName}: The document for {payloadId} is not found.");

                return new NotFoundObjectResult($"Failed to get the data with {payloadId}.");
            }

            if (newPayload.MetadataType == Constants.PayloadInlineMetadataAsBlobAttachmentType)
            {
                if (newPayload.Metadata == null)
                {
                    log.LogError($"Blob attachment is not set on payload id {newPayload.Id}.");

                    return new InternalServerErrorResult();
                }

                var blobAttachment = newPayload.Metadata.ToObject<BlobAttachmentReference>();

                if (blobAttachment == null)
                {
                    log.LogError($"Blob attachment is not set on payload id {newPayload.Id}.");

                    return new InternalServerErrorResult();
                }

                if (await payloadAttachmentRepository.ExistsAsync(blobAttachment))
                {
                    var stream = await payloadAttachmentRepository.GetAttachmentAsync(blobAttachment);
                    var data = await new StreamReader(stream).ReadToEndAsync().ConfigureAwait(false);
                    var contentType = blobAttachment.AttachmentContentType ?? MediaTypeNames.Text.Plain;

                    return data.ToContentResult(req, contentType, requireSign, signProvider);
                }
                else
                {
                    log.LogError($"Blob attachment is not found in blob for payload id {newPayload.Id}.");

                    return new InternalServerErrorResult();
                }
            }
            else
            {
                return newPayload.Metadata.ToOkObjectResult(req, requireSign, signProvider);
            }
        }
    }
}
