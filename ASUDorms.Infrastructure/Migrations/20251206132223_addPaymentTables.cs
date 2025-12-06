using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASUDorms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentExemption_Students_StudentNationalId",
                table: "PaymentExemption");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransaction_Students_StudentNationalId",
                table: "PaymentTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentTransaction",
                table: "PaymentTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentExemption",
                table: "PaymentExemption");

            migrationBuilder.RenameTable(
                name: "PaymentTransaction",
                newName: "PaymentTransactions");

            migrationBuilder.RenameTable(
                name: "PaymentExemption",
                newName: "PaymentExemptions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentTransactions",
                table: "PaymentTransactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentExemptions",
                table: "PaymentExemptions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentExemptions_Students_StudentNationalId",
                table: "PaymentExemptions",
                column: "StudentNationalId",
                principalTable: "Students",
                principalColumn: "NationalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Students_StudentNationalId",
                table: "PaymentTransactions",
                column: "StudentNationalId",
                principalTable: "Students",
                principalColumn: "NationalId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentExemptions_Students_StudentNationalId",
                table: "PaymentExemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Students_StudentNationalId",
                table: "PaymentTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentTransactions",
                table: "PaymentTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentExemptions",
                table: "PaymentExemptions");

            migrationBuilder.RenameTable(
                name: "PaymentTransactions",
                newName: "PaymentTransaction");

            migrationBuilder.RenameTable(
                name: "PaymentExemptions",
                newName: "PaymentExemption");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentTransaction",
                table: "PaymentTransaction",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentExemption",
                table: "PaymentExemption",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentExemption_Students_StudentNationalId",
                table: "PaymentExemption",
                column: "StudentNationalId",
                principalTable: "Students",
                principalColumn: "NationalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransaction_Students_StudentNationalId",
                table: "PaymentTransaction",
                column: "StudentNationalId",
                principalTable: "Students",
                principalColumn: "NationalId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
