namespace Juno.Payload.Client.Extensions
{
    using System;

    using Juno.Payload.Client.Configuration;

    public static class PayloadClientOptionsExtensions
    {
        public static PayloadClientConfig AsPayloadConfig(this PayloadClientOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new PayloadClientConfig(options.ServiceUri, options.MSIScope);
        }
    }
}
