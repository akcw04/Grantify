using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grantify.Migrations
{
    /// <inheritdoc />
    public partial class AddScholarshipMasterDataLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstitutionId",
                table: "Scholarships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntakePeriodId",
                table: "Scholarships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScholarshipCategoryId",
                table: "Scholarships",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_InstitutionId",
                table: "Scholarships",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_IntakePeriodId",
                table: "Scholarships",
                column: "IntakePeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_ScholarshipCategoryId",
                table: "Scholarships",
                column: "ScholarshipCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scholarships_Institutions_InstitutionId",
                table: "Scholarships",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scholarships_IntakePeriods_IntakePeriodId",
                table: "Scholarships",
                column: "IntakePeriodId",
                principalTable: "IntakePeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scholarships_ScholarshipCategories_ScholarshipCategoryId",
                table: "Scholarships",
                column: "ScholarshipCategoryId",
                principalTable: "ScholarshipCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scholarships_Institutions_InstitutionId",
                table: "Scholarships");

            migrationBuilder.DropForeignKey(
                name: "FK_Scholarships_IntakePeriods_IntakePeriodId",
                table: "Scholarships");

            migrationBuilder.DropForeignKey(
                name: "FK_Scholarships_ScholarshipCategories_ScholarshipCategoryId",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_Scholarships_InstitutionId",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_Scholarships_IntakePeriodId",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_Scholarships_ScholarshipCategoryId",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "IntakePeriodId",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "ScholarshipCategoryId",
                table: "Scholarships");
        }
    }
}
