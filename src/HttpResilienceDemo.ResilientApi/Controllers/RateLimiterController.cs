using Microsoft.AspNetCore.Mvc;
using Polly.RateLimiting;

namespace HttpResilienceDemo.ResilientApi.Controllers;

[ApiController]
[Route("api/rate-limiter")]
public class RateLimiterController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RateLimiterController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWithRateLimiter()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UnreliableWeatherApi-RateLimiter");
            var response = await client.GetAsync("/api/weather/limited");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (RateLimiterRejectedException)
        {
            return StatusCode(429, new { Message = "Rate limit exceeded. Too many concurrent requests." });
        }
    }
}
