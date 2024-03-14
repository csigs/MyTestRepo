namespace Juno.Payload.Service.Extensions
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class StringExtensions
    {
        public static string ReplaceEncoding(this string source, string replacement)
        {
            return source.Replace("encoding=\"utf-16\"", replacement);
        }

        public static Stream GetStream(this string resourceString)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw new ArgumentException($"{nameof(resourceString)} cannot be null or empty");
            }

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, System.Text.Encoding.UTF8); // don't dispose as it closes underlying stream
            writer.Write(resourceString.ReplaceEncoding("encoding=\"utf-8\""));
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
