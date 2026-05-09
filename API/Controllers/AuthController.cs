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
                if (!result.Succeeded) return BadRequest(new { message = "Erro ao criar usuário", errors = result.Errors });
            }

            var isValidPassword = await _userManager.CheckPasswordAsync(user, model.Senha);
            if (!isValidPassword)
            {
                return Unauthorized(new { message = "Credenciais inválidas" });
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }

        [HttpPost("demo")]
        public async Task<IActionResult> LoginDemo()
        {
            try 
            {
                var demoEmail = "guest@rondon.com";
                var demoPass = "Guest@123!";
                
                var user = await _userManager.FindByEmailAsync(demoEmail);
                
                if (user == null)
                {
                    // Forçamos o UserName a ser o e-mail para evitar conflitos de validação no Postgres
                    user = new IdentityUser { UserName = demoEmail, Email = demoEmail };
                    var result = await _userManager.CreateAsync(user, demoPass);
                    
                    if (!result.Succeeded) 
                    {
                        return BadRequest(new { message = "Falha ao criar convidado", errors = result.Errors });
                    }
                }

                var token = _tokenService.GenerateToken(user);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                // Agora retornando a mensagem real para facilitar o debug no Railway
                return StatusCode(500, new { message = "Erro interno no servidor", detail = ex.Message });
            }
        }
    }
}