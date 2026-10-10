using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "exam_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_types", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "section_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    exam_type_id = table.Column<int>(type: "int", nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false),
                    skill = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    part_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_section_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_section_templates_exam_types_exam_type_id",
                        column: x => x.exam_type_id,
                        principalTable: "exam_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    stored_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    duration_ms = table.Column<int>(type: "int", nullable: true),
                    uploaded_by_user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_assets_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "exams",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    exam_type_id = table.Column<int>(type: "int", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    room = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    session_pin = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    invigilator_password = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    seb_config_key_hash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true),
                    created_by_user_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exams", x => x.id);
                    table.ForeignKey(
                        name: "FK_exams_exam_types_exam_type_id",
                        column: x => x.exam_type_id,
                        principalTable: "exam_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exams_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_papers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    section_template_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    created_by_user_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_papers", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_papers_section_templates_section_template_id",
                        column: x => x.section_template_id,
                        principalTable: "section_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_papers_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "site_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    centre_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    logo_asset_id = table.Column<int>(type: "int", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_by_user_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_site_settings_assets_logo_asset_id",
                        column: x => x.logo_asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_site_settings_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "candidates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    exam_id = table.Column<int>(type: "int", nullable: false),
                    candidate_number = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    username = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    password_protected = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    seat_no = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    planned_extra_percent = table.Column<int>(type: "int", nullable: false),
                    parent_end_override_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    text_size = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    contrast = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidates", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidates_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "exam_invigilators",
                columns: table => new
                {
                    exam_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_invigilators", x => new { x.exam_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_exam_invigilators_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_invigilators_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "exam_sections",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    exam_id = table.Column<int>(type: "int", nullable: false),
                    section_template_id = table.Column<int>(type: "int", nullable: false),
                    test_paper_id = table.Column<int>(type: "int", nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_sections_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_sections_section_templates_section_template_id",
                        column: x => x.section_template_id,
                        principalTable: "section_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_sections_test_papers_test_paper_id",
                        column: x => x.test_paper_id,
                        principalTable: "test_papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "paper_parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    test_paper_id = table.Column<int>(type: "int", nullable: false),
                    part_no = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    stimulus_html = table.Column<string>(type: "mediumtext", nullable: true),
                    layout = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    audio_asset_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_paper_parts_assets_audio_asset_id",
                        column: x => x.audio_asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_paper_parts_test_papers_test_paper_id",
                        column: x => x.test_paper_id,
                        principalTable: "test_papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "candidate_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    exam_id = table.Column<int>(type: "int", nullable: false),
                    candidate_id = table.Column<int>(type: "int", nullable: true),
                    text = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    sent_by_user_id = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_messages_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_messages_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_messages_users_sent_by_user_id",
                        column: x => x.sent_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    candidate_id = table.Column<int>(type: "int", nullable: false),
                    exam_section_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    paused_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    paused_seconds = table.Column<int>(type: "int", nullable: false),
                    planned_extra_seconds = table.Column<int>(type: "int", nullable: false),
                    incident_extra_seconds = table.Column<int>(type: "int", nullable: false),
                    disregard_extra_time = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    end_at_override = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    submit_reason = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    connection_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    audio_started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    audio_adjust_ms = table.Column<int>(type: "int", nullable: false),
                    raw_score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    max_score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    grading_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_attempts_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attempts_exam_sections_exam_section_id",
                        column: x => x.exam_section_id,
                        principalTable: "exam_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "question_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    paper_part_id = table.Column<int>(type: "int", nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false),
                    interaction_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    instructions = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    body_html = table.Column<string>(type: "mediumtext", nullable: true),
                    option_pool_json = table.Column<string>(type: "json", nullable: true),
                    allow_option_reuse = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    max_words_per_answer = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_question_groups_paper_parts_paper_part_id",
                        column: x => x.paper_part_id,
                        principalTable: "paper_parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "invigilator_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    exam_id = table.Column<int>(type: "int", nullable: false),
                    attempt_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    payload_json = table.Column<string>(type: "json", nullable: true),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    batch_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invigilator_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_invigilator_actions_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_invigilator_actions_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invigilator_actions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "security_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    attempt_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    detail = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    detected_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    resolved_by_user_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_security_events_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_security_events_users_resolved_by_user_id",
                        column: x => x.resolved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    question_group_id = table.Column<int>(type: "int", nullable: false),
                    number = table.Column<int>(type: "int", nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false),
                    prompt_html = table.Column<string>(type: "text", nullable: true),
                    stimulus_html = table.Column<string>(type: "mediumtext", nullable: true),
                    options_json = table.Column<string>(type: "json", nullable: true),
                    points = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    min_words = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_question_groups_question_group_id",
                        column: x => x.question_group_id,
                        principalTable: "question_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "annotations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    attempt_id = table.Column<int>(type: "int", nullable: false),
                    paper_part_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: true),
                    kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    anchor_json = table.Column<string>(type: "json", nullable: false),
                    note_text = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annotations", x => x.id);
                    table.ForeignKey(
                        name: "FK_annotations_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_annotations_paper_parts_paper_part_id",
                        column: x => x.paper_part_id,
                        principalTable: "paper_parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_annotations_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "answer_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    is_case_sensitive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_keys", x => x.id);
                    table.ForeignKey(
                        name: "FK_answer_keys_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "responses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    attempt_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    is_flagged = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_correct = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    auto_score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_responses_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_responses_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "manual_marks",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    response_id = table.Column<long>(type: "bigint", nullable: false),
                    marker_user_id = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    max_score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    criteria_json = table.Column<string>(type: "json", nullable: true),
                    comment = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    marked_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manual_marks", x => x.id);
                    table.ForeignKey(
                        name: "FK_manual_marks_responses_response_id",
                        column: x => x.response_id,
                        principalTable: "responses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manual_marks_users_marker_user_id",
                        column: x => x.marker_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "exam_types",
                columns: new[] { "id", "code", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "A2_KEY_FS", true, "A2 Key for Schools" },
                    { 2, "B1_PRELIM_FS", true, "B1 Preliminary for Schools" }
                });

            migrationBuilder.InsertData(
                table: "site_settings",
                columns: new[] { "id", "centre_name", "logo_asset_id", "updated_at", "updated_by_user_id" },
                values: new object[] { 1, "Victory", null, new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "section_templates",
                columns: new[] { "id", "duration_minutes", "exam_type_id", "order_no", "part_count", "skill", "title" },
                values: new object[,]
                {
                    { 1, 60, 1, 1, 7, "ReadingWriting", "Reading & Writing" },
                    { 2, 30, 1, 2, 5, "Listening", "Listening" },
                    { 3, 45, 2, 1, 6, "Reading", "Reading" },
                    { 4, 45, 2, 2, 2, "Writing", "Writing" },
                    { 5, 30, 2, 3, 4, "Listening", "Listening" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_annotations_attempt_id",
                table: "annotations",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_annotations_paper_part_id",
                table: "annotations",
                column: "paper_part_id");

            migrationBuilder.CreateIndex(
                name: "IX_annotations_question_id",
                table: "annotations",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_answer_keys_question_id",
                table: "answer_keys",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_assets_sha256",
                table: "assets",
                column: "sha256");

            migrationBuilder.CreateIndex(
                name: "IX_assets_uploaded_by_user_id",
                table: "assets",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_candidate_id_exam_section_id",
                table: "attempts",
                columns: new[] { "candidate_id", "exam_section_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attempts_exam_section_id",
                table: "attempts",
                column: "exam_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_messages_candidate_id",
                table: "candidate_messages",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_messages_exam_id_sent_at",
                table: "candidate_messages",
                columns: new[] { "exam_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_messages_sent_by_user_id",
                table: "candidate_messages",
                column: "sent_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidates_exam_id_candidate_number",
                table: "candidates",
                columns: new[] { "exam_id", "candidate_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidates_username",
                table: "candidates",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_invigilators_user_id",
                table: "exam_invigilators",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_sections_exam_id_order_no",
                table: "exam_sections",
                columns: new[] { "exam_id", "order_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_sections_section_template_id",
                table: "exam_sections",
                column: "section_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_sections_test_paper_id",
                table: "exam_sections",
                column: "test_paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_types_code",
                table: "exam_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exams_created_by_user_id",
                table: "exams",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_exams_exam_type_id",
                table: "exams",
                column: "exam_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_exams_scheduled_date",
                table: "exams",
                column: "scheduled_date");

            migrationBuilder.CreateIndex(
                name: "IX_invigilator_actions_attempt_id",
                table: "invigilator_actions",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_invigilator_actions_batch_id",
                table: "invigilator_actions",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_invigilator_actions_exam_id_created_at",
                table: "invigilator_actions",
                columns: new[] { "exam_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_invigilator_actions_user_id",
                table: "invigilator_actions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_manual_marks_marker_user_id",
                table: "manual_marks",
                column: "marker_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_manual_marks_response_id",
                table: "manual_marks",
                column: "response_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_parts_audio_asset_id",
                table: "paper_parts",
                column: "audio_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_parts_test_paper_id_part_no",
                table: "paper_parts",
                columns: new[] { "test_paper_id", "part_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_groups_paper_part_id_order_no",
                table: "question_groups",
                columns: new[] { "paper_part_id", "order_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_questions_question_group_id_order_no",
                table: "questions",
                columns: new[] { "question_group_id", "order_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_responses_attempt_id_question_id",
                table: "responses",
                columns: new[] { "attempt_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_responses_question_id",
                table: "responses",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_section_templates_exam_type_id_order_no",
                table: "section_templates",
                columns: new[] { "exam_type_id", "order_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_events_attempt_id_detected_at",
                table: "security_events",
                columns: new[] { "attempt_id", "detected_at" });

            migrationBuilder.CreateIndex(
                name: "IX_security_events_resolved_by_user_id",
                table: "security_events",
                column: "resolved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_logo_asset_id",
                table: "site_settings",
                column: "logo_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_updated_by_user_id",
                table: "site_settings",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_papers_created_by_user_id",
                table: "test_papers",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_papers_section_template_id",
                table: "test_papers",
                column: "section_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annotations");

            migrationBuilder.DropTable(
                name: "answer_keys");

            migrationBuilder.DropTable(
                name: "candidate_messages");

            migrationBuilder.DropTable(
                name: "exam_invigilators");

            migrationBuilder.DropTable(
                name: "invigilator_actions");

            migrationBuilder.DropTable(
                name: "manual_marks");

            migrationBuilder.DropTable(
                name: "security_events");

            migrationBuilder.DropTable(
                name: "site_settings");

            migrationBuilder.DropTable(
                name: "responses");

            migrationBuilder.DropTable(
                name: "attempts");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "candidates");

            migrationBuilder.DropTable(
                name: "exam_sections");

            migrationBuilder.DropTable(
                name: "question_groups");

            migrationBuilder.DropTable(
                name: "exams");

            migrationBuilder.DropTable(
                name: "paper_parts");

            migrationBuilder.DropTable(
                name: "assets");

            migrationBuilder.DropTable(
                name: "test_papers");

            migrationBuilder.DropTable(
                name: "section_templates");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "exam_types");
        }
    }
}
