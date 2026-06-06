using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentBridge.Migrations
{
    /// <inheritdoc />
    public partial class AddAfterTreatmentImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAfterTreatment",
                table: "CaseImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAfterTreatment",
                table: "CaseImages");
        }
    }
}
