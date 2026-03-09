using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BAU_Plagiarism_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassNameToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiDetectionJson",
                table: "PlagiarismChecks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiDetectionLevel",
                table: "PlagiarismChecks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AiProbability",
                table: "PlagiarismChecks",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassName",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiDetectionJson",
                table: "PlagiarismChecks");

            migrationBuilder.DropColumn(
                name: "AiDetectionLevel",
                table: "PlagiarismChecks");

            migrationBuilder.DropColumn(
                name: "AiProbability",
                table: "PlagiarismChecks");

            migrationBuilder.DropColumn(
                name: "ClassName",
                table: "Documents");
        }
    }
}
