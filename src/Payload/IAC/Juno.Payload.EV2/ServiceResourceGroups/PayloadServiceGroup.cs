using System.Reflection.PortableExecutable;
using Juno.Payload.EV2;

namespace Juno.Payload.EV2.ServiceResourceGroups
{

    internal class PayloadServiceGroup : IServiceResourceGroupConfiguration
    {
        public void Configure(IServiceResourceGroupBuilder builder, EV2TemplateGeneratorContext context)
        {
            if (!context.AdditionalConfiguration.ContainsKey(ConfigurationNames.AzureSubscriptionId))
            {
                throw new InvalidOperationException($"Context not initialized with {ConfigurationNames.AzureSubscriptionId}");
            }

            if (!Guid.TryParse(context.AdditionalConfiguration[ConfigurationNames.AzureSubscriptionId], out var subId))
            {
                throw new InvalidOperationException($"{ConfigurationNames.AzureSubscriptionId} in context must be a valid Guid");
            }

            foreach (var region in context.BuildFor)
            {
                builder
                .DeployToAzureSubscriptionId(subId)
                .UseResourceGroupName($"JunoPayload{context.ApplicationEnvironment}")
                .UseLocation(region.Name)
                .InstanceOf(new PayloadServiceGroupDefinition())
                .AddServiceResource(new PayloadService());
            }            
        }
    }
}
