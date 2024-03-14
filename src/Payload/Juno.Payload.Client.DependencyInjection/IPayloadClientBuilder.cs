using Juno.Payload.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juno.Payload.Client.DependencyInjection
{
    public interface IPayloadHttpClientBuilder : IHttpClientBuilder
    {
        IPayloadHttpClientBuilder UsePayloadClientConfig(PayloadClientConfig payloadClientConfig);
    }
}
