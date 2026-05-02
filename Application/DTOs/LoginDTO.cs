using System.ComponentModel.DataAnnotations;

namespace FinanceAPI.Application.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória")]
        public required string Senha { get; set; }
    }
}
