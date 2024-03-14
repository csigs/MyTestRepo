namespace Juno.Payload.Service.Extensions;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Net.Http.Headers;

using Microsoft.Localization.SignProviders;

public static class StreamExtensions
{
    public static StreamContent ToStreamContent(this Stream stream, string mediaType, bool requireSign, ISignProvider signProvider)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var targetStream = stream;
        var signature = string.Empty;

        if (requireSign)
        {
            if (signProvider == null)
            {
                throw new ArgumentNullException(nameof(signProvider));
            }

            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            var data = memoryStream.ToArray();
            memoryStream.Position = 0;
            targetStream = memoryStream;

            signature = signProvider.Sign(data);
        }

        var content = new StreamContent(targetStream, 4096);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType ?? MediaTypeNames.Application.Octet);

        if (requireSign)
        {
            content.Headers.Add(Constants.SignatureHttpHeader, signature);
        }

        return content;
    }
}
