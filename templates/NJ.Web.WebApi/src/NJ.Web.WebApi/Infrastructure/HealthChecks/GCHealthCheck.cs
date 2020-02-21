using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace NJ.Web.WebApi.Infrastructure.HealthChecks
{
    public class GCHealthCheck : IHealthCheck
    {
        private readonly IOptionsMonitor<GCInfoOptions> _options;

        public GCHealthCheck(IOptionsMonitor<GCInfoOptions> options)
        {
            _options = options;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var options = _options.Get(context.Registration.Name);

            var allocated = GC.GetTotalMemory(forceFullCollection: false);
            var data = new Dictionary<string, object>()
            {
                {"Allocated", allocated},
                {"Gen0Collections", GC.CollectionCount(0)},
                {"Gen1Collections", GC.CollectionCount(1)},
                {"Gen2Collections", GC.CollectionCount(2)},
            };

            var result = allocated >= options.ThresholdInBytes ? context.Registration.FailureStatus : HealthStatus.Healthy;

            return Task.FromResult(new HealthCheckResult(
                result,
                description: $"reports degraded status if allocated bytes >= {options.ThresholdInBytes} bytes",
                data: data));
        }
    }

    public static class GCHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddGCCheck(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            long? thresholdInBytes = null)
        {
            builder.AddCheck<GCHealthCheck>(name, failureStatus ?? HealthStatus.Degraded, tags);

            if (thresholdInBytes.HasValue)
            {
                builder.Services.Configure<GCInfoOptions>(name, options =>
                {
                    options.ThresholdInBytes = thresholdInBytes.Value;
                });
            }

            return builder;
        }
    }
    
    public class GCInfoOptions
    {
        public static long OneGigaBytes => 1024L * 1024L * 1024L;
        
        public long ThresholdInBytes { get; set; } = OneGigaBytes;
    }
}