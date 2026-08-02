using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grantify.Migrations
{
    /// <inheritdoc />
    public partial class FixApplicationStudentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipApplications_AspNetUsers_StudentId",
                table: "ScholarshipApplications");

            migrationBuilder.DropIndex(
                name: "IX_ScholarshipApplications_StudentId",
                table: "ScholarshipApplications");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "ScholarshipApplications");

            migrationBuilder.AlterColumn<string>(
                name: "StudentUserId",
                table: "ScholarshipApplications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipApplications_StudentUserId",
                table: "ScholarshipApplications",
                column: "StudentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipApplications_AspNetUsers_StudentUserId",
                table: "ScholarshipApplications",
                column: "StudentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipApplications_AspNetUsers_StudentUserId",
                table: "ScholarshipApplications");

            migrationBuilder.DropIndex(
                name: "IX_ScholarshipApplications_StudentUserId",
                table: "ScholarshipApplications");

            migrationBuilder.AlterColumn<string>(
                name: "StudentUserId",
                table: "ScholarshipApplications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "ScholarshipApplications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipApplications_StudentId",
                table: "ScholarshipApplications",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipApplications_AspNetUsers_StudentId",
                table: "ScholarshipApplications",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
