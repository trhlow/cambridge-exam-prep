using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    target_id = table.Column<int>(type: "int", nullable: true),
                    summary = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    requested_by_user_id = table.Column<int>(type: "int", nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    decided_by_user_id = table.Column<int>(type: "int", nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    decision_note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    outcome = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_requests", x => x.id);
                    table.CheckConstraint("CK_approval_requests_decider", "decided_by_user_id IS NULL OR decided_by_user_id <> requested_by_user_id OR status = 'Cancelled'");
                    table.ForeignKey(
                        name: "FK_approval_requests_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_approval_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_decided_by_user_id",
                table: "approval_requests",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_requested_by_user_id",
                table: "approval_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_status_kind_target_id",
                table: "approval_requests",
                columns: new[] { "status", "kind", "target_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_requests");
        }
    }
}
