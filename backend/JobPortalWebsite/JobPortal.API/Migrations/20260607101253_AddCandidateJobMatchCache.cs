using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JobPortal.API.Migrations;

/// <inheritdoc />
public partial class AddCandidateJobMatchCache : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CandidateJobMatchCaches",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                JobId = table.Column<int>(type: "integer", nullable: false),
                ProfileUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Score = table.Column<int>(type: "integer", nullable: false),
                StrengthsJson = table.Column<string>(type: "text", nullable: true),
                GapsJson = table.Column<string>(type: "text", nullable: true),
                Summary = table.Column<string>(type: "text", nullable: true),
                Tip = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CandidateJobMatchCaches", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CandidateJobMatchCaches_UserId_JobId",
            table: "CandidateJobMatchCaches",
            columns: new[] { "UserId", "JobId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CandidateJobMatchCaches");
    }
}
