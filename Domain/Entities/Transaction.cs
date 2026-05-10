namespace FinanceAPI.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public required string Type { get; set; } // "Income" (Receita) ou "Expense" (Despesa)
        public string? UserId { get; set; }
    }
}
