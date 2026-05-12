using FinanceAPI.Application.DTOs;
using FinanceAPI.Application.Services;
using FinanceAPI.Domain.Entities;
using FinanceAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAPI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionService _service;

        public TransactionsController(TransactionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _service.GetUserTransactionsAsync(userId);
            return Ok(transactions);
        }

        [HttpGet("balance")]
        public async Task<ActionResult<decimal>> GetBalance()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var balance = await _service.CalculateBalanceAsync(userId);
            return Ok(balance);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transaction = await _service.GetTransactionByIdAsync(id, userId);

            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }

        [HttpPost]
        public async Task<ActionResult<Transaction>> PostTransaction(TransacaoCreateDTO transacaoDto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (transacaoDto.Tipo != "Income" && transacaoDto.Tipo != "Expense")
            {
                return BadRequest(new { message = "O tipo deve ser apenas 'Income' ou 'Expense'." });
            }

            var transaction = new Transaction
            {
                Description = transacaoDto.Descricao,
                Amount = transacaoDto.Valor,
                Date = transacaoDto.Data,
                Type = transacaoDto.Tipo,
                UserId = userId
            };

            await _service.AddTransactionAsync(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, TransacaoCreateDTO transacaoDto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (transacaoDto.Tipo != "Income" && transacaoDto.Tipo != "Expense")
            {
                return BadRequest(new { message = "O tipo deve ser apenas 'Income' ou 'Expense'." });
            }

            var transaction = await _service.GetTransactionByIdAsync(id, userId);
            if (transaction == null)
            {
                return NotFound();
            }

            transaction.Description = transacaoDto.Descricao;
            transaction.Amount = transacaoDto.Valor;
            transaction.Date = transacaoDto.Data;
            transaction.Type = transacaoDto.Tipo;

            await _service.UpdateTransactionAsync(transaction);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _service.DeleteTransactionAsync(id, userId);
            return NoContent();
        }
    }
}
