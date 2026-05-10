using FinanceAPI.Application.DTOs;
using FinanceAPI.Domain.Entities;
using FinanceAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAPI.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionRepository _repository;

        public TransactionsController(ITransactionRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Obtém todas as transações cadastradas.
        /// </summary>
        /// <returns>Uma lista de transações.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            var transactions = await _repository.GetAllAsync();
            return Ok(transactions);
        }

        /// <summary>
        /// Obtém uma transação específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da transação.</param>
        /// <returns>A transação solicitada.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }

        /// <summary>
        /// Cadastra uma nova transação.
        /// </summary>
        /// <param name="transacaoDto">Os dados da nova transação.</param>
        /// <returns>A transação recém-criada.</returns>
        [HttpPost]
        public async Task<ActionResult<Transaction>> PostTransaction(TransacaoCreateDTO transacaoDto)
        {
            if (transacaoDto.Tipo != "Income" && transacaoDto.Tipo != "Expense")
            {
                return BadRequest(new { message = "O tipo deve ser apenas 'Income' ou 'Expense'." });
            }

            // Mapeamento manual de TransacaoCreateDTO para Transaction (Domínio)
            var transaction = new Transaction
            {
                Description = transacaoDto.Descricao,
                Amount = transacaoDto.Valor,
                Date = transacaoDto.Data,
                Type = transacaoDto.Tipo
            };

            await _repository.AddAsync(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        /// <summary>
        /// Atualiza uma transação existente.
        /// </summary>
        /// <param name="id">O ID da transação a ser atualizada.</param>
        /// <param name="transacaoDto">Os novos dados da transação.</param>
        /// <returns>Sem conteúdo em caso de sucesso.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, TransacaoCreateDTO transacaoDto)
        {
            if (transacaoDto.Tipo != "Income" && transacaoDto.Tipo != "Expense")
            {
                return BadRequest(new { message = "O tipo deve ser apenas 'Income' ou 'Expense'." });
            }

            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            transaction.Description = transacaoDto.Descricao;
            transaction.Amount = transacaoDto.Valor;
            transaction.Date = transacaoDto.Data;
            transaction.Type = transacaoDto.Tipo;

            await _repository.UpdateAsync(transaction);

            return NoContent();
        }

        /// <summary>
        /// Exclui uma transação existente.
        /// </summary>
        /// <param name="id">O ID da transação a ser excluída.</param>
        /// <returns>Sem conteúdo em caso de sucesso.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
