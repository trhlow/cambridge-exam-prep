using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EndAllAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_invigilator_actions_action_type_values",
                table: "invigilator_actions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invigilator_actions_action_type_values",
                table: "invigilator_actions",
                sql: "action_type IN ('Pause', 'Resume', 'AddIncidentExtraTime', 'EditEndTime', 'EndNow', 'UseTestEndTime', 'RewindAudio', 'SendMessage', 'AnnounceStart', 'EndAll')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_invigilator_actions_action_type_values",
                table: "invigilator_actions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invigilator_actions_action_type_values",
                table: "invigilator_actions",
                sql: "action_type IN ('Pause', 'Resume', 'AddIncidentExtraTime', 'EditEndTime', 'EndNow', 'UseTestEndTime', 'RewindAudio', 'SendMessage', 'AnnounceStart')");
        }
    }
}
