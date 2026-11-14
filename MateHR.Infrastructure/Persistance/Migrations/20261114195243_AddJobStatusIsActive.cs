using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MateHR.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddJobStatusIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "JobStatuses",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "JobStatuses");
        }
    }
}
