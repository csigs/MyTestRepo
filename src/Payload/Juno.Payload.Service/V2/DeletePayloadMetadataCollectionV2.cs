// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeletePayloadMetadataCollectionV2.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Delete a payload metadata from database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.V2;

using System;
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
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;

/// <summary>
/// Delete a payload metadata collection by payloadId with HttpTrigger.
/// </summary>
public static class DeletePayloadMetadataCollectionV2
{
    private const string OpId = nameof(Constants.DeletePayloadMetadataV2);
    private const string FnName = Constants.DeletePayloadMetadataV2;
    private const string Route = $"v2/payloads/{{{Constants.PartitionKeyParamName}}}/{{{Constants.PayloadIdParamName}}}/metadata";

    //[OpenApiOperation(
    //    operationId: OpId,
    //    tags: new[] { "PayloadMetadataCollection" },
    //    Description = $"{FnName}: Deletes a metadata collection of a specified payload",
    //    Visibility = OpenApiVisibilityType.Important)]
    //[OpenApiSecurity(Constants.PayloadAuthSchemeName, SecuritySchemeType.OAuth2, Flows = typeof(PayloadSecurityFlows))]
    //[OpenApiParameter(
    //    Constants.PartitionKeyParamName,
    //    In = ParameterLocation.Path,
    //    Required = true,
    //    Type = typeof(string),
    //    Description = "Payload id is used internally as a partition key while a non-empty partition key must still be specified in the path.")]
    //[OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    //[OpenApiResponseWithoutBody(HttpStatusCode.OK, Description = "Metadata of a specified payload are successfully deleted.")]
    //[OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route)] HttpRequest req,
        string partitionkey,
        string id,
        [Inject] IPayloadMetadataRepository payloadMetadataRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            var functionName = Constants.DeletePayloadMetadataV2;

            log.LogInformation($"{functionName}: HTTP trigger to delete a payload metadata for payload with partition key {partitionkey} and payload id {id} started at {DateTime.Now.ToUniversalTime()}.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            await payloadMetadataRepository.DeleteItemsSteamAsync<PayloadMetadataCollectionItem<JObject>>(b => b.PayloadId.ToString() == id, id.ToString());

            log.LogInformation($"{functionName}: HTTP trigger to delete a payload metadata for payload with partition key {partitionkey} and payload id {id} finished at {DateTime.Now.ToUniversalTime()}");

            return new OkObjectResult($"The payload metadata for payload with partition key {partitionkey} and payload id {id} is deleted.");
        }
    }
}
