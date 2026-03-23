using fiap_cloud_games_functions.Api.Filters;
using MassTransit;
using Microsoft.Azure.Functions.Worker.Builder;

namespace fiap_cloud_games_functions.Extensions
{
    public static class MassTransitExtensions
    {
        public static FunctionsApplicationBuilder AddMassTransitConfiguration(this FunctionsApplicationBuilder builder)
        {
            builder.Services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: builder.Environment.EnvironmentName, includeNamespace: false));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.UseSendFilter(typeof(TracingSendFilter<>), context);
                    cfg.UsePublishFilter(typeof(TracingPublishFilter<>), context);

                    cfg.UseConsumeFilter(typeof(TracingConsumeFilter<>), context);

                    cfg.Host(builder.Configuration["RabbitMQ:Host"], builder.Configuration["RabbitMQ:VirtualHost"], h =>
                    {
                        h.Username(builder.Configuration["RabbitMQ:UserName"]);
                        h.Password(builder.Configuration["RabbitMQ:Password"]);
                    });

                    cfg.UseMessageRetry(r => r.Immediate(2));
                    cfg.ConfigureEndpoints(context);
                });
            });

            return builder;
        }
    }
}
