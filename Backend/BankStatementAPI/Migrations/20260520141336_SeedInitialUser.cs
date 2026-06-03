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
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 1)
BEGIN
    INSERT INTO [Users] ([Id], [AddedBy], [CreatedAt], [Email], [FullName], [IsActive], [Username])
    VALUES (1, N'Daniel', '2026-05-20T00:00:00.0000000Z', N'Daniel.Dzivor@myumbbank.com', N'Daniel Dzivor', CAST(1 AS bit), N'Daniel.Dzivor')
END");
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
