using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EV2.Templates.Fluent;

namespace Juno.Payload.EV2.ServiceResourceDefinitions
{
    internal class PayloadServiceDefinition : IServiceResourceDefinitionConfiguration
    {
        public void Configure(IServiceResourceDefinitionBuilder builder, EV2TemplateGeneratorContext context)
        {
            builder
                .HasName("PayloadServiceDefinition")
                .IsComposedOfArm(a =>
                    a
                    .TemplateAt("ARMTemplates\\azuredeploy.json")
                    .ParameterFileAt($"Parameters\\azuredeploy.parameters.{context.ApplicationEnvironment.ToString().ToLower()}.json"));
        }
    }
}
