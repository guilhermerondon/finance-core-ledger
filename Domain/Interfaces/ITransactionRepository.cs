using FinanceAPI.Domain.Entities;

namespace FinanceAPI.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetAllAsync(string userId);
        Task<Transaction?> GetByIdAsync(int id, string userId);
        Task AddAsync(Transaction transaction);
        Task UpdateAsync(Transaction transaction);
        Task DeleteAsync(int id);
    }
}
