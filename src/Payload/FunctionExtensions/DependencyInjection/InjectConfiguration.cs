

namespace FunctionExtensions.DependencyInjection
{
    using Microsoft.Azure.WebJobs.Host.Config;

    public class InjectConfiguration : IExtensionConfigProvider
    {
        private InjectBindingProvider _injectBindingProvider;

        public InjectConfiguration(InjectBindingProvider injectBindingProvider)
        {
            _injectBindingProvider = injectBindingProvider;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context
                        .AddBindingRule<InjectAttribute>()
                        .Bind(_injectBindingProvider);
        }
    }
}