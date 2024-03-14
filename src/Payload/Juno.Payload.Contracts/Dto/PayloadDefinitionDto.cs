using System;
using System.Collections.Generic;
using System.Text;
using Juno.Payload.Contracts.Dto;

namespace Juno.Payload.Dto
{
    public class PayloadDefinitionDto
    {
        public Guid Id { get; set; }

        public string Category { get; set; }

        public PayloadVersionDto PayloadVersion { get; set; }
    }
}
