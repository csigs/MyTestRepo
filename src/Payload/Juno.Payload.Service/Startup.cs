[assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(Juno.Payload.Service.Startup), "A Web Jobs Extension")]
namespace Juno.Payload.Service;

using System;

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using FunctionExtensions.DependencyInjection;
using Juno.Payload.Service.Configurations;
using Juno.Payload.Service.Metrics;
using Juno.Payload.Service.Metrics.DI;
using Juno.Payload.Service.Repository;
using Juno.Payload.Service.DI;

public class Startup : IWebJobsStartup
{
    public void Configure(IWebJobsBuilder builder)
    {
        var serviceProvider = ConfigureServices(builder.Services);
        builder.Services.AddSingleton(new InjectBindingProvider(serviceProvider));
        builder
               .Services
               .AddLogging(builder =>
               {
                   builder.AddOpenTelemetry(o =>
                   {
                       o.AddGenevaLogExporter(eo =>
                       {
                           eo.ConnectionString = "EtwSession=OpenTelemetry";
                       });
                   });
               });

        builder.AddExtension<InjectConfiguration>();
    }

    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddConfigurations();

        //Register services and modules

        var payloadBlobAttachmentRepository = PayloadBlobAttachmentRepositoryFactory.Create();
        var payloadRepository = PayloadRepositoryFactory.CreatePayloadRepository();
        var payloadMetadataRepository = PayloadRepositoryFactory.CreatePayloadMetadataRepository();
        var payloadDataRefRepository = PayloadRepositoryFactory.CreatePayloadDataReferenceRepository();

        containerBuilder.Register(ctx => payloadBlobAttachmentRepository).As<IPayloadAttachmentRepository>().SingleInstance();
        containerBuilder.Register(ctx => payloadRepository).As<IPayloadRepository>().SingleInstance();
        containerBuilder.Register(ctx => payloadMetadataRepository).As<IPayloadMetadataRepository>().SingleInstance();
        containerBuilder.Register(ctx => payloadDataRefRepository).As<IPayloadDataRefRepository>().SingleInstance();

        //TODO: drive by configuration and not compilation profile
#if DEBUG
        containerBuilder
            .RegisterType<MockPayloadMetricEmitter>().As<IPayloadMetricEmitter>().SingleInstance();
#else

        var genevaAccount = config[GenevaConfigurationSettings.GenevaMonitoringAccountName];
        var genevaNamespace = config[GenevaConfigurationSettings.GenevaMonitoringNamespaceName];
        var genevaConnectionString = $"Account={genevaAccount};Namespace={genevaNamespace}";
        containerBuilder
            .Register(ctx => new GenevaPayloadMetricEmitter(
                genevaConnectionString,
                config[GenevaConfigurationSettings.CustomerResourceId],
                config[GenevaConfigurationSettings.Region]))
            .As<IPayloadMetricEmitter>()
            .SingleInstance();
#endif

        containerBuilder.AddSignProvider();

        containerBuilder.AddGenevaMetrics(metricsConfig =>
            metricsConfig
#if DEBUG
                .UseMockGeneva()
#endif
                .AddMetric<PayloadLatency>(config => config
                    .UseMetricName("PayloadLatency")
                    .WithRawValue(c => c.RawValue)
                    .WithMetricDimension(c => c.CustomerId, "PayloadLatency_CustomerId", 0)
                    .WithMetricDimension(c => c.Region, "PayloadLatency_LocationId", 1)
                    .WithMetricDimension(c => c.ResponseTime, "PayloadLatency_ResponseTime", 2))
                .AddMetric<PayloadStatus>(config => config
                    .UseMetricName("PayloadStatus")
                    .WithRawValue(c => c.RawValue)
                    .WithMetricDimension(c => c.CustomerId, "PayloadStatus_CustomerId", 0)
                    .WithMetricDimension(c => c.Instance, "PayloadStatus_Instance", 1)
                    .WithMetricDimension(c => c.StatusCode, "PayloadStatus_StatusCode", 2))
                );

        var applicationContainer = containerBuilder.Build();

        return new AutofacServiceProvider(applicationContainer);
    }
}
