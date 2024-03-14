using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Juno.Payload.Contracts.Dto.Pagination;
using Microsoft.Localization.SignProviders;
using Newtonsoft.Json;

namespace Juno.Payload.Client.Extensions
{
    internal static class HttpContentExtensions
    {
        //internal static HttpContent ConvertToJsonContent(this object model, ISignProvider signProvider)
        //{
        //    var json = model.SerializeObject();

        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    content.AddSignatureHeader(signProvider, json);

        //    return content;
        //}

        internal static HttpRequestMessage ToHttpRequestMessageWithJsonContent(this object model, HttpMethod httpMethod, string functionUrl, ISignProvider signProvider)
        {
            var request = new HttpRequestMessage(httpMethod, functionUrl);
            var json = JsonConvert.SerializeObject(model);

            request.AddSignatureHeader(signProvider, json);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json"); ;

            return request;
        }

        internal static void AddSignatureHeader(this HttpRequestMessage request, ISignProvider signProvider, object data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (data == null)
            {
                return;
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var textData = JsonConvert.SerializeObject(data);

            request.AddSignatureHeader(signProvider, textData);
        }

        internal static void AddSignatureHeader(this HttpRequestMessage request, ISignProvider signProvider, string data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var bytesData = Encoding.UTF8.GetBytes(data);

            request.AddSignatureHeader(signProvider, bytesData);
        }

        internal static void AddSignatureHeader(this HttpRequestMessage request, ISignProvider signProvider, byte[] data)
        {
            if (signProvider == null)
            {
                return;
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var signature = signProvider.Sign(data);
            request.Headers.Add(Constants.SignatureHttpHeader, signature);
        }
    }
}
