// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeletePayloadV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Delete a payload document from database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;

using FunctionExtensions.DependencyInjection;
using Juno.Common.DataReference;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class DeletePayloadV2
{
    private const string OpId = nameof(Constants.DeletePayloadV2);
    private const string FnName = Constants.DeletePayloadV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "Payload" },
    //    Description = $"{FnName}: Deletes a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(Constants.PartitionKeyParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "A specified payload is successfully deleted.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route)] HttpRequest req,
        string partitionKey,
        string id,
        [Inject] IPayloadRepository payloadRepository,
        [Inject] IPayloadAttachmentRepository payloadAttachmentRepository,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] IPayloadDataRefRepository payloadDataReferencesRepository,
        CancellationToken cancellationToken,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            //TODO: implement deletion on all other resources - blob attachment, metadata container and data references container

            Trace.TraceInformation($"{FnName}: HTTP trigger to delete a payload started at {DateTime.Now.ToUniversalTime()}.");

            // TODO: Consolidate input validation
            if (string.IsNullOrWhiteSpace(id))
            {
                log.LogWarning($"{FnName}: Payload id is not provided.");

                return new BadRequestObjectResult("Please pass a payload id to delete payload or call with api/v2/payloads/{partitionKey}/{id}.");
            }

            if (!Guid.TryParse(id, out var payloadIdGuid))
            {
                log.LogWarning($"{FnName}: Payload id guid is not provided.");

                return new BadRequestObjectResult("Please pass a payload id to delete payload or call with api/v2/payloads/{partitionKey}/{id}.");
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                log.LogWarning($"{FnName}: {nameof(partitionKey)} is not provided.");

                return new BadRequestObjectResult("Please pass a payload id and partitionKey to delete a payload or call with DELETE api/v2/payloads/{partitionKey}/{id}.");
            }

            log.LogInformation($"{FnName}: Deleting payload with {id} with partition key {partitionKey} started at {DateTime.Now.ToUniversalTime()}");

            //TODO: implement attachment delete if exist

            var payload = await payloadRepository.GetItemAsync<PayloadWithData>(id, partitionKey);

            if (payload != null && payload.TryGetBlobAttachmentReference(out var blobAttachmentReference))
            {
                if (await payloadAttachmentRepository.ExistsAsync(blobAttachmentReference))
                {
                    log.LogInformation($"Deleting blob attachment {blobAttachmentReference.AttachmentId} for payload id {payload.Id} partition key {partitionKey}");

                    await payloadAttachmentRepository.DeleteAttachmentAsync(blobAttachmentReference);
                }
            }

            await payloadMetadataRepository.DeleteItemsSteamAsync<PayloadMetadataCollectionItem<JObject>>(
                item => item.PayloadId == payloadIdGuid, payloadIdGuid.ToString(), cancellationToken); // payload id is also partition key for metadata collection

            await payloadDataReferencesRepository.DeleteItemsSteamAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(
                item => item.PayloadId == id, payloadIdGuid.ToString(), cancellationToken); // payload id is also partition key for data references collection

            await payloadRepository.DeleteItemsSteamAsync<PayloadWithData>(item => item.Id == id, partitionKey, cancellationToken);

            log.LogInformation($"{FnName}: HTTP trigger to delete a payload finished at {DateTime.Now.ToUniversalTime()}");

            return new OkObjectResult($"The payload with {id} and partitionKey {partitionKey} is deleted.");
        }
    }
}
