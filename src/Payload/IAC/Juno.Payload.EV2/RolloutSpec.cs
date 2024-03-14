namespace Juno.Payload.EV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class RolloutSpec : IRolloutSpecConfiguration
    {
        public void Configure(IRolloutSpecBuilder builder,EV2TemplateGeneratorContext context)
        {
            builder
                .WithMetadata(rm =>
                {
                    rm
                    .HasName("Payload Main")
                    .ForServiceModelAt("ServiceModel.json")
                    .WithType("Major")
                    .WithNotification(n => n.AddEmailNotification(en => en.To("gbxco@microsoft.com")))
                    .ForBuildSource("version.txt");
                })
                .AddOrchestratedStep(o =>
                {
                    o
                    .HasName("DeployPayload")
                    .ForTarget(new PayloadService())
                    .PerformAcions(new[] { "deploy" });
                });
        }
    }
}
