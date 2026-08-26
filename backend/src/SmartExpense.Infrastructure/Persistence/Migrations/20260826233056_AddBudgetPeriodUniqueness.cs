using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartExpense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPeriodUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_budgets_user_id_month_year",
                table: "budgets",
                columns: new[] { "user_id", "month", "year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_budgets_user_id_month_year",
                table: "budgets");
        }
    }
}
