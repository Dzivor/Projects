using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankStatementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingsAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingsAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppSettingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingsAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingsAuditLogs_AppSettings_AppSettingId",
                        column: x => x.AppSettingId,
                        principalTable: "AppSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettingsAuditLogs_AppSettingId",
                table: "SettingsAuditLogs",
                column: "AppSettingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettingsAuditLogs");

            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
