using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HttpResilienceDemo.UnreliableWeatherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private static int _slowRequestCount;
    private static readonly Stopwatch _unstableTimer = Stopwatch.StartNew();

    [HttpGet]
    public IActionResult Get()
    {
        if (Random.Shared.Next(1, 4) == 1)
        {
            return Ok(new { Temperature = 25, Summary = "Sunny" });
        }

        return StatusCode(500);
    }

    [HttpGet("slow")]
    public IActionResult GetSlow()
    {
        var count = Interlocked.Increment(ref _slowRequestCount);

        if (count <= 4)
        {
            return StatusCode(503, new { Message = "Service temporarily unavailable" });
        }

        Interlocked.Exchange(ref _slowRequestCount, 0);

        return Ok(new { Temperature = 20, Summary = "Cloudy" });
    }

    [HttpGet("rate-limited")]
    public IActionResult GetRateLimited()
    {
        if (Random.Shared.Next(1, 4) == 1)
        {
            return Ok(new { Temperature = 22, Summary = "Partly Cloudy" });
        }

        Response.Headers["Retry-After"] = "2";

        return StatusCode(429, new { Message = "Too many requests" });
    }

    [HttpGet("mixed")]
    public IActionResult GetMixed()
    {
        var roll = Random.Shared.Next(1, 4);

        if (roll == 1)
        {
            return Ok(new { Temperature = 24, Summary = "Mild" });
        }

        if (roll == 2)
        {
            return StatusCode(503, new { Message = "Service unavailable" });
        }

        return StatusCode(500, new { Message = "Internal server error" });
    }

    [HttpGet("unstable")]
    public IActionResult GetUnstable()
    {
        var secondsInCycle = _unstableTimer.Elapsed.TotalSeconds % 20;

        if (secondsInCycle < 12)
        {
            return StatusCode(500, new { Message = "Service is down" });
        }

        return Ok(new { Temperature = 18, Summary = "Windy" });
    }

    [HttpGet("delayed")]
    public async Task<IActionResult> GetDelayed()
    {
        var delaySeconds = Random.Shared.Next(5, 11);

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        return Ok(new { Temperature = 15, Summary = "Rainy" });
    }

    [HttpGet("limited")]
    public async Task<IActionResult> GetLimited()
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        return Ok(new { Temperature = 21, Summary = "Clear" });
    }
}
