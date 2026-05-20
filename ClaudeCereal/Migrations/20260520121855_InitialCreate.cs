using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudeCereal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cereals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Mfr = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Calories = table.Column<int>(type: "INTEGER", nullable: true),
                    Protein = table.Column<int>(type: "INTEGER", nullable: true),
                    Fat = table.Column<int>(type: "INTEGER", nullable: true),
                    Sodium = table.Column<int>(type: "INTEGER", nullable: true),
                    Fiber = table.Column<double>(type: "REAL", nullable: true),
                    Carbo = table.Column<double>(type: "REAL", nullable: true),
                    Sugars = table.Column<int>(type: "INTEGER", nullable: true),
                    Potass = table.Column<int>(type: "INTEGER", nullable: true),
                    Vitamins = table.Column<int>(type: "INTEGER", nullable: true),
                    Shelf = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<double>(type: "REAL", nullable: true),
                    Cups = table.Column<double>(type: "REAL", nullable: true),
                    Rating = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cereals", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cereals");
        }
    }
}
