using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BAU_Plagiarism_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyCheckLimitSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChecksUsedToday",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DailyCheckLimit",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckResetDate",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChecksUsedToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DailyCheckLimit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastCheckResetDate",
                table: "Users");
        }
    }
}
