using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Fallback;
using Polly.Hedging;
using Polly.Retry;
using Polly.Timeout;
using System.Net;
using System.Threading.RateLimiting;

namespace HttpResilienceDemo.ResilientApi.Extensions;

public static class HttpClientResilienceExtensions
{
    private static Func<OnRetryArguments<HttpResponseMessage>, ValueTask> OnRetry => args =>
    {
        Console.WriteLine($"Retry attempt {args.AttemptNumber + 1} after {args.RetryDelay.TotalSeconds:F1}s delay. Status: {args.Outcome.Result?.StatusCode}");
        return default;
    };

    public static IServiceCollection AddConstantRetryClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Constant", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("constant-retry", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = OnRetry
            });
        });

        return services;
    }

    public static IServiceCollection AddLinearRetryClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Linear", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("linear-retry", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Linear,
                OnRetry = OnRetry
            });
        });

        return services;
    }

    public static IServiceCollection AddExponentialRetryClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Exponential", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("exponential-retry", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = OnRetry
            });
        });

        return services;
    }

    public static IServiceCollection AddSelectiveRetryClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Selective", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("selective-retry", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Result?.StatusCode is HttpStatusCode.TooManyRequests
                        or HttpStatusCode.ServiceUnavailable),
                OnRetry = OnRetry
            });
        });

        return services;
    }

    public static IServiceCollection AddRetryAfterClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-RetryAfter", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("retry-after", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromSeconds(1),
                DelayGenerator = static args =>
                {
                    if (args.Outcome.Result is HttpResponseMessage response
                        && response.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
                    {
                        return ValueTask.FromResult<TimeSpan?>(retryAfter);
                    }

                    return ValueTask.FromResult<TimeSpan?>(null);
                },
                OnRetry = OnRetry
            });
        });

        return services;
    }

    public static IServiceCollection AddCircuitBreakerClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-CircuitBreaker", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("circuit-breaker", pipelineBuilder =>
        {
            pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(5),
                OnOpened = args =>
                {
                    Console.WriteLine($"Circuit OPENED. Break duration: {args.BreakDuration.TotalSeconds:F1}s");
                    return default;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("Circuit CLOSED. Requests flowing normally.");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("Circuit HALF-OPENED. Testing with next request...");
                    return default;
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddTimeoutClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Timeout", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("timeout", pipelineBuilder =>
        {
            pipelineBuilder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(3),
                OnTimeout = static args =>
                {
                    Console.WriteLine($"Request timed out after {args.Timeout.TotalSeconds:F1}s");
                    return default;
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddFallbackClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Fallback", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("fallback", pipelineBuilder =>
        {
            pipelineBuilder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Exception is not null ||
                    args.Outcome.Result?.IsSuccessStatusCode == false),
                FallbackAction = static args =>
                {
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new { Temperature = 0, Summary = "N/A (fallback)" })
                    };
                    return Outcome.FromResultAsValueTask(response);
                },
                OnFallback = static args =>
                {
                    Console.WriteLine($"Fallback triggered. Reason: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                    return default;
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddTimeoutFallbackClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-TimeoutFallback", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("timeout-fallback", pipelineBuilder =>
        {
            pipelineBuilder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Exception is not null ||
                    args.Outcome.Result?.IsSuccessStatusCode == false),
                FallbackAction = static args =>
                {
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new { Temperature = 0, Summary = "N/A (fallback)" })
                    };
                    return Outcome.FromResultAsValueTask(response);
                },
                OnFallback = static args =>
                {
                    Console.WriteLine($"Fallback triggered. Reason: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                    return default;
                }
            });

            pipelineBuilder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(3),
                OnTimeout = static args =>
                {
                    Console.WriteLine($"Request timed out after {args.Timeout.TotalSeconds:F1}s");
                    return default;
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddRateLimiterClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-RateLimiter", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("rate-limiter", pipelineBuilder =>
        {
            pipelineBuilder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueLimit = 2
            });
        });

        return services;
    }

    public static IServiceCollection AddHedgingClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Hedging", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("hedging", pipelineBuilder =>
        {
            pipelineBuilder.AddHedging(new HedgingStrategyOptions<HttpResponseMessage>
            {
                MaxHedgedAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Exception is not null ||
                    args.Outcome.Result?.IsSuccessStatusCode == false),
                OnHedging = static args =>
                {
                    Console.WriteLine($"Hedging: attempt #{args.AttemptNumber} started.");
                    return default;
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddCombinedClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Combined", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddResilienceHandler("combined", pipelineBuilder =>
        {
            pipelineBuilder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                OnTimeout = static args =>
                {
                    Console.WriteLine($"Total request timed out after {args.Timeout.TotalSeconds:F1}s");
                    return default;
                }
            });

            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = OnRetry
            });

            pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(5),
                OnOpened = args =>
                {
                    Console.WriteLine($"[Combined] Circuit OPENED. Break duration: {args.BreakDuration.TotalSeconds:F1}s");
                    return default;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("[Combined] Circuit CLOSED. Requests flowing normally.");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("[Combined] Circuit HALF-OPENED. Testing with next request...");
                    return default;
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddStandardResilienceClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient("UnreliableWeatherApi-Standard", client =>
        {
            client.BaseAddress = baseAddress;
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
