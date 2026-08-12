using FinanceAPI.Domain.Entities;
using FinanceAPI.Domain.Interfaces;

namespace FinanceAPI.Application.Services
{
    public class TransactionService
    {
        private readonly ITransactionRepository _repository;
        private readonly RabbitMqPublisher _rabbitMqPublisher;

        public TransactionService(ITransactionRepository repository, RabbitMqPublisher rabbitMqPublisher)
        {
            _repository = repository;
            _rabbitMqPublisher = rabbitMqPublisher;
        }

        public async Task<IEnumerable<Transaction>> GetUserTransactionsAsync(string userId)
        {
            return await _repository.GetAllAsync(userId);
        }

        public async Task<decimal> CalculateBalanceAsync(string userId)
        {
            var transactions = await _repository.GetAllAsync(userId);

            var income = transactions
                .Where(t => t.Type == "Income")
                .Sum(t => t.Amount);

            var expense = transactions
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            return income - expense;
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int id, string userId)
        {
            return await _repository.GetByIdAsync(id, userId);
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            await _repository.AddAsync(transaction);
            await _rabbitMqPublisher.PublishTransactionEventAsync(transaction);
        }

        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            await _repository.UpdateAsync(transaction);
        }

        public async Task<bool> DeleteTransactionAsync(int id, string userId)
        {
            var transaction = await _repository.GetByIdAsync(id, userId);

            if (transaction != null)
            {
                await _repository.DeleteAsync(id, userId);
                return true;
            }
            return false;
        }
    }
}
