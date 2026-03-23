using Elastic.Apm;
using Elastic.Apm.Api;
using MassTransit;

namespace fiap_cloud_games_functions.Api.Filters
{
    public class TracingConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
    {
        public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            var traceparent = context.Headers.Get<string>("traceparent");

            DistributedTracingData tracingData = null;

            if (!string.IsNullOrEmpty(traceparent))
            {
                tracingData = DistributedTracingData.TryDeserializeFromString(traceparent);
            }

            var transaction = Agent.Tracer.StartTransaction(
                "Consume Message",
                "messaging",
                tracingData
            );

            try
            {
                await next.Send(context);
            }
            finally
            {
                transaction?.End();
            }
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("TracingConsumeFilter");
        }
    }
}
