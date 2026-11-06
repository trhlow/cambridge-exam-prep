using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnswerCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "answer_corrections",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    test_paper_id = table.Column<int>(type: "int", nullable: false),
                    question_number = table.Column<int>(type: "int", nullable: false),
                    old_answers = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    new_answers = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    corrected_by_user_id = table.Column<int>(type: "int", nullable: true),
                    corrected_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    attempts_remarked = table.Column<int>(type: "int", nullable: false),
                    attempts_skipped = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_corrections", x => x.id);
                    table.CheckConstraint("CK_answer_corrections_counts", "attempts_remarked >= 0 AND attempts_skipped >= 0");
                    table.ForeignKey(
                        name: "FK_answer_corrections_test_papers_test_paper_id",
                        column: x => x.test_paper_id,
                        principalTable: "test_papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_answer_corrections_users_corrected_by_user_id",
                        column: x => x.corrected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_answer_corrections_corrected_by_user_id",
                table: "answer_corrections",
                column: "corrected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_answer_corrections_test_paper_id_question_number",
                table: "answer_corrections",
                columns: new[] { "test_paper_id", "question_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answer_corrections");
        }
    }
}
