using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CandidateSpeakingScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "speaking_score",
                table: "candidates",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_candidates_speaking_score",
                table: "candidates",
                sql: "speaking_score IS NULL OR (speaking_score >= 0 AND speaking_score * 2 = FLOOR(speaking_score * 2))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_candidates_speaking_score",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "speaking_score",
                table: "candidates");
        }
    }
}
