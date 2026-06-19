using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoScorer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Searches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Criteria_City = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Criteria_PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Criteria_PropertyType = table.Column<string>(type: "TEXT", nullable: false),
                    Criteria_MinPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    Criteria_MaxPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    Criteria_MinArea = table.Column<decimal>(type: "TEXT", nullable: true),
                    Criteria_MaxArea = table.Column<decimal>(type: "TEXT", nullable: true),
                    Criteria_MinRooms = table.Column<int>(type: "INTEGER", nullable: true),
                    Criteria_MaxRooms = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Area = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rooms = table.Column<int>(type: "INTEGER", nullable: true),
                    Floor = table.Column<int>(type: "INTEGER", nullable: true),
                    EnergyRating = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    PricePerM2 = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReferencePricePerM2 = table.Column<decimal>(type: "TEXT", nullable: false),
                    OriginalUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ScrapedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address_PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Address_Neighborhood = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Price_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price_Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Score_Value = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreBreakdown_PriceGapScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreBreakdown_AreaScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreBreakdown_FloorScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreBreakdown_EnergyScore = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Searches_CreatedAt",
                table: "Searches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_SearchId_Score_Value",
                table: "Listings",
                columns: new[] { "SearchId", "Score_Value" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Source_ExternalId",
                table: "Listings",
                columns: new[] { "Source", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Listings");
            migrationBuilder.DropTable(name: "Searches");
        }
    }
}
