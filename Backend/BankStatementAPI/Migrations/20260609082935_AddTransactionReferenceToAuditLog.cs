using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankStatementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionReferenceToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankTransactionReference",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChargeTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebitAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatementAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankTransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffUsername = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuditLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargeTransactions_AuditLogs_AuditLogId",
                        column: x => x.AuditLogId,
                        principalTable: "AuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeTransactions_AuditLogId",
                table: "ChargeTransactions",
                column: "AuditLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeTransactions");

            migrationBuilder.DropColumn(
                name: "BankTransactionReference",
                table: "AuditLogs");
        }
    }
}
