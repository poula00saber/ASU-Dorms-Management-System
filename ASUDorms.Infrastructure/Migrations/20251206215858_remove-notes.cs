using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASUDorms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removenotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PaymentTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PaymentTransactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
