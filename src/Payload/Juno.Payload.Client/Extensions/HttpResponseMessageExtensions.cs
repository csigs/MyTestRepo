namespace Juno.Payload.Client.Extensions
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    using Microsoft.Localization.SignProviders;

    internal static class HttpResponseMessageExtensions
    {
        public static async Task<TData> ReadAsJsonAsync<TData>(this HttpResponseMessage httpResponseMessage, ISignProvider signProvider)
        {
            if (httpResponseMessage is null)
            {
                throw new ArgumentNullException(nameof(httpResponseMessage));
            }

            httpResponseMessage.EnsureSuccessStatusCode();
            var payloadDataJson = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            httpResponseMessage.VerifySignature(signProvider, payloadDataJson);

            return JsonConvert.DeserializeObject<TData>(payloadDataJson);
        }

        /// <summary>
        /// Try to get signature from <paramref name="req"/> header.
        /// </summary>
        /// <param name="req">Http request.</param>
        /// <param name="signature">Signature string.</param>
        /// <returns></returns>
        public static bool TryGetSignature(this HttpResponseMessage res, out string signature)
        {
            if (res == null)
            {
                throw new ArgumentNullException(nameof(res));
            }

            signature = null;
            if (res.Headers.TryGetValues(Constants.SignatureHttpHeader, out var value))
            {
                signature = value.First();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Verify signature for string <paramref name="data"/>.
        /// </summary>
        /// <param name="req">HttpRequest that has signature header.</param>
        /// <param name="signProvider">Sign provider.</param>
        /// <param name="data">Data to verify signature.</param>
        /// <exception cref="CryptographicException"></exception>
        public static void VerifySignature(this HttpResponseMessage res, ISignProvider signProvider, string data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (res == null)
            {
                throw new ArgumentNullException(nameof(res));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var dataBytes = Encoding.UTF8.GetBytes(data);

            VerifySignature(res, signProvider, dataBytes);
        }

        /// <summary>
        ///  Verify signature for bytes <paramref name="data"/>.
        /// </summary>
        /// <param name="req">HttpRequest that has signature header.</param>
        /// <param name="signProvider">Sign provider.</param>
        /// <param name="data">Data to verify signature.</param>
        /// <exception cref="CryptographicException"></exception>
        public static void VerifySignature(this HttpResponseMessage res, ISignProvider signProvider, byte[] data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (res == null)
            {
                throw new ArgumentNullException(nameof(res));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!res.TryGetSignature(out var signature))
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

