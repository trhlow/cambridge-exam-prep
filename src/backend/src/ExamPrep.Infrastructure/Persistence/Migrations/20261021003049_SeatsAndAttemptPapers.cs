using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeatsAndAttemptPapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "seats_per_row",
                table: "exams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "paper_mix_id",
                table: "exam_sections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "test_paper_id",
                table: "attempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Attempts made before mixes sat their section's paper.
            migrationBuilder.Sql("UPDATE attempts a JOIN exam_sections s ON s.id = a.exam_section_id SET a.test_paper_id = s.test_paper_id;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_exams_seats_per_row",
                table: "exams",
                sql: "seats_per_row IS NULL OR seats_per_row BETWEEN 1 AND 50");

            migrationBuilder.CreateIndex(
                name: "IX_exam_sections_paper_mix_id",
                table: "exam_sections",
                column: "paper_mix_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_test_paper_id",
                table: "attempts",
                column: "test_paper_id");

            migrationBuilder.AddForeignKey(
                name: "FK_attempts_test_papers_test_paper_id",
                table: "attempts",
                column: "test_paper_id",
                principalTable: "test_papers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_sections_paper_mixes_paper_mix_id",
                table: "exam_sections",
                column: "paper_mix_id",
                principalTable: "paper_mixes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attempts_test_papers_test_paper_id",
                table: "attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_sections_paper_mixes_paper_mix_id",
                table: "exam_sections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_exams_seats_per_row",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "IX_exam_sections_paper_mix_id",
                table: "exam_sections");

            migrationBuilder.DropIndex(
                name: "IX_attempts_test_paper_id",
                table: "attempts");

            migrationBuilder.DropColumn(
                name: "seats_per_row",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "paper_mix_id",
                table: "exam_sections");

            migrationBuilder.DropColumn(
                name: "test_paper_id",
                table: "attempts");
        }
    }
}
