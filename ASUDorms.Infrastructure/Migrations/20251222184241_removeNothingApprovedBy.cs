using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASUDorms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeNothingApprovedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedBy",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "PaymentExemptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessedBy",
                table: "PaymentTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "PaymentExemptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
