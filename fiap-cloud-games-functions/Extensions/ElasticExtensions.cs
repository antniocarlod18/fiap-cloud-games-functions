using Elastic.Apm.SerilogEnricher;
using Elastic.Channels;
using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace fiap_cloud_games_functions.Extensions;

public static class ElasticExtensions
{
    public static FunctionsApplicationBuilder AddElasticConfiguration(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddSerilog((context, config) =>
        {
            config
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithCorrelationId()
                .Enrich.WithMachineName()
                .Enrich.WithElasticApmCorrelationInfo()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .WriteTo.Console() 
                .WriteTo.Elasticsearch([new Uri(builder.Configuration["ElasticSearch:Uri"])], opts =>
                {
                    opts.DataStream = new DataStreamName("logs", builder.Configuration["ElasticSearch:IndexName"], builder.Environment.EnvironmentName);
                    opts.BootstrapMethod = BootstrapMethod.Failure;
                    opts.TextFormatting = new EcsTextFormatterConfiguration<LogEventEcsDocument>();
                    opts.ConfigureChannel = channelOpts =>
                    {
                        channelOpts.BufferOptions = new BufferOptions();
                    };
                }, transport =>
                {
                    transport.Authentication(new ApiKey(builder.Configuration["ElasticSearch:ApiKey"]));
                });
        });

        builder.Services.AddAllElasticApm();

        return builder;
    }
}
