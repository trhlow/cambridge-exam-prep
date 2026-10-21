using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChooseOneQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selection",
                table: "responses",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                // Existing answers were never chosen.
                defaultValue: "Undecided");

            migrationBuilder.AddColumn<int>(
                name: "choose_count",
                table: "question_groups",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_question_groups_choose_count",
                table: "question_groups",
                sql: "choose_count IS NULL OR choose_count > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_question_groups_choose_count",
                table: "question_groups");

            migrationBuilder.DropColumn(
                name: "selection",
                table: "responses");

            migrationBuilder.DropColumn(
                name: "choose_count",
                table: "question_groups");
        }
    }
}
