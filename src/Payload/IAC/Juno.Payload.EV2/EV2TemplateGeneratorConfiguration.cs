namespace Juno.Payload.EV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EV2TemplateGeneratorConfiguration : IEVTemplateGeneratorConfiguration
    {
        private readonly string _pathToExport;

        public EV2TemplateGeneratorConfiguration(string pathToExport)
        {
            if (string.IsNullOrWhiteSpace(pathToExport))
            {
                throw new ArgumentException($"'{nameof(pathToExport)}' cannot be null or whitespace.", nameof(pathToExport));
            }

            _pathToExport = pathToExport;
        }

        public void Configure(IEV2EnvironmentConfigurationBuilder builder)
        {
            builder
                .AddEnvironment(s =>
                {
                    s
                    .ForEnvironment(DeploymentEnvironment.Poc)
                    .AddRegion(AzureRegion.EastUS)
                    .AddAdditionalContext(s=>s.AddSetting(ConfigurationNames.AzureSubscriptionId, "58db12b6-6fe2-41da-b9e6-a92159a6f11b"));
                })
                .AddEnvironment(s =>
                {
                    s
                    .ForEnvironment(DeploymentEnvironment.Int)
                    .AddRegion(AzureRegion.WestUS2)
                    .AddAdditionalContext(s => s.AddSetting(ConfigurationNames.AzureSubscriptionId, "1cc566d4-c401-4a12-a612-e96a916b391f"));
                })
                .AddEnvironment(s =>
                {
                    s
                    .ForEnvironment(DeploymentEnvironment.UAT)
                    .AddRegion(AzureRegion.WestUS2)
                    .AddAdditionalContext(s => s.AddSetting(ConfigurationNames.AzureSubscriptionId, "b63743ce-834d-4f8e-b22e-a687cf0bea0c"));
                })
                 .AddEnvironment(s =>
                 {
                     s
                     .ForEnvironment(DeploymentEnvironment.Production)
                     .AddRegion(AzureRegion.WestUS2)
                     .AddAdditionalContext(s => s.AddSetting(ConfigurationNames.AzureSubscriptionId, "55c732aa-bf28-4158-83a5-28ced24b19f1"));
                 })
                .ApplyServiceModelConfiguration(new ServiceModel())
                .ApplyRolloutSpecConfiguration(new RolloutSpec())
                .ExportTo(_pathToExport);
        }
    }
}
