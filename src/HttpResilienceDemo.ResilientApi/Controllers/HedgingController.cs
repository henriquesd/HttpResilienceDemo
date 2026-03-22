using Microsoft.AspNetCore.Mvc;

namespace HttpResilienceDemo.ResilientApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HedgingController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HedgingController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWithHedging()
    {
        var client = _httpClientFactory.CreateClient("UnreliableWeatherApi-Hedging");
        var response = await client.GetAsync("/api/weather");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        return StatusCode((int)response.StatusCode);
    }
}
