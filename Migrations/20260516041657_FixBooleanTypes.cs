using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixBooleanTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"EmailConfirmed\" TYPE boolean USING \"EmailConfirmed\"::boolean;");
            migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"PhoneNumberConfirmed\" TYPE boolean USING \"PhoneNumberConfirmed\"::boolean;");
            migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"TwoFactorEnabled\" TYPE boolean USING \"TwoFactorEnabled\"::boolean;");
            migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"LockoutEnabled\" TYPE boolean USING \"LockoutEnabled\"::boolean;");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
