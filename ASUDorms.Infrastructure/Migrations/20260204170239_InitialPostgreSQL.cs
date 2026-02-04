using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASUDorms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DormLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCombinedMealScan = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DormLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    NationalId = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    DormLocationId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsEgyptian = table.Column<bool>(type: "boolean", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Religion = table.Column<string>(type: "text", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Government = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    District = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StreetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Faculty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PercentageGrade = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    SecondarySchoolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SecondarySchoolGovernment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HighSchoolPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    DormType = table.Column<string>(type: "text", nullable: false),
                    BuildingNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HasSpecialNeeds = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialNeedsDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsExemptFromFees = table.Column<bool>(type: "boolean", nullable: false),
                    MissedMealsCount = table.Column<int>(type: "integer", nullable: false),
                    HasOutstandingPayment = table.Column<bool>(type: "boolean", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FatherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FatherNationalId = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    FatherProfession = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FatherPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GuardianName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GuardianRelationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GuardianPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.NationalId);
                    table.ForeignKey(
                        name: "FK_Students_DormLocations_DormLocationId",
                        column: x => x.DormLocationId,
                        principalTable: "DormLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    DormLocationId = table.Column<int>(type: "integer", nullable: false),
                    AccessibleDormLocationIds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_DormLocations_DormLocationId",
                        column: x => x.DormLocationId,
                        principalTable: "DormLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNationalId = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_Students_StudentNationalId",
                        column: x => x.StudentNationalId,
                        principalTable: "Students",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DormLocationId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StudentNationalId = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentExemptions_Students_StudentNationalId",
                        column: x => x.StudentNationalId,
                        principalTable: "Students",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DormLocationId = table.Column<int>(type: "integer", nullable: false),
                    StudentNationalId = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    MissedMealsCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Students_StudentNationalId",
                        column: x => x.StudentNationalId,
                        principalTable: "Students",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNationalId = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MealTypeId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    DormLocationId = table.Column<int>(type: "integer", nullable: false),
                    ScannedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealTransactions_DormLocations_DormLocationId",
                        column: x => x.DormLocationId,
                        principalTable: "DormLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealTransactions_MealTypes_MealTypeId",
                        column: x => x.MealTypeId,
                        principalTable: "MealTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealTransactions_Students_StudentNationalId",
                        column: x => x.StudentNationalId,
                        principalTable: "Students",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealTransactions_Users_ScannedByUserId",
                        column: x => x.ScannedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MealTypes",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[,]
                {
                    { 1, "Breakfast & Dinner", "BreakfastDinner" },
                    { 2, "Lunch", "Lunch" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Dates",
                table: "Holidays",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_StudentNationalId",
                table: "Holidays",
                column: "StudentNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_Date",
                table: "MealTransactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_DormLocationId",
                table: "MealTransactions",
                column: "DormLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_MealTypeId",
                table: "MealTransactions",
                column: "MealTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_ScannedByUserId",
                table: "MealTransactions",
                column: "ScannedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_Student_Date_MealType",
                table: "MealTransactions",
                columns: new[] { "StudentNationalId", "Date", "MealTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_StudentNationalId",
                table: "MealTransactions",
                column: "StudentNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTransactions_Time",
                table: "MealTransactions",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExemptions_Student_Active",
                table: "PaymentExemptions",
                columns: new[] { "StudentNationalId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Date",
                table: "PaymentTransactions",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Student",
                table: "PaymentTransactions",
                column: "StudentNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_DormLocation_Building",
                table: "Students",
                columns: new[] { "DormLocationId", "BuildingNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_DormLocation_Faculty",
                table: "Students",
                columns: new[] { "DormLocationId", "Faculty" });

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
                name: "IX_Students_StudentId_DormLocationId",
                table: "Students",
                columns: new[] { "StudentId", "DormLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DormLocationId",
                table: "Users",
                column: "DormLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "MealTransactions");

            migrationBuilder.DropTable(
                name: "PaymentExemptions");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "MealTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "DormLocations");
        }
    }
}
