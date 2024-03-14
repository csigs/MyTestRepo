using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EV2.Templates.Fluent;


namespace Juno.Payload.EV2.ServiceResourceGroupDefinitions
{
    internal class PayloadServiceGroupDefinition : IServiceResourceGroupDefinitionConfiguration
    {
        public void Configure(IServiceResourceGroupDefinitionBuilder builder, EV2TemplateGeneratorContext context)
        {
            builder
                .HasName("PayloadServiceGroupDefinition")
                .AddServiceResourceDefinition(new PayloadServiceDefinition());
        }

       
    }
}
