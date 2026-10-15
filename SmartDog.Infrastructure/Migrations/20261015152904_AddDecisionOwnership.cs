using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Decisions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_OwnerId_CreatedAt",
                table: "Decisions",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_OwnerId",
                table: "Decisions",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_OwnerId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_OwnerId_CreatedAt",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Decisions");
        }
    }
}
