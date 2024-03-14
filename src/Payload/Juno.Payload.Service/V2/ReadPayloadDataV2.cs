// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadPayloadDataV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   TODO: Fix documentation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Microsoft.Localization.SignProviders;

using FunctionExtensions.DependencyInjection;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.Extensions;


/// <summary>
/// Get payload's metadata by Id with HttpTrigger.
/// </summary>
public static class ReadPayloadDataV2
{
    private const string OpId = nameof(Constants.ReadPayloadDataV2);
    private const string FnName = Constants.ReadPayloadDataV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/data";

    // BUG: What's so unique about this method that it doesn't return IActionResult like the other APIs?
    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadData" },
    //    Description = $"{FnName}: Gets data of a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiResponseWithBody(
    //    HttpStatusCode.OK,
    //    MediaTypeNames.Application.Json,
    //    bodyType: typeof(PayloadWithData),
    //    Description = "Data of a specified payload")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Data are not found for a specified payload.")]
    [FunctionName(FnName)]
    public static async Task<HttpResponseMessage> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
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
            Trace.TraceInformation($"{FnName}: HTTP trigger to get a payload.");

            if (string.IsNullOrWhiteSpace(id))
            {
                log.LogWarning($"{FnName}: Payload id is not provided.");

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Please pass a payload id in query string or call with api/v2/payloads/{partitionKey}/{id}.")
                };
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                log.LogWarning($"{FnName}: {nameof(partitionKey)} is not provided.");

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Please pass a {nameof(partitionKey)} or call with api/v2/payloads/{{partitionKey}}/{{id}}.")  // if used interpolation string we need to pass double braces
                };
            }

            if (!Guid.TryParse(id, out var payloadId))
            {
                log.LogError($"{FnName}: PayloadId could not be determined.");

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please pass a payload Id with metadata in the request.")  // if used interpolation string we need to pass double braces
                };
            }

            if (payloadId == Guid.Empty)
            {
                log.LogWarning($"{FnName}: payload Id can't be empty.");

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Payload id can't be empty.")  // if used interpolation string we need to pass double braces
                };
            }

            var newPayload = await payloadRepository.GetItemAsync<PayloadWithData>(payloadId.ToString(), partitionKey);

            if (newPayload == null)
            {
                log.LogInformation($"{FnName}: The document for {payloadId} is not found.");

                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent($"Failed to get the data with {payloadId}.")  // if used interpolation string we need to pass double braces
                };
            }
            var requireSign = req.RequireSign();

            switch (newPayload.PayloadDataType)
            {
                case PayloadDataType.BlobAttachment:
                    return await GetBlobAttachmentPayloadData(newPayload, payloadAttachmentRepository, requireSign, signProvider, log).ConfigureAwait(false);
                case PayloadDataType.InlineMetadata:
                    return GetInlineMetadata(newPayload, requireSign, signProvider, log);
                case PayloadDataType.BinaryBlobAttachment:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(newPayload.PayloadDataType.ToString());
            }
        }
    }

    private static HttpResponseMessage GetInlineMetadata(PayloadWithData newPayload, bool requireSign, ISignProvider signProvider, ILogger log)
    {
        var json = JsonConvert.SerializeObject(newPayload.Metadata);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                MediaTypeNames.Application.Json)
        };

        if (requireSign)
        {
            var data = Encoding.UTF8.GetBytes(json);
            var signature = signProvider.Sign(data);
            response.Headers.Add(Constants.SignatureHttpHeader, signature);
        }

        return response;
    }

    private static async Task<HttpResponseMessage> GetBlobAttachmentPayloadData(
        PayloadWithData newPayload,
        IPayloadAttachmentRepository payloadAttachmentRepository,
        bool requireSign,
        ISignProvider signProvider,
        ILogger log)
    {
        if (newPayload.Metadata == null)
        {
            log.LogError($"Blob attachment is not set on payload id {newPayload.Id}.");

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        var blobAttachment = newPayload.Metadata.ToObject<BlobAttachmentReference>();

        if (blobAttachment == null)
        {
            log.LogError($"Blob attachment is not set on payload id {newPayload.Id}.");

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        if (await payloadAttachmentRepository.ExistsAsync(blobAttachment))
        {
            var stream = await payloadAttachmentRepository.GetAttachmentAsync(blobAttachment); // HttpResponseMessage will dispose the stream through underlying StreamContent

            var content = stream.ToStreamContent(blobAttachment.AttachmentContentType, requireSign, signProvider);

            if (!string.IsNullOrEmpty(blobAttachment.AttachmentContentEncoding))
            {
                log.LogInformation($"Found original Content-Encoding header '{blobAttachment.AttachmentContentEncoding}' adding to response headers");
                content.Headers.Add("Content-Encoding", blobAttachment.AttachmentContentEncoding);
            }

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };

            return responseMessage;
        }
        else
        {
            log.LogError($"Blob attachment is not found in blob for payload id {newPayload.Id}.");

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }
}

