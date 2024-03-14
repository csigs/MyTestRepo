namespace Juno.Payload.Service.V1;
using System;
using System.Diagnostics;
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

public static class GetPayload
{
    private const string OpId = nameof(Constants.GetPayloadV1);
    private const string FnName = Constants.GetPayloadV1;
    private const string Route = $"v1/payloads/{{{Constants.PayloadIdParamName}}}";

    [OpenApiOperation(
        operationId: OpId,
        tags: new[] { "Payload" },
        Description = $"{FnName}: Gets a specified payload",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(Constants.PayloadIdParamName, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        MediaTypeNames.Application.Json,
        bodyType: typeof(PayloadWithData),
        Description = "A specified payload is successfully retrieved")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Required parameters are not specified.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "A specified payload is not found.")]
    [FunctionName(FnName)]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        string id,
        [Inject] IPayloadRepository payloadRepository,
        [Inject] IPayloadMetricEmitter metricEmitter,
        [Inject] ISignProvider signProvider,
        ILogger log)
    {
        using (var timedOperation = metricEmitter.BeginTimedOperation(FnName))
        {
            Trace.TraceInformation($"{FnName}: HTTP trigger to get a payload skeleton.");

            if (!id.IsValidPayloadId(log, FnName, out var payloadId, out var badRequestErrorMessageResult))
            {
                return badRequestErrorMessageResult;
            }

            var requireSign = req.RequireSign();

            //TODO: TMP fix for UAT with hardcoded partition key.

            var payload = await payloadRepository.GetItemAsync<PayloadWithData>(
                payloadId.ToString(),
                PayloadWithData.DefaultPartitionKeyValue);

            if (payload == null)
            {
                log.LogInformation($"{FnName}: The document for {payloadId} is not found.");

                return new NotFoundObjectResult($"Payload id {payloadId} was not found.");
            }

            return payload.ToOkObjectResult(req, requireSign, signProvider);
        }
    }
}
