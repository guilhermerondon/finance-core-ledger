using FinanceAPI.Application.Services;
using FinanceAPI.Domain.Entities;
using FinanceAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Finance.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _repositoryMock;
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            _repositoryMock = new Mock<ITransactionRepository>();
            _service = new TransactionService(_repositoryMock.Object, null!);
        }

        [Fact]
        public async Task CalculateBalanceAsync_ShouldReturnCorrectBalance_WhenTransactionsExist()
        {
            // Arrange
            var userId = "user-123";
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Description = "Salário", Amount = 5000, Type = "Income", UserId = userId },
                new Transaction { Id = 2, Description = "Aluguel", Amount = 1500, Type = "Expense", UserId = userId },
                new Transaction { Id = 3, Description = "Freelance", Amount = 1000, Type = "Income", UserId = userId },
                new Transaction { Id = 4, Description = "Internet", Amount = 100, Type = "Expense", UserId = userId }
            };

            _repositoryMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(transactions);

            // Act
            var balance = await _service.CalculateBalanceAsync(userId);

            // Assert
            balance.Should().Be(4400); // (5000 + 1000) - (1500 + 100)
        }

        [Fact]
        public async Task GetUserTransactionsAsync_ShouldIsolateUserTransactions()
        {
            // Arrange
            var userA = "user-A";
            var userB = "user-B";

            var transactionsA = new List<Transaction>
            {
                new Transaction { Id = 1, Description = "Job A", Amount = 100, Type = "Income", UserId = userA }
            };

            var transactionsB = new List<Transaction>
            {
                new Transaction { Id = 2, Description = "Job B", Amount = 200, Type = "Income", UserId = userB }
            };

            _repositoryMock.Setup(r => r.GetAllAsync(userA)).ReturnsAsync(transactionsA);
            _repositoryMock.Setup(r => r.GetAllAsync(userB)).ReturnsAsync(transactionsB);

            // Act
            var resultA = await _service.GetUserTransactionsAsync(userA);
            var resultB = await _service.GetUserTransactionsAsync(userB);

            // Assert
            resultA.Should().ContainSingle(t => t.UserId == userA);
            resultA.Should().NotContain(t => t.UserId == userB);

            resultB.Should().ContainSingle(t => t.UserId == userB);
            resultB.Should().NotContain(t => t.UserId == userA);
        }
    }
}
