using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASUDorms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class sameh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holidays_Students_StudentId",
                table: "Holidays");

            migrationBuilder.DropForeignKey(
                name: "FK_MealTransactions_Students_StudentId",
                table: "MealTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Students",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_DormLocationId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_MealTransactions_StudentId_Date_MealTypeId",
                table: "MealTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_StudentId",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Holidays");

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Students",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);

            migrationBuilder.AddColumn<bool>(
                name: "HasOutstandingPayment",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HighSchoolPercentage",
                table: "Students",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEgyptian",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MissedMealsCount",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "Students",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentageGrade",
                table: "Students",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondarySchoolGovernment",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondarySchoolName",
                table: "Students",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNationalId",
                table: "MealTransactions",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentNationalId",
                table: "Holidays",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Students",
                table: "Students",
                column: "NationalId");

            migrationBuilder.CreateTable(
                name: "PaymentExemption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormLocationId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StudentNationalId = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentExemption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentExemption_Students_StudentNationalId",
                        column: x => x.StudentNationalId,
                        principalTable: "Students",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormLocationId = table.Column<int>(type: "int", nullable: false),
                    StudentNationalId = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    MissedMealsCount = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Students_StudentNationalId",
                        column: x => x.StudentNationalId,
                        principalTable: "Students",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_DormLocation_Building",
                table: "Students",
                columns: new[] { "DormLocationId", "BuildingNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_DormLocation_Faculty",
                table: "Students",
                columns: new[] { "DormLocationId", "Faculty" });

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_StudentNationalId_Date_MealTypeId",
                table: "MealTransactions",
                columns: new[] { "StudentNationalId", "Date", "MealTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_StudentNationalId",
                table: "Holidays",
                column: "StudentNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExemptions_Student_Active",
                table: "PaymentExemption",
                columns: new[] { "StudentNationalId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Date",
                table: "PaymentTransaction",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Student",
                table: "PaymentTransaction",
                column: "StudentNationalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Holidays_Students_StudentNationalId",
                table: "Holidays",
                column: "StudentNationalId",
                principalTable: "Students",
                principalColumn: "NationalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealTransactions_Students_StudentNationalId",
                table: "MealTransactions",
                column: "StudentNationalId",
                principalTable: "Students",
                principalColumn: "NationalId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holidays_Students_StudentNationalId",
                table: "Holidays");

            migrationBuilder.DropForeignKey(
                name: "FK_MealTransactions_Students_StudentNationalId",
                table: "MealTransactions");

            migrationBuilder.DropTable(
                name: "PaymentExemption");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Students",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_DormLocation_Building",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_DormLocation_Faculty",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_MealTransactions_StudentNationalId_Date_MealTypeId",
                table: "MealTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_StudentNationalId",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "HasOutstandingPayment",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "HighSchoolPercentage",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsEgyptian",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MissedMealsCount",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PercentageGrade",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SecondarySchoolGovernment",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SecondarySchoolName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentNationalId",
                table: "MealTransactions");

            migrationBuilder.DropColumn(
                name: "StudentNationalId",
                table: "Holidays");

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Students",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Holidays",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Students",
                table: "Students",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_DormLocationId",
                table: "Students",
                column: "DormLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_StudentId_Date_MealTypeId",
                table: "MealTransactions",
                columns: new[] { "StudentId", "Date", "MealTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_StudentId",
                table: "Holidays",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Holidays_Students_StudentId",
                table: "Holidays",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealTransactions_Students_StudentId",
                table: "MealTransactions",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
