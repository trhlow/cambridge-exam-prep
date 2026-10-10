using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchemaReviewFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_at_override",
                table: "attempts");

            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp",
                table: "users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "start_announced_at",
                table: "exam_sections",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp",
                table: "candidates",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "end_adjust_seconds",
                table: "attempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_section_templates_duration",
                table: "section_templates",
                sql: "duration_minutes > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_section_templates_order",
                table: "section_templates",
                sql: "order_no > 0 AND part_count > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_questions_min_words",
                table: "questions",
                sql: "min_words IS NULL OR min_words >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_questions_number",
                table: "questions",
                sql: "number > 0 AND order_no > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_questions_points",
                table: "questions",
                sql: "points >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_question_groups_max_words",
                table: "question_groups",
                sql: "max_words_per_answer IS NULL OR max_words_per_answer > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_question_groups_order",
                table: "question_groups",
                sql: "order_no > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_paper_parts_part_no",
                table: "paper_parts",
                sql: "part_no > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_manual_marks_score",
                table: "manual_marks",
                sql: "score >= 0 AND score <= max_score");

            migrationBuilder.AddCheckConstraint(
                name: "CK_exam_sections_duration",
                table: "exam_sections",
                sql: "duration_minutes > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_exam_sections_order",
                table: "exam_sections",
                sql: "order_no > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_candidates_planned_extra",
                table: "candidates",
                sql: "planned_extra_percent BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_non_negative",
                table: "attempts",
                sql: "paused_seconds >= 0 AND planned_extra_seconds >= 0 AND incident_extra_seconds >= 0 AND audio_adjust_ms >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_assets_size",
                table: "assets",
                sql: "size_bytes >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_section_templates_duration",
                table: "section_templates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_section_templates_order",
                table: "section_templates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_questions_min_words",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_questions_number",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_questions_points",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_question_groups_max_words",
                table: "question_groups");

            migrationBuilder.DropCheckConstraint(
                name: "CK_question_groups_order",
                table: "question_groups");

            migrationBuilder.DropCheckConstraint(
                name: "CK_paper_parts_part_no",
                table: "paper_parts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_manual_marks_score",
                table: "manual_marks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_exam_sections_duration",
                table: "exam_sections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_exam_sections_order",
                table: "exam_sections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_candidates_planned_extra",
                table: "candidates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_non_negative",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_assets_size",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "start_announced_at",
                table: "exam_sections");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "end_adjust_seconds",
                table: "attempts");

            migrationBuilder.AddColumn<DateTime>(
                name: "end_at_override",
                table: "attempts",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
