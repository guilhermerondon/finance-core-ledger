using FinanceAPI.Application.DTOs;
using FinanceAPI.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Senha))
            {
                var token = _tokenService.GenerateToken(user);
                return Ok(new { token });
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpPost("anonymous")]
        public async Task<IActionResult> LoginAnonymous()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = Guid.NewGuid().ToString().Substring(0, 8);
            var guestEmail = $"guest_{timestamp}_{random}@gr.com";
            var guestUser = new IdentityUser { UserName = guestEmail, Email = guestEmail };

            // Senha robusta para cumprir requisitos do Identity
            var guestPassword = "GuestPassword@123!";
            
            Console.WriteLine($"[AUTH] Tentando criar usuário anônimo: {guestEmail}");

            var result = await _userManager.CreateAsync(guestUser, guestPassword);
            if (result.Succeeded)
            {
                Console.WriteLine($"[AUTH] Usuário anônimo criado com sucesso: {guestUser.Id}");
                var token = _tokenService.GenerateToken(guestUser);
                return Ok(new { token });
            }

            Console.WriteLine($"[AUTH] Erro ao criar usuário anônimo: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            return BadRequest(result.Errors);
        }

        [HttpPost("demo")]
        public async Task<IActionResult> LoginDemo()
        {
            // O demo agora é sinônimo de anonymous para isolamento total
            return await LoginAnonymous();
        }
    }
}