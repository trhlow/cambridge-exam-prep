using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnonymousMarking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "examiner_user_id",
                table: "candidates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "marking_code",
                table: "candidates",
                type: "char(6)",
                fixedLength: true,
                maxLength: 6,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidates_exam_id_marking_code",
                table: "candidates",
                columns: new[] { "exam_id", "marking_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidates_examiner_user_id",
                table: "candidates",
                column: "examiner_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_candidates_marking_code",
                table: "candidates",
                sql: "marking_code IS NULL OR REGEXP_LIKE(marking_code, '^[A-HJ-NP-Z2-9]{6}$', 'c')");

            migrationBuilder.AddForeignKey(
                name: "FK_candidates_users_examiner_user_id",
                table: "candidates",
                column: "examiner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_candidates_users_examiner_user_id",
                table: "candidates");

            migrationBuilder.DropIndex(
                name: "IX_candidates_exam_id_marking_code",
                table: "candidates");

            migrationBuilder.DropIndex(
                name: "IX_candidates_examiner_user_id",
                table: "candidates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_candidates_marking_code",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "examiner_user_id",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "marking_code",
                table: "candidates");
        }
    }
}
