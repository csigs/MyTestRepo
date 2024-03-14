namespace Juno.Payload.Handoff
{
    using System;
    using Juno.Common.Contracts;
    using Juno.Common.Contracts.Eol;

    //TODO: move this project to SW Handoff repository if needed
    public class HandoffPayloadMetadata
    {
        public Guid PayloadId { get; set; }

        public DateTime PayloadCreatedOn { get; set; }

        public BuildMetadata Build { get; set; }

        public BranchMetadata Branch { get; set; }

        public FullEolMapMetadata EolMapMetadata { get; set; }
    }
}
