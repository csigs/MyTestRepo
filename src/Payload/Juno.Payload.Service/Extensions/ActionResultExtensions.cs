namespace Juno.Payload.Service.Extensions;

using System;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Microsoft.Localization.SignProviders;

public static class ActionResultExtensions
{
    public static IActionResult ToOkObjectResult(this object data, HttpRequest req, bool requireSign, ISignProvider signProvider)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (requireSign)
        {
            if (signProvider == null)
            {
                throw new ArgumentNullException(nameof(signProvider), $"Sign requested but {nameof(signProvider)} not provided.");
            }

            var signData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            var signature = signProvider.Sign(signData);

            if (req.HttpContext.Response.Headers.ContainsKey(Constants.SignatureHttpHeader))
            {
                req.HttpContext.Response.Headers.Remove(Constants.SignatureHttpHeader);
            }

            req.HttpContext.Response.Headers.Add(Constants.SignatureHttpHeader, signature);
        }

        return new OkObjectResult(data);
    }

    public static IActionResult ToContentResult(this string data, HttpRequest req, string contentType, bool requireSign, ISignProvider signProvider)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (requireSign)
        {
            if (signProvider == null)
            {
                throw new ArgumentNullException(nameof(signProvider), $"Sign requested but {nameof(signProvider)} not provided.");
            }

            var signData = Encoding.UTF8.GetBytes(data);
            var signature = signProvider.Sign(signData);

            req.HttpContext.Response.Headers.Add(Constants.SignatureHttpHeader, signature);
        }

        return new ContentResult()
        {
            Content = data,
            ContentType = contentType
        };
    }
}
