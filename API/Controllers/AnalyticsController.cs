using FinanceAPI.Domain.Entities;
using FinanceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinanceAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly FinanceDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AnalyticsController(FinanceDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [AllowAnonymous]
        [HttpPost("click")]
        public async Task<IActionResult> TrackClick([FromBody] ClickRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProjectName))
            {
                return BadRequest("Project name is required.");
            }

            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            // Cleanup IP list if it's forwarded through multiple proxies
            if (!string.IsNullOrWhiteSpace(ip) && ip.Contains(','))
            {
                ip = ip.Split(',')[0].Trim();
            }

            string state = "Local/Desconhecido";
            string city = "Local/Desconhecido";

            if (!string.IsNullOrWhiteSpace(ip) && ip != "127.0.0.1" && ip != "::1")
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync($"http://ip-api.com/json/{ip}?fields=status,regionName,city");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var geo = JsonSerializer.Deserialize<IpApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (geo != null && geo.Status == "success")
                        {
                            state = geo.RegionName ?? state;
                            city = geo.City ?? city;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Analytics] Error tracking IP {ip}: {ex.Message}");
                }
            }

            var log = new ClickLog
            {
                ProjectName = request.ProjectName,
                State = state,
                City = city,
                ClickedAt = DateTime.UtcNow
            };

            _context.ClickLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class ClickRequest
    {
        public string ProjectName { get; set; } = string.Empty;
    }

    public class IpApiResponse
    {
        public string? Status { get; set; }
        public string? RegionName { get; set; }
        public string? City { get; set; }
    }
}
