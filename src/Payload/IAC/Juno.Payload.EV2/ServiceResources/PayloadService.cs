namespace Juno.Payload.EV2.ServiceResources
{
    internal class PayloadService : IServiceResourceConfiguration
    {
        public void Configure(IServiceResourceBuilder builder, EV2TemplateGeneratorContext context)
        {
            builder
                .HasName("PayloadService")
                .InstanceOf(new PayloadServiceDefinition());
        }
    }
}
