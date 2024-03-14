namespace Juno.Payload.EV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class ServiceModel : IServiceModelConfiguration
    {
        public void Configure(IServiceModelBuilder builder, EV2TemplateGeneratorContext context)
        {
            builder
                .WithServiceMetadata(sm =>
                {
                    sm
                    .ForService(new Guid("41200fb5-60f6-4492-9dae-bd23d7d85aaa"))
                    .UnderServiceGroup("Microsoft.CE.ICE.Payload")
                    .ForEnvironment(context.ApplicationEnvironment.ToString());
                })
                .AddServiceResourceGroupDefinition(new PayloadServiceGroupDefinition())
                .AddServiceResourceGroup(new PayloadServiceGroup());
        }
    }
}
