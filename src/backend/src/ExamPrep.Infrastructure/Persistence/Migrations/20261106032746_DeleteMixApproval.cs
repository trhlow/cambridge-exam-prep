using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeleteMixApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_approval_requests_kind_values",
                table: "approval_requests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_approval_requests_kind_values",
                table: "approval_requests",
                sql: "kind IN ('DeletePaper', 'DeleteExam', 'DeleteCandidate', 'DeleteMix', 'ChangeStaffAccount', 'CreateAdmin')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_approval_requests_kind_values",
                table: "approval_requests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_approval_requests_kind_values",
                table: "approval_requests",
                sql: "kind IN ('DeletePaper', 'DeleteExam', 'DeleteCandidate', 'ChangeStaffAccount', 'CreateAdmin')");
        }
    }
}
