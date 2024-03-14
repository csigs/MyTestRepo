namespace Juno.Payload.Service;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Juno.Payload.Service.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Localization.SignProviders;
using Newtonsoft.Json;

internal static class InputValidationHelper
{
    internal static bool IsValidPartitionKey(
        string partitionKey,
        ILogger logger,
        string functionName,
        string resultMessage,
        out BadRequestErrorMessageResult badRequestErrorMessageResult)
    {
        throw new NotImplementedException();
    }

    internal static bool IsValidPayloadId(
        this string payloadIdString,
        ILogger logger,
        string functionName,
        out Guid payloadId,
        out BadRequestErrorMessageResult badRequestErrorMessageResult)
    {
        var errorMessage = "Specify a payload id. A valid payload id is a non-empty GUID value.";

        return payloadIdString.IsValidPayloadId(
            logger,
            functionName,
            errorMessage,
            errorMessage,
            out payloadId,
            out badRequestErrorMessageResult);
    }

    internal static bool IsValidPayloadId(
        this string payloadIdString,
        ILogger logger,
        string functionName,
        string logMessage,
        string resultMessage,
        out Guid payloadId,
        out BadRequestErrorMessageResult badRequestErrorMessageResult)
    {
        functionName = string.IsNullOrWhiteSpace(functionName) ? functionName.Trim() : "(unspecified function name)";

        if (Guid.TryParse(payloadIdString, out payloadId) && payloadId != Guid.Empty)
        {
            badRequestErrorMessageResult = null;

            return true;
        }
        else
        {
            logger.LogError($"{functionName}: {logMessage}");
            badRequestErrorMessageResult = new BadRequestErrorMessageResult(resultMessage);

            return false;
        }
    }

    internal static async Task<(bool ParseResult, T DeserializedRequestBody, BadRequestErrorMessageResult BadRequestErrorMessageResult)> TryParseRequestBodyAsync<T>(
        this Stream requestBody,
        ILogger logger,
        string functionName,
        string logMessage,
        string resultMessage,
        HttpRequest req,
        bool requireSign,
        ISignProvider signProvider)
    {
        functionName = string.IsNullOrWhiteSpace(functionName) ? functionName.Trim() : "(unspecified function name)";

        var requestBodyText = string.Empty;

        try
        {
            requestBodyText = await (new StreamReader(requestBody)).ReadToEndAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(requestBodyText))
            {
                logger.LogError($"{functionName}: {logMessage}");

                return (false, default, new BadRequestErrorMessageResult(resultMessage));
            }

            if (requireSign)
            {
                req.VerifySignature(signProvider, requestBodyText);
            }

            return (
                true,
                JsonConvert.DeserializeObject<T>(requestBodyText),
                null);
        }
        catch (Exception e)
        {
            var errorMessage = string.Join(
                Environment.NewLine,
                $"{functionName}: ",
                logMessage,
                requestBodyText,
                e.Message);

            logger.LogError(errorMessage);

            return (false, default, new BadRequestErrorMessageResult(resultMessage));
        }
    }
}
