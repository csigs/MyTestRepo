// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadDataV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's data object in required style as BlobAttachment, InlineMetadata or BinaryBlobAttachment. Use UpdatePayloadMetadata to upload a collection of metadata.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
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
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Extensions;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Update a payload's inline metadata with HttpTrigger. Inline metadata should be less than 2MB. 
/// If the metadata is a collection, consider use UploadPayloadMetadata.
/// </summary>
public static class UpdatePayloadDataV2
{
    private const string OpId = nameof(Constants.UpdatePayloadDataV2);
    private const string FnName = Constants.UpdatePayloadDataV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/data";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadData" },
    //    Description = $"{FnName}: Updates data of a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //// TODO: What's in the request body?
    //// [OpenApiRequestBody()]
    //[OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Data of a specified payload are successfully updated.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters or request body are not specified.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route)] HttpRequest req,
        string partitionKey,
        string id,
        [Inject] IPayloadRepository payloadRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] IPayloadAttachmentRepository payloadAttachmentRepository,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to update a payload started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            // TODO: Consolidate input validation
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                log.LogWarning($"{FnName}: {partitionKey} can't be null, empty, or whitespace.");

                return new BadRequestObjectResult($"{partitionKey} can't be null, empty or whitespace.");
            }

            if (!req.Query.ContainsKey(Constants.PayloadDataTypeParamName))
            {
                log.LogWarning($"{FnName}: {Constants.PayloadDataTypeParamName} need to be supplied as query param.");

                return new BadRequestObjectResult($"{Constants.PayloadDataTypeParamName} need to be supplied as query param.");
            }

            var payloadDataTypeStr = req.Query[Constants.PayloadDataTypeParamName];

            if (string.IsNullOrWhiteSpace(payloadDataTypeStr))
            {
                log.LogWarning($"{FnName}: {Constants.PayloadDataTypeParamName} can't be null, empty, or whitespace.");

                return new BadRequestObjectResult($"{Constants.PayloadDataTypeParamName} can't be null, empty, or whitespace.");
            }

            if (!Enum.TryParse(payloadDataTypeStr, true, out PayloadDataType payloadDataType))
            {
                log.LogWarning($"{FnName}: Can't parse {Constants.PayloadDataTypeParamName} from value {payloadDataTypeStr}.");

                return new BadRequestObjectResult($"Can't parse {Constants.PayloadDataTypeParamName} from value {payloadDataTypeStr}.");
            }

            log.LogInformation($"{FnName}: Updating payload id {payloadId} and partitionKey {partitionKey} with inline metadata started at {DateTime.Now.ToUniversalTime()}");

            //TODO: TMP fix for UAT with hardcoded partition key.
            var storedPayload = await payloadRepository.GetItemAsync<PayloadWithData>(payloadId.ToString(), partitionKey);

            PayloadWithData doc;
            if (storedPayload == null)
            {
                log.LogWarning($"Payload id {payloadId} with partitionKey {partitionKey} not found");

                return new NotFoundObjectResult($"Payload id {payloadId} with partitionKey {partitionKey} not found.");
            }
            else
            {
                switch (payloadDataType)
                {
                    case PayloadDataType.BlobAttachment:
                        // TODO: validate and throw?
                        if (!req.ContentType.Contains(MediaTypeNames.Application.Json))
                        {
                            // TODO: handle case when content type is not json
                            log.LogWarning($"Requested payload content type for payload id:{payloadId} partition key:{partitionKey} is not application/json. Supplied type is:{req.ContentType}");
                        }

                        var blobAttachmentReference = new BlobAttachmentReference(
                            BlobAttachmentId.CreateFor(storedPayload),
                            new ContentType(MediaTypeNames.Application.Json),
                            GetGzipContentEncodingValue(req));

                        storedPayload.UpdatePayloadInlineMetadataWith(payloadDataType, blobAttachmentReference);

                    var dataStream = req.Body;

                    try
                    {
                        var requireSign = req.RequireSign();

                        if (requireSign)
                        {
                            dataStream = req.VerifySignature(signProvider);
                        }
                    }
                    catch (Exception e)
                    {
                        var errorMessage = $"Failed to create new payload.";
                        log.LogError($"{FnName}: {errorMessage} {e.Message}");

                        return new BadRequestObjectResult($"{errorMessage} {e.Message}");
                    }

                    await payloadAttachmentRepository.UploadAttachmentAsync(blobAttachmentReference, dataStream);
                    doc = await payloadRepository.UpdateItemAsync(payloadId.ToString(), storedPayload);

                    break;
                case PayloadDataType.InlineMetadata:
                case PayloadDataType.BinaryBlobAttachment:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(payloadDataType.ToString());
            }
        }

            log.LogInformation($"{FnName}: HTTP trigger to updated a payload finished at {DateTime.Now.ToUniversalTime()}");

            return doc != null
                ? new OkObjectResult($"The payload with {payloadId} is updated.")
                : (ActionResult)new NotFoundObjectResult($"The payload with {payloadId} couldn't be updated. Check if it exists.");
        }
    }

    private static string GetGzipContentEncodingValue(HttpRequest req)
    {
        if (req is null)
        {
            throw new ArgumentNullException(nameof(req));
        }

        if (req.HttpContext.Request.Headers.TryGetValue("Content-Encoding", out var encodings))
        {
            if (encodings.Contains("gzip"))
            {
                return "gzip";
            }
        }

        return null;
    }
}
