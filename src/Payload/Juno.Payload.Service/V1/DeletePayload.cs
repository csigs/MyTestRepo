// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeletePayload.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Delete a payload document from database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V1;

using System;
using System.Diagnostics;
using System.Net;
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
using Juno.Common.DataReference;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;

/// <summary>
/// Delete a payload document by id with HttpTrigger.
/// </summary>
public static class DeletePayload
{
    private const string OpId = nameof(Constants.DeletePayloadV1);
    private const string FnName = Constants.DeletePayloadV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "Payload" },
        Description = $"{FnName}: Deletes a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "A specified payload is successfully deleted.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadRepository payloadRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] IPayloadAttachmentRepository payloadAttachmentRepository,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadDataRefRepository payloadDataReferencesRepository,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to delete a payload started at {DateTime.Now.ToUniversalTime()}.");

            // TODO: Consolidate input validations
            if (string.IsNullOrWhiteSpace(id))
            {
                log.LogWarning($"{FnName}: Payload Id is not provided.");

                return new BadRequestObjectResult("Please pass a payload id to delete in query string or call with DELETE api/v1/payloads/{{id}}.");
            }

            if (!Guid.TryParse(id, out var payloadIdGuid))
            {
                log.LogWarning($"{FnName}: Payload Id provided can't be parse out.");

                return new BadRequestObjectResult("Please pass a payload id to delete in query string or call with DELETE api/v1/payloads/{{id}}.");
            }

            log.LogInformation($"{FnName}: Deleting payload with {id} started at {DateTime.Now.ToUniversalTime()}");

            var partitionKey = PayloadWithData.DefaultPartitionKeyValue; // partition key for payload v1

            var payload = await payloadRepository.GetItemAsync<PayloadWithData>(id, partitionKey);

            if (payload != null && payload.TryGetBlobAttachmentReference(out var blobAttachmentReference))
            {
                if (await payloadAttachmentRepository.ExistsAsync(blobAttachmentReference))
                {
                    log.LogInformation($"Deleting blob attachment {blobAttachmentReference.AttachmentId} for payload id {payload.Id} partition key {partitionKey}");

                    await payloadAttachmentRepository.DeleteAttachmentAsync(blobAttachmentReference);
                }
            }

            await payloadMetadataRepository.DeleteItemsSteamAsync<PayloadMetadata<JObject>>(
                item => item.PayloadId == payloadIdGuid, payloadIdGuid.ToString()); // payload id is also partition key for metadata collection

            await payloadDataReferencesRepository.DeleteItemsSteamAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(
                item => item.PayloadId == id, payloadIdGuid.ToString()); // payload id is also partition key for data references collection

            await payloadRepository.DeleteItemsSteamAsync<PayloadWithData>(item => item.Id == id, partitionKey);

            log.LogInformation($"{FnName}: HTTP trigger to delete a payload finished at {DateTime.Now.ToUniversalTime()}");

            return new OkObjectResult($"The payload with {id} is deleted.");
        }
    }
}
