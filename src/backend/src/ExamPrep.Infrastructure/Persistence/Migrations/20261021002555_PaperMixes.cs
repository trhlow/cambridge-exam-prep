using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaperMixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mix_pattern",
                table: "test_papers",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mix_version",
                table: "test_papers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "paper_mix_id",
                table: "test_papers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "paper_mixes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    section_template_id = table.Column<int>(type: "int", nullable: false),
                    source_a_paper_id = table.Column<int>(type: "int", nullable: false),
                    source_b_paper_id = table.Column<int>(type: "int", nullable: false),
                    seed = table.Column<int>(type: "int", nullable: false),
                    created_by_user_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_mixes", x => x.id);
                    table.CheckConstraint("CK_paper_mixes_sources", "source_a_paper_id <> source_b_paper_id");
                    table.ForeignKey(
                        name: "FK_paper_mixes_section_templates_section_template_id",
                        column: x => x.section_template_id,
                        principalTable: "section_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_paper_mixes_test_papers_source_a_paper_id",
                        column: x => x.source_a_paper_id,
                        principalTable: "test_papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_paper_mixes_test_papers_source_b_paper_id",
                        column: x => x.source_b_paper_id,
                        principalTable: "test_papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_paper_mixes_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_test_papers_paper_mix_id_mix_version",
                table: "test_papers",
                columns: new[] { "paper_mix_id", "mix_version" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_test_papers_mix_version",
                table: "test_papers",
                sql: "(paper_mix_id IS NULL AND mix_version IS NULL AND mix_pattern IS NULL) OR (paper_mix_id IS NOT NULL AND mix_version BETWEEN 1 AND 4 AND mix_pattern IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_paper_mixes_created_by_user_id",
                table: "paper_mixes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_mixes_section_template_id",
                table: "paper_mixes",
                column: "section_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_mixes_source_a_paper_id",
                table: "paper_mixes",
                column: "source_a_paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_mixes_source_b_paper_id",
                table: "paper_mixes",
                column: "source_b_paper_id");

            migrationBuilder.AddForeignKey(
                name: "FK_test_papers_paper_mixes_paper_mix_id",
                table: "test_papers",
                column: "paper_mix_id",
                principalTable: "paper_mixes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_test_papers_paper_mixes_paper_mix_id",
                table: "test_papers");

            migrationBuilder.DropTable(
                name: "paper_mixes");

            migrationBuilder.DropIndex(
                name: "IX_test_papers_paper_mix_id_mix_version",
                table: "test_papers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_test_papers_mix_version",
                table: "test_papers");

            migrationBuilder.DropColumn(
                name: "mix_pattern",
                table: "test_papers");

            migrationBuilder.DropColumn(
                name: "mix_version",
                table: "test_papers");

            migrationBuilder.DropColumn(
                name: "paper_mix_id",
                table: "test_papers");
        }
    }
}
