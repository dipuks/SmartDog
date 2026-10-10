using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionCriteriaAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_DecisionAlternatives_Id_DecisionId",
                table: "DecisionAlternatives",
                columns: new[] { "Id", "DecisionId" });

            migrationBuilder.CreateTable(
                name: "DecisionCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionCriteria", x => x.Id);
                    table.UniqueConstraint("AK_DecisionCriteria_Id_DecisionId", x => new { x.Id, x.DecisionId });
                    table.ForeignKey(
                        name: "FK_DecisionCriteria_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "Decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecisionAlternativeCriterionRatings",
                columns: table => new
                {
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionAlternativeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionAlternativeCriterionRatings", x => new { x.DecisionId, x.DecisionAlternativeId, x.DecisionCriterionId });
                    table.ForeignKey(
                        name: "FK_DecisionAlternativeCriterionRatings_DecisionAlternatives_DecisionAlternativeId_DecisionId",
                        columns: x => new { x.DecisionAlternativeId, x.DecisionId },
                        principalTable: "DecisionAlternatives",
                        principalColumns: new[] { "Id", "DecisionId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecisionAlternativeCriterionRatings_DecisionCriteria_DecisionCriterionId_DecisionId",
                        columns: x => new { x.DecisionCriterionId, x.DecisionId },
                        principalTable: "DecisionCriteria",
                        principalColumns: new[] { "Id", "DecisionId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionAlternativeCriterionRatings_DecisionAlternativeId_DecisionId",
                table: "DecisionAlternativeCriterionRatings",
                columns: new[] { "DecisionAlternativeId", "DecisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionAlternativeCriterionRatings_DecisionCriterionId_DecisionId",
                table: "DecisionAlternativeCriterionRatings",
                columns: new[] { "DecisionCriterionId", "DecisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionCriteria_DecisionId",
                table: "DecisionCriteria",
                column: "DecisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionAlternativeCriterionRatings");

            migrationBuilder.DropTable(
                name: "DecisionCriteria");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DecisionAlternatives_Id_DecisionId",
                table: "DecisionAlternatives");
        }
    }
}
