using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPrep.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// VE-70 (Victory 29/10): a mix takes 2–6 source papers (<c>paper_mix_sources</c>) and makes 4–18 papers
    /// (<c>paper_mixes.version_count</c>). Existing mixes keep their two sources as A and B and their four papers.
    /// </summary>
    public partial class MixManyPapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paper_mix_sources",
                columns: table => new
                {
                    paper_mix_id = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<int>(type: "int", nullable: false),
                    test_paper_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_mix_sources", x => new { x.paper_mix_id, x.position });
                    table.CheckConstraint("CK_paper_mix_sources_position", "position BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_paper_mix_sources_paper_mixes_paper_mix_id",
                        column: x => x.paper_mix_id,
                        principalTable: "paper_mixes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_mix_sources_test_papers_test_paper_id",
                        column: x => x.test_paper_id,
                        principalTable: "test_papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql(
                "INSERT INTO paper_mix_sources (paper_mix_id, position, test_paper_id) " +
                "SELECT id, 1, source_a_paper_id FROM paper_mixes UNION ALL SELECT id, 2, source_b_paper_id FROM paper_mixes;");

            migrationBuilder.AddColumn<int>(
                name: "version_count",
                table: "paper_mixes",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.DropForeignKey(name: "FK_paper_mixes_test_papers_source_a_paper_id", table: "paper_mixes");
            migrationBuilder.DropForeignKey(name: "FK_paper_mixes_test_papers_source_b_paper_id", table: "paper_mixes");
            migrationBuilder.DropCheckConstraint(name: "CK_paper_mixes_sources", table: "paper_mixes");
            migrationBuilder.DropIndex(name: "IX_paper_mixes_source_a_paper_id", table: "paper_mixes");
            migrationBuilder.DropIndex(name: "IX_paper_mixes_source_b_paper_id", table: "paper_mixes");
            migrationBuilder.DropColumn(name: "source_a_paper_id", table: "paper_mixes");
            migrationBuilder.DropColumn(name: "source_b_paper_id", table: "paper_mixes");

            migrationBuilder.DropCheckConstraint(name: "CK_test_papers_mix_version", table: "test_papers");
            migrationBuilder.AddCheckConstraint(
                name: "CK_test_papers_mix_version",
                table: "test_papers",
                sql: "(paper_mix_id IS NULL AND mix_version IS NULL AND mix_pattern IS NULL) OR (paper_mix_id IS NOT NULL AND mix_version BETWEEN 1 AND 18 AND mix_pattern IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_paper_mixes_version_count",
                table: "paper_mixes",
                sql: "version_count BETWEEN 4 AND 18");

            migrationBuilder.CreateIndex(
                name: "IX_paper_mix_sources_paper_mix_id_test_paper_id",
                table: "paper_mix_sources",
                columns: new[] { "paper_mix_id", "test_paper_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_mix_sources_test_paper_id",
                table: "paper_mix_sources",
                column: "test_paper_id");
        }

        /// <summary>
        /// Back to two sources and four papers: a mix of more papers makes the restored check on <c>mix_version</c> fail,
        /// so the database refuses to go back while one exists.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_test_papers_mix_version", table: "test_papers");
            migrationBuilder.DropCheckConstraint(name: "CK_paper_mixes_version_count", table: "paper_mixes");

            migrationBuilder.AddColumn<int>(name: "source_a_paper_id", table: "paper_mixes", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "source_b_paper_id", table: "paper_mixes", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.Sql(
                "UPDATE paper_mixes m SET " +
                "source_a_paper_id = (SELECT test_paper_id FROM paper_mix_sources s WHERE s.paper_mix_id = m.id AND s.position = 1), " +
                "source_b_paper_id = (SELECT test_paper_id FROM paper_mix_sources s WHERE s.paper_mix_id = m.id AND s.position = 2);");
            migrationBuilder.DropColumn(name: "version_count", table: "paper_mixes");
            migrationBuilder.DropTable(name: "paper_mix_sources");

            migrationBuilder.AddCheckConstraint(
                name: "CK_test_papers_mix_version",
                table: "test_papers",
                sql: "(paper_mix_id IS NULL AND mix_version IS NULL AND mix_pattern IS NULL) OR (paper_mix_id IS NOT NULL AND mix_version BETWEEN 1 AND 4 AND mix_pattern IS NOT NULL)");
            migrationBuilder.CreateIndex(name: "IX_paper_mixes_source_a_paper_id", table: "paper_mixes", column: "source_a_paper_id");
            migrationBuilder.CreateIndex(name: "IX_paper_mixes_source_b_paper_id", table: "paper_mixes", column: "source_b_paper_id");
            migrationBuilder.AddCheckConstraint(name: "CK_paper_mixes_sources", table: "paper_mixes", sql: "source_a_paper_id <> source_b_paper_id");
            migrationBuilder.AddForeignKey(
                name: "FK_paper_mixes_test_papers_source_a_paper_id",
                table: "paper_mixes",
                column: "source_a_paper_id",
                principalTable: "test_papers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_paper_mixes_test_papers_source_b_paper_id",
                table: "paper_mixes",
                column: "source_b_paper_id",
                principalTable: "test_papers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
