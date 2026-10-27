using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attempts_candidates_candidate_id",
                table: "attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_attempts_exam_sections_exam_section_id",
                table: "attempts");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_role_values",
                table: "users",
                sql: "role IN ('Admin', 'Invigilator', 'Manager', 'Examiner')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_areas_area_values",
                table: "user_areas",
                sql: "area IN ('Papers', 'Exams', 'Candidates', 'Centre')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_test_papers_status_values",
                table: "test_papers",
                sql: "status IN ('Draft', 'Published', 'Archived')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_site_settings_single_row",
                table: "site_settings",
                sql: "id = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_security_events_type_values",
                table: "security_events",
                sql: "type IN ('InvalidSebKey', 'LockdownBrowserExited', 'ConnectionLost', 'ClockSkew', 'StartedEarly')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_section_templates_skill_values",
                table: "section_templates",
                sql: "skill IN ('Reading', 'Writing', 'ReadingWriting', 'Listening')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_responses_selection_values",
                table: "responses",
                sql: "selection IN ('Undecided', 'Yes', 'No')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_question_groups_interaction_type_values",
                table: "question_groups",
                sql: "interaction_type IN ('SingleChoice', 'ImageChoice', 'InlineChoice', 'InlineText', 'DragDropMatch', 'Essay')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_paper_parts_layout_values",
                table: "paper_parts",
                sql: "layout IN ('Single', 'Split')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invigilator_actions_action_type_values",
                table: "invigilator_actions",
                sql: "action_type IN ('Pause', 'Resume', 'AddIncidentExtraTime', 'EditEndTime', 'EndNow', 'UseTestEndTime', 'RewindAudio', 'SendMessage', 'AnnounceStart')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_exams_seb_config_key",
                table: "exams",
                sql: "seb_config_key_hash IS NULL OR REGEXP_LIKE(seb_config_key_hash, '^[0-9a-f]{64}$', 'c')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_exams_session_pin",
                table: "exams",
                sql: "REGEXP_LIKE(session_pin, '^[0-9]{4,8}$', 'c')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_exams_status_values",
                table: "exams",
                sql: "status IN ('Draft', 'Scheduled', 'Active', 'Closed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_candidates_contrast_values",
                table: "candidates",
                sql: "contrast IN ('BlackOnWhite', 'WhiteOnBlack', 'YellowOnBlack')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_candidates_text_size_values",
                table: "candidates",
                sql: "text_size IN ('Regular', 'Large', 'ExtraLarge')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_connection_status_values",
                table: "attempts",
                sql: "connection_status IN ('Offline', 'Online')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_grading_status_values",
                table: "attempts",
                sql: "grading_status IN ('NotGraded', 'AutoGraded', 'NeedsManualMarking', 'Final')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_score",
                table: "attempts",
                sql: "(raw_score IS NULL OR max_score IS NOT NULL) AND (raw_score IS NULL OR (raw_score >= 0 AND raw_score <= max_score)) AND (max_score IS NULL OR max_score >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_state",
                table: "attempts",
                sql: "(status = 'NotStarted' OR started_at IS NOT NULL) AND ((status = 'Paused') = (paused_at IS NOT NULL)) AND ((status = 'Submitted') = (submitted_at IS NOT NULL AND submit_reason IS NOT NULL)) AND (grading_status = 'NotGraded' OR status = 'Submitted')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_status_values",
                table: "attempts",
                sql: "status IN ('NotStarted', 'InProgress', 'Paused', 'Submitted')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_attempts_submit_reason_values",
                table: "attempts",
                sql: "submit_reason IS NULL OR submit_reason IN ('Candidate', 'TimeExpired', 'InvigilatorEndNow')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_assets_kind_values",
                table: "assets",
                sql: "kind IN ('Image', 'Audio')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_approval_requests_decision",
                table: "approval_requests",
                sql: "(status = 'Pending') = (decided_by_user_id IS NULL AND decided_at IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_approval_requests_kind_values",
                table: "approval_requests",
                sql: "kind IN ('DeletePaper', 'DeleteExam', 'DeleteCandidate', 'ChangeStaffAccount', 'CreateAdmin')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_approval_requests_status_values",
                table: "approval_requests",
                sql: "status IN ('Pending', 'Approved', 'Rejected', 'Cancelled', 'Failed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_annotations_kind_values",
                table: "annotations",
                sql: "kind IN ('Highlight', 'Note')");

            migrationBuilder.AddForeignKey(
                name: "FK_attempts_candidates_candidate_id",
                table: "attempts",
                column: "candidate_id",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_attempts_exam_sections_exam_section_id",
                table: "attempts",
                column: "exam_section_id",
                principalTable: "exam_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attempts_candidates_candidate_id",
                table: "attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_attempts_exam_sections_exam_section_id",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_role_values",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_user_areas_area_values",
                table: "user_areas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_test_papers_status_values",
                table: "test_papers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_site_settings_single_row",
                table: "site_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_security_events_type_values",
                table: "security_events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_section_templates_skill_values",
                table: "section_templates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_responses_selection_values",
                table: "responses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_question_groups_interaction_type_values",
                table: "question_groups");

            migrationBuilder.DropCheckConstraint(
                name: "CK_paper_parts_layout_values",
                table: "paper_parts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invigilator_actions_action_type_values",
                table: "invigilator_actions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_exams_seb_config_key",
                table: "exams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_exams_session_pin",
                table: "exams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_exams_status_values",
                table: "exams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_candidates_contrast_values",
                table: "candidates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_candidates_text_size_values",
                table: "candidates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_connection_status_values",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_grading_status_values",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_score",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_state",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_status_values",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_attempts_submit_reason_values",
                table: "attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_assets_kind_values",
                table: "assets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_approval_requests_decision",
                table: "approval_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_approval_requests_kind_values",
                table: "approval_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_approval_requests_status_values",
                table: "approval_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_annotations_kind_values",
                table: "annotations");

            migrationBuilder.AddForeignKey(
                name: "FK_attempts_candidates_candidate_id",
                table: "attempts",
                column: "candidate_id",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attempts_exam_sections_exam_section_id",
                table: "attempts",
                column: "exam_section_id",
                principalTable: "exam_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
