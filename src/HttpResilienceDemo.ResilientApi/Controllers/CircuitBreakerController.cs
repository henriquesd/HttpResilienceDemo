using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;

namespace HttpResilienceDemo.ResilientApi.Controllers;

[ApiController]
[Route("api/circuit-breaker")]
public class CircuitBreakerController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CircuitBreakerController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWithCircuitBreaker()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UnreliableWeatherApi-CircuitBreaker");
            var response = await client.GetAsync("/api/weather/unstable");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (BrokenCircuitException)
        {
            return StatusCode(503, new { Message = "Circuit breaker is open. Service is temporarily unavailable." });
        }
    }
}
