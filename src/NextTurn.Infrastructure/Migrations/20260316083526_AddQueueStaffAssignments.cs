using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueStaffAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueueStaffAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueStaffAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueStaffAssignments_OrganisationId",
                table: "QueueStaffAssignments",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "UX_QueueStaffAssignments_QueueId_StaffUserId",
                table: "QueueStaffAssignments",
                columns: new[] { "QueueId", "StaffUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueStaffAssignments");
        }
    }
}
