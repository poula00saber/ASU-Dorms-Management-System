using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASUDorms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class testFootPrint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_NationalId",
                table: "Students");

            migrationBuilder.RenameIndex(
                name: "IX_MealTransactions_StudentNationalId_Date_MealTypeId",
                table: "MealTransactions",
                newName: "IX_MealTransactions_Student_Date_MealType");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "PaymentTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "PaymentExemptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Holidays",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Students_DormLocationId",
                table: "Students",
                column: "DormLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_NationalId",
                table: "Students",
                column: "NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentId",
                table: "Students",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_Date",
                table: "MealTransactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_StudentNationalId",
                table: "MealTransactions",
                column: "StudentNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_Time",
                table: "MealTransactions",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Dates",
                table: "Holidays",
                columns: new[] { "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_DormLocationId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_NationalId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_StudentId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_MealTransactions_Date",
                table: "MealTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MealTransactions_StudentNationalId",
                table: "MealTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MealTransactions_Time",
                table: "MealTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_Dates",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "PaymentExemptions");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Holidays");

            migrationBuilder.RenameIndex(
                name: "IX_MealTransactions_Student_Date_MealType",
                table: "MealTransactions",
                newName: "IX_MealTransactions_StudentNationalId_Date_MealTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_NationalId",
                table: "Students",
                column: "NationalId");
        }
    }
}
