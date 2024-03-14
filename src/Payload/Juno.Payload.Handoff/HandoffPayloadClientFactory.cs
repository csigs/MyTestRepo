namespace Juno.Payload.Handoff
{
    using System;
    using Juno.Common.Contracts.Eol;
    using Juno.Payload.Client;
    using Juno.Payload.Client.Configuration;

    //[Obsolete("This is not needed anymore with V2")]
    public class HandoffPayloadClientFactory : PayloadClientFactory<HandoffPayloadMetadata, LcgFullEolMap>
    {
        public HandoffPayloadClientFactory(PayloadClientConfig payloadClientConfig) : base(payloadClientConfig)
        {
        }
    }
}
