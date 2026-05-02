using System.ComponentModel.DataAnnotations;

namespace FinanceAPI.Application.DTOs
{
    public class TransacaoCreateDTO
    {
        [Required(ErrorMessage = "A descrição é obrigatória")]
        public required string Descricao { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Valor { get; set; }

        public DateTime Data { get; set; }

        [Required(ErrorMessage = "O tipo é obrigatório")]
        public required string Tipo { get; set; }
    }
}
