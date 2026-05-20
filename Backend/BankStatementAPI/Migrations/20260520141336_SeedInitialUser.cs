using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankStatementAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AddedBy", "CreatedAt", "Email", "FullName", "IsActive", "Username" },
                values: new object[] { 1, "Daniel", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Daniel.Dzivor@myumbbank.com", "Daniel Dzivor", true, "Daniel.Dzivor" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
