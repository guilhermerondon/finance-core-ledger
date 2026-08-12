using FinanceAPI.Application.DTOs;
using FinanceAPI.Application.Services;
using FinanceAPI.Domain.Entities;
using FinanceAPI.Domain.Interfaces;
using FinanceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace FinanceAPI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionService _service;
        private readonly FinanceDbContext _context;
        private readonly IDistributedCache _cache;

        public TransactionsController(TransactionService service, FinanceDbContext context, IDistributedCache cache)
        {
            _service = service;
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cacheKey = $"finance_transactions:{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedTransactions = JsonSerializer.Deserialize<IEnumerable<Transaction>>(cachedData);
                return Ok(cachedTransactions);
            }

            var transactions = await _service.GetUserTransactionsAsync(userId);
            
            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(transactions), cacheOptions);

            return Ok(transactions);
        }

        [HttpGet("balance")]
        public async Task<ActionResult<decimal>> GetBalance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cacheKey = $"finance_balance:{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return Ok(JsonSerializer.Deserialize<decimal>(cachedData));
            }

            var balance = await _service.CalculateBalanceAsync(userId);
            
            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(balance), cacheOptions);

            return Ok(balance);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Normaliza o tipo para o formato esperado pelo sistema (PascalCase)
            var tipoNormalizado = transacaoDto.Tipo?.Trim().ToLower() switch
            {
                "income" or "entrada" or "receita" => "Income",
                "expense" or "saida" or "saída" or "despesa" => "Expense",
                _ => transacaoDto.Tipo
            };

            if (tipoNormalizado != "Income" && tipoNormalizado != "Expense")
            {
                return BadRequest(new { message = "O tipo deve ser 'Income' (Entrada) ou 'Expense' (Saída)." });
            }

            var transaction = new Transaction
            {
                Description = transacaoDto.Descricao,
                Amount = transacaoDto.Valor,
                Date = transacaoDto.Data,
                Type = tipoNormalizado,
                UserId = userId
            };

            await _service.AddTransactionAsync(transaction);
            
            await _cache.RemoveAsync($"finance_transactions:{userId}");
            await _cache.RemoveAsync($"finance_balance:{userId}");
            
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, TransacaoCreateDTO transacaoDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var tipoNormalizado = transacaoDto.Tipo?.Trim().ToLower() switch
            {
                "income" or "entrada" or "receita" => "Income",
                "expense" or "saida" or "saída" or "despesa" => "Expense",
                _ => transacaoDto.Tipo
            };

            if (tipoNormalizado != "Income" && tipoNormalizado != "Expense")
            {
                return BadRequest(new { message = "O tipo deve ser 'Income' (Entrada) ou 'Expense' (Saída)." });
            }

            var transaction = await _service.GetTransactionByIdAsync(id, userId);
            if (transaction == null)
            {
                return NotFound();
            }

            transaction.Description = transacaoDto.Descricao;
            transaction.Amount = transacaoDto.Valor;
            transaction.Date = transacaoDto.Data;
            transaction.Type = tipoNormalizado;

            await _service.UpdateTransactionAsync(transaction);

            await _cache.RemoveAsync($"finance_transactions:{userId}");
            await _cache.RemoveAsync($"finance_balance:{userId}");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            // 1. Extrair o UserId de forma segura
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário não identificado.");

            // 2. Buscar a transação
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            // 3. DEFESA CRÍTICA DE NULO (Evita o Erro 500)
            if (transaction == null)
            {
                return NotFound("Transação não encontrada ou você não tem permissão para excluí-la.");
            }

            // 4. Executar a remoção em bloco seguro
            try
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
                
                await _cache.RemoveAsync($"finance_transactions:{userId}");
                await _cache.RemoveAsync($"finance_balance:{userId}");
                
                return NoContent(); // 204 Sucesso sem conteúdo
            }
            catch (Exception ex)
            {
                // Loga o erro real no console do Render se o Postgres chiar
                Console.WriteLine($"Erro crítico ao deletar: {ex.Message}");
                return StatusCode(500, "Erro interno ao persistir a exclusão no banco.");
            }
        }
    }
}
