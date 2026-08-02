using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grantify.Migrations
{
    /// <inheritdoc />
    public partial class OfficerReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewedByName",
                table: "ScholarshipApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "ScholarshipApplications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedOn",
                table: "ScholarshipApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficerNote",
                table: "ApplicationDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByName",
                table: "ApplicationDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByUserId",
                table: "ApplicationDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedOn",
                table: "ApplicationDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationReviewLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScholarshipApplicationId = table.Column<int>(type: "int", nullable: false),
                    OfficerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationReviewLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationReviewLogs_ScholarshipApplications_ScholarshipApplicationId",
                        column: x => x.ScholarshipApplicationId,
                        principalTable: "ScholarshipApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationReviewLogs_ScholarshipApplicationId",
                table: "ApplicationReviewLogs",
                column: "ScholarshipApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationReviewLogs");

            migrationBuilder.DropColumn(
                name: "ReviewedByName",
                table: "ScholarshipApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ScholarshipApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedOn",
                table: "ScholarshipApplications");

            migrationBuilder.DropColumn(
                name: "OfficerNote",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedByName",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedOn",
                table: "ApplicationDocuments");
        }
    }
}
