using FinanceAPI.Application.DTOs;
using FinanceAPI.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAPI.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly TokenService _tokenService;

        public AuthController(UserManager<IdentityUser> userManager, TokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            // BYPASS: Retornando sucesso imediato com token fake
            return Ok(new { token = "fake-jwt-token-for-stabilization" });
        }

        [HttpPost("demo")]
        public async Task<IActionResult> LoginDemo()
        {
            // BYPASS: Retornando sucesso imediato com token fake
            return Ok(new { token = "fake-jwt-token-for-stabilization" });
        }
    }
}