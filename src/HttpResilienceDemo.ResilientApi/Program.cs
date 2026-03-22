using HttpResilienceDemo.ResilientApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var unreliableWeatherApiBaseAddress = new Uri(builder.Configuration["UnreliableWeatherApi:BaseAddress"]!);

builder.Services
    .AddConstantRetryClient(unreliableWeatherApiBaseAddress)
    .AddLinearRetryClient(unreliableWeatherApiBaseAddress)
    .AddExponentialRetryClient(unreliableWeatherApiBaseAddress)
    .AddSelectiveRetryClient(unreliableWeatherApiBaseAddress)
    .AddRetryAfterClient(unreliableWeatherApiBaseAddress)
    .AddCircuitBreakerClient(unreliableWeatherApiBaseAddress)
    .AddTimeoutClient(unreliableWeatherApiBaseAddress)
    .AddFallbackClient(unreliableWeatherApiBaseAddress)
    .AddTimeoutFallbackClient(unreliableWeatherApiBaseAddress)
    .AddRateLimiterClient(unreliableWeatherApiBaseAddress)
    .AddHedgingClient(unreliableWeatherApiBaseAddress)
    .AddCombinedClient(unreliableWeatherApiBaseAddress)
    .AddStandardResilienceClient(unreliableWeatherApiBaseAddress);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
