using FinanceAPI.Application.DTOs;
using FinanceAPI.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAPI.API.Controllers
{
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
            var user = await _userManager.FindByEmailAsync(model.Email);
            
            if (user == null)
            {
                user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Senha);
                if (!result.Succeeded) return Unauthorized(result.Errors);
            }

            var isValidPassword = await _userManager.CheckPasswordAsync(user, model.Senha);
            if (!isValidPassword)
            {
                return Unauthorized(new { message = "Credenciais inválidas" });
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }

        // --- AJUSTE: Endpoint para o botão "Acesso Demonstrativo" ---
        [HttpPost("demo")]
        public async Task<IActionResult> LoginDemo()
        {
            var demoEmail = "guest@rondon.com";
            var demoPass = "Guest@123";
            
            var user = await _userManager.FindByEmailAsync(demoEmail);
            
            if (user == null)
            {
                user = new IdentityUser { UserName = "GuestUser", Email = demoEmail };
                // Cria o usuário demo se ele ainda não existir no banco do Railway
                await _userManager.CreateAsync(user, demoPass);
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}