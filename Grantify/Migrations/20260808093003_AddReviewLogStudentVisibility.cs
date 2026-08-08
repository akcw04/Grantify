using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grantify.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewLogStudentVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStudentVisible",
                table: "ApplicationReviewLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // History written before this column existed gets the flag it would
            // have been given. A row that recorded a real change of status was a
            // decision the student was already told about, so it stays visible.
            // Everything else — document notes, notes re-saved without a status
            // change — keeps the new default of false and stays internal.
            migrationBuilder.Sql(@"
                UPDATE [ApplicationReviewLogs]
                SET [IsStudentVisible] = 1
                WHERE [ToStatus] IS NOT NULL
                  AND ([FromStatus] IS NULL OR [FromStatus] <> [ToStatus]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStudentVisible",
                table: "ApplicationReviewLogs");
        }
    }
}
