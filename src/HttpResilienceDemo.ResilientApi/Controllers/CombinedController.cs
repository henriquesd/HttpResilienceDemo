using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace HttpResilienceDemo.ResilientApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CombinedController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CombinedController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWithCombinedPipeline()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UnreliableWeatherApi-Combined");
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
        catch (TimeoutRejectedException)
        {
            return StatusCode(504, new { Message = "Request timed out." });
        }
    }
}
