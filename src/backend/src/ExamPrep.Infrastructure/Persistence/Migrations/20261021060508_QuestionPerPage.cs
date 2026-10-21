using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuestionPerPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "question_per_page",
                table: "paper_parts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "question_per_page",
                table: "paper_parts");
        }
    }
}
