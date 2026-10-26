using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReporting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentSourceAndMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SourceMessageId",
                table: "Incidents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_SourceMessageId",
                table: "Incidents",
                column: "SourceMessageId",
                unique: true,
                filter: "[SourceMessageId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_SourceMessageId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SourceMessageId",
                table: "Incidents");
        }
    }
}
