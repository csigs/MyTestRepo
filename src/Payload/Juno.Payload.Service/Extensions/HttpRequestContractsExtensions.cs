namespace Juno.Payload.Service.Extensions
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Localization.SignProviders;
    
    using PayloadDataTypeContract = Juno.Payload.Dto.PayloadDataStorageTypeDto;

    public static class HttpRequestContractsExtensions
    {
        public const string CategoryQueryParamName = "category";

        public const string PayloadDataTypeQueryParamName = "payloadDataType";

        public const string WithPayloadDataQueryParamName = "withPayloadData";

        public const string UseTransactionQueryParamName = "useTransaction";

        public static bool GetUseTransactionSafely(this HttpRequest req)
        {
            Requires.NotNull(req, nameof(req));

            if (req.Query.ContainsKey(UseTransactionQueryParamName))
            {
                return req.Query[UseTransactionQueryParamName].ToString().ToLower() == "true";
            }
            return false;
        }


        public static bool TryGetPayloadDataType(
            this HttpRequest req,
            string functionName,
            ILogger log,
            out PayloadDataTypeContract payloadDataType,
            out BadRequestObjectResult badRequestError)
        {
            Requires.NotNull(req, nameof(req));
            Requires.NotNull(functionName, nameof(functionName));
            Requires.NotNull(log, nameof(log));

            if (!req.Query.ContainsKey(Constants.PayloadDataTypeParamName))
            {
                log.LogWarning($"{functionName}: {Constants.PayloadDataTypeParamName} need to be supplied as query param.");
                badRequestError = new BadRequestObjectResult($"{Constants.PayloadDataTypeParamName} need to be supplied as query param.");
                payloadDataType = default;

                return false;
            }

            var payloadDataTypeStr = req.Query[Constants.PayloadDataTypeParamName];

            if (string.IsNullOrWhiteSpace(payloadDataTypeStr))
            {
                log.LogWarning($"{functionName}: {Constants.PayloadDataTypeParamName} can't be null or whitespace.");
                badRequestError = new BadRequestObjectResult($"{Constants.PayloadDataTypeParamName} can't be null or whitespace.");
                payloadDataType = default;

                return false;
            }

            if (!Enum.TryParse(payloadDataTypeStr, true, out payloadDataType))
            {
                log.LogWarning($"{functionName}: Can't parse {Constants.PayloadDataTypeParamName} from value {payloadDataTypeStr}.");
                badRequestError = new BadRequestObjectResult($"Can't parse {Constants.PayloadDataTypeParamName} from value {payloadDataTypeStr}.");
                payloadDataType = default;

                return false;
            }

            badRequestError = null;
            return true;
        }

        // TODO: Fix the error message that's mixing v1 and v2 usage
        public static bool TryGetCategory(
            this HttpRequest req,
            string functionName,
            ILogger log,
            out string category,
            out BadRequestObjectResult badRequestError,
            bool defaultCategoryIfMissing = true)
        {
            category = "Default";

            if (req.Query.ContainsKey(Constants.CategoryParamName))
            {
                category = req.Query[Constants.CategoryParamName];

                if (string.IsNullOrWhiteSpace(category))
                {
                    log.LogWarning($"{functionName}: Can't parse {Constants.CategoryParamName} from value {category}.");
                    badRequestError = new BadRequestObjectResult("Passed category value can't be null or whitespace. Use with POST api/v2/payloads/{partitionKey}?category={category}.");

                    return false;
                }

                if (category.Length > 100)
                {
                    log.LogWarning($"{functionName}: {Constants.CategoryParamName} with value {category} is longer than 100 characters.");
                    badRequestError = new BadRequestObjectResult("Passed category value can't be longer than 100 characters. Use with POST api/v2/payloads/{partitionKey}?category={category}.");

                    return false;
                }
            }
            else if (!defaultCategoryIfMissing)
            {
                badRequestError = new BadRequestObjectResult("Passed category can't be found. Use with POST api/v2/payloads/{partitionKey}?category={category}.");

                return false;
            }

            badRequestError = null;
            return true;
        }

        public static bool ContainsWithPayloadData(this HttpRequest req)
        {
            if (req.Query.ContainsKey(Constants.WithPayloadDataParamName))
            {
                string withPayloadDataValue = req.Query[Constants.WithPayloadDataParamName];
                if (string.IsNullOrWhiteSpace(withPayloadDataValue))
                {
                    return false;
                }

                var value = withPayloadDataValue.Trim().ToLower();

                return value == "true" || value == "1";
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Try to get signature from <paramref name="req"/> header.
        /// </summary>
        /// <param name="req">Http request.</param>
        /// <param name="signature">Signature string.</param>
        /// <returns></returns>
        public static bool TryGetSignature(this HttpRequest req, out string signature)
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            signature = null;
            if (req.Headers.TryGetValue(Constants.SignatureHttpHeader, out var value))
            {
                signature = value;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check if sign is required by looking at query param.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <returns></returns>
        public static bool RequireSign(this HttpRequest req)
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (req.Query.ContainsKey(Constants.RequireSignQueryParamName))
            {
                string requireSign = req.Query[Constants.RequireSignQueryParamName];
                if (string.IsNullOrWhiteSpace(requireSign))
                {
                    return false;
                }

                var value = requireSign.Trim().ToLower();

                return value == "true" || value == "1";
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Verify signature with <paramref name="req"/> body and return new stream of its copy.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="signProvider"></param>
        /// <returns>Copy of req.Body <see cref="Stream"/>.</returns>
        /// <exception cref="CryptographicException"></exception>
        public static Stream VerifySignature(this HttpRequest req, ISignProvider signProvider)
        {
            if (signProvider == null)
            {
                return null;
            }

            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            var memoryStream = new MemoryStream();
            req.Body.CopyTo(memoryStream);

            var data = memoryStream.ToArray();
            memoryStream.Position = 0;

            VerifySignature(req, signProvider, data);

            return memoryStream;
        }

        /// <summary>
        /// Verify signature for string <paramref name="data"/>.
        /// </summary>
        /// <param name="req">HttpRequest that has signature header.</param>
        /// <param name="signProvider">Sign provider.</param>
        /// <param name="data">Data to verify signature.</param>
        /// <exception cref="CryptographicException"></exception>
        public static void VerifySignature(this HttpRequest req, ISignProvider signProvider, string data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var dataBytes = Encoding.UTF8.GetBytes(data);

            VerifySignature(req, signProvider, dataBytes);
        }

        /// <summary>
        ///  Verify signature for bytes <paramref name="data"/>.
        /// </summary>
        /// <param name="req">HttpRequest that has signature header.</param>
        /// <param name="signProvider">Sign provider.</param>
        /// <param name="data">Data to verify signature.</param>
        /// <exception cref="CryptographicException"></exception>
        public static void VerifySignature(this HttpRequest req, ISignProvider signProvider, byte[] data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!req.TryGetSignature(out var signature))
            {
                throw new CryptographicException("Sign requested but no signature header found.");
            }

            if (!signProvider.VerifyData(data, signature))
            {
                throw new CryptographicException("Signature is not verified. It is possible the data is modified.");
            }
        }
    }
}
