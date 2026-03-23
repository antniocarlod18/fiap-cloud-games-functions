using Elastic.Apm;
using MassTransit;
using System.Diagnostics;
using System.Threading.Channels;

namespace fiap_cloud_games_functions.Api.Filters;

public class TracingSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    public async Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        var transaction = Agent.Tracer.CurrentTransaction;

        if (transaction != null)
        {
            var traceparent = transaction
                .OutgoingDistributedTracingData
                .SerializeToString();

            context.Headers.Set("traceparent", traceparent);
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("TracingSendFilter");
    }
}
