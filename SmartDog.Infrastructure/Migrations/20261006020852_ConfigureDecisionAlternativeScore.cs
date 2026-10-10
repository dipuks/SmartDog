using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureDecisionAlternativeScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Decisions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Decisions");
        }
    }
}
