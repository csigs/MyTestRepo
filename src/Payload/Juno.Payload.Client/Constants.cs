using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Juno.Payload.Client.IntegrationTests")]
namespace Juno.Payload.Client
{
    internal class Constants
    {
        internal const string RequireSignQueryParamName = "sign";
        internal const string SignatureHttpHeader = "Signature";
    }
}
