using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudeCereal.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Cereals_DeletedAt",
                table: "Cereals",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cereals_DeletedAt",
                table: "Cereals");
        }
    }
}
