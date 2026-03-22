using Microsoft.AspNetCore.Mvc;
using Polly.Timeout;

namespace HttpResilienceDemo.ResilientApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeoutController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TimeoutController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWithTimeout()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UnreliableWeatherApi-Timeout");
            var response = await client.GetAsync("/api/weather/delayed");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (TimeoutRejectedException)
        {
            return StatusCode(504, new { Message = "Request timed out. The downstream service took too long to respond." });
        }
    }
}
