// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreatePayloadV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Create a payload by registering it payload document to database.
//   The payload document is any format of document. Its format depends on the feeder and consumer. 
//   i.e. Handoff payload will have Lcg to Language mapping while handback payload will have full mapping.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using FunctionExtensions.DependencyInjection;
using Juno.Payload.Dto;
using Juno.Payload.Service.Extensions;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.SignProviders;

/// <summary>
/// Function to create a payload document in database with given metadata. It returns new id generated.
/// </summary>
public static class CreatePayloadV2
{
    private const string OpId = nameof(Constants.CreatePayloadV2);
    private const string FnName = Constants.CreatePayloadV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}?}}";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "Payload" },
    //    Description = $"{FnName}: Creates a payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiRequestBody(
    //    MediaTypeNames.Application.Json,
    //    typeof(PayloadDataStorageTypeDto),
    //    Required = true,
    //    Description = "TBD")]
    //[OpenApiResponseWithBody(
    //    HttpStatusCode.OK,
    //    MediaTypeNames.Application.Json,
    //    bodyType: typeof(IIdentifiableObject),
    //    Description = "A payload is successfully created.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
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
            Requires.NotNull(req, nameof(req));
            Requires.NotNull(payloadRepository, nameof(payloadRepository));
            Requires.NotNull(payloadAttachmentRepository, nameof(payloadAttachmentRepository));
            Requires.NotNull(payloadRepository, nameof(payloadRepository));
            Requires.NotNull(log, nameof(log));

            log.LogDebug($"{FnName}: HTTP trigger to create a payload with partitionKey: '{partitionKey}' and optional id: '{id}' started at {DateTime.Now.ToUniversalTime()}");

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                log.LogWarning($"{FnName}: {nameof(partitionKey)} is not provided.");

                return new BadRequestObjectResult("Please pass a partitionKey to create a payload or call with POST api/v2/payloads/{partitionKey}/{*id}.");
            }

            var newPayloadId = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(id) && !Guid.TryParse(id, out newPayloadId))
            {
                log.LogWarning($"{FnName}: {nameof(id)} is not provided correctly non empty guid is expected.");

                // BUG: Payload id is optional. If a valid one isn't provided, just assign a new one, instead of returning an error?
                return new BadRequestObjectResult("Please pass a id to create a payload or call with POST api/v2/payloads/{partitionKey}/{*id}.");
            }

            if (!req.TryGetCategory(FnName, log, out var category, out var badRequestObjectResult))
            {
                return badRequestObjectResult;
            }

            var payload = newPayloadId == Guid.Empty
                ? PayloadWithData.CreateNew(category: category, partitionKey: partitionKey, payloadVersion: PayloadVersion.V2)
                : PayloadWithData.CreateNew(newPayloadId, category: category, partitionKey: partitionKey, payloadVersion: PayloadVersion.V2);

            Verify.Operation(
                !string.IsNullOrWhiteSpace(payload.Id) && payload.Id != Guid.Empty.ToString(),
                "Payload Id can't be empty after payload object creation");

            var responseContract = new
            {
                payload.Id
            };

            if (req.ContainsWithPayloadData())
            {
                if (!req.TryGetPayloadDataType(FnName, log, out var payloadDataTypeDto, out badRequestObjectResult))
                {
                    return badRequestObjectResult;
                }

                var payloadDataType = payloadDataTypeDto.ToDomain();
                var blobAttachmentReference = new BlobAttachmentReference(
                    BlobAttachmentId.CreateFor(payload),
                    GetContentTypeFor(payloadDataType, req, log));
                payload.UpdatePayloadInlineMetadataWith(payloadDataType, blobAttachmentReference);

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
                var doc = await payloadRepository.CreateItemAsync(payload);

                if (doc.Id != responseContract.Id.ToString())
                {
                    throw new InvalidOperationException($"Returned document id {doc.Id} was different from requested payload id {responseContract.Id}");
                }

                return new OkObjectResult(responseContract);
            }
            else
            {
                log.LogInformation($"{FnName}: New payload registered with {responseContract.Id}.");
                log.LogInformation($"{FnName}: HTTP trigger to create a payload finished at {DateTime.Now.ToUniversalTime()}");

                var doc = await payloadRepository.CreateItemAsync(payload);

                if (doc.Id != responseContract.Id.ToString())
                {
                    throw new InvalidOperationException($"Returned document id {doc.Id} was different from requested payload id {responseContract.Id}");
                }

                return new OkObjectResult(responseContract);
            }
        }
    }

    private static ContentType GetContentTypeFor(PayloadDataType payloadDataType, HttpRequest req, ILogger logger)
    {
        switch (payloadDataType)
        {
            case PayloadDataType.BlobAttachment:
            case PayloadDataType.InlineMetadata:
                if (!req.ContentType.Contains(MediaTypeNames.Application.Json))
                {
                    logger.LogWarning($"Requested payload content type is not application/json. Supplied type is:{req.ContentType}");
                }
                return new ContentType(MediaTypeNames.Application.Json);
            case PayloadDataType.BinaryBlobAttachment:
                return new ContentType(req.ContentType);
            default:
                throw new ArgumentOutOfRangeException(payloadDataType.ToString());
        }
    }
}
