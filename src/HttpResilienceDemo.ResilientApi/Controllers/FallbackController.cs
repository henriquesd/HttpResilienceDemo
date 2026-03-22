using Microsoft.AspNetCore.Mvc;

namespace HttpResilienceDemo.ResilientApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FallbackController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FallbackController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWithFallback()
    {
        var client = _httpClientFactory.CreateClient("UnreliableWeatherApi-Fallback");
        var response = await client.GetAsync("/api/weather");

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
