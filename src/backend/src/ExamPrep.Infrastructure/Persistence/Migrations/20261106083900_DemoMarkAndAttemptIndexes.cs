using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Review D4 and D5. <c>exams.is_demo</c> marks the exams the development demo seeder made, so its reset deletes only
    /// those (until now it deleted whatever bore the demo's name). The two demo exams already in a database are marked once
    /// here by their fixed names, the only time a name decides. Indexes on <c>attempts</c> for the sweeps that run every
    /// 2 to 5 seconds: expiry, marking and the heartbeat watch.
    /// </summary>
    public partial class DemoMarkAndAttemptIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_demo",
                table: "exams",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE exams SET is_demo = 1 WHERE name LIKE 'Victory demo % A2 Key for Schools' OR name LIKE 'Victory demo % B1 Preliminary for Schools';");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_last_heartbeat_at",
                table: "attempts",
                column: "last_heartbeat_at");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_status_ends_at",
                table: "attempts",
                columns: new[] { "status", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_attempts_status_grading_status",
                table: "attempts",
                columns: new[] { "status", "grading_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attempts_last_heartbeat_at",
                table: "attempts");

            migrationBuilder.DropIndex(
                name: "IX_attempts_status_ends_at",
                table: "attempts");

            migrationBuilder.DropIndex(
                name: "IX_attempts_status_grading_status",
                table: "attempts");

            migrationBuilder.DropColumn(
                name: "is_demo",
                table: "exams");
        }
    }
}
