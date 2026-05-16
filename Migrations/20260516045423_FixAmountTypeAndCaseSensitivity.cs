using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixAmountTypeAndCaseSensitivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Converte a coluna Amount de text para numeric usando o CAST correto do Postgres
            migrationBuilder.Sql("ALTER TABLE \"Transactions\" ALTER COLUMN \"Amount\" TYPE numeric USING \"Amount\"::numeric;");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
