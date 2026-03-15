using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AppointmentScheduleRules_OrganisationId_DayOfWeek",
                table: "AppointmentScheduleRules");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_OrganisationId_SlotStart",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                table: "Appointments");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentProfileId",
                table: "AppointmentScheduleRules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentProfileId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AppointmentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShareableLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProfiles", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO AppointmentProfiles (Id, OrganisationId, Name, IsActive, ShareableLink)
                SELECT
                    NEWID(),
                    src.OrganisationId,
                    'Default Appointments',
                    1,
                    ''
                FROM (
                    SELECT DISTINCT OrganisationId FROM Appointments
                    UNION
                    SELECT DISTINCT OrganisationId FROM AppointmentScheduleRules
                ) AS src;
                """);

            migrationBuilder.Sql(
                """
                UPDATE p
                SET p.ShareableLink = '/appointments/' + CONVERT(nvarchar(36), p.OrganisationId) + '/' + CONVERT(nvarchar(36), p.Id)
                FROM AppointmentProfiles p
                WHERE p.ShareableLink = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE a
                SET a.AppointmentProfileId = p.Id
                FROM Appointments a
                INNER JOIN AppointmentProfiles p
                    ON p.OrganisationId = a.OrganisationId
                   AND p.Name = 'Default Appointments'
                WHERE a.AppointmentProfileId = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.Sql(
                """
                UPDATE r
                SET r.AppointmentProfileId = p.Id
                FROM AppointmentScheduleRules r
                INNER JOIN AppointmentProfiles p
                    ON p.OrganisationId = r.OrganisationId
                   AND p.Name = 'Default Appointments'
                WHERE r.AppointmentProfileId = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "UX_AppointmentScheduleRules_OrganisationId_ProfileId_DayOfWeek",
                table: "AppointmentScheduleRules",
                columns: new[] { "OrganisationId", "AppointmentProfileId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganisationId_ProfileId_SlotStart",
                table: "Appointments",
                columns: new[] { "OrganisationId", "AppointmentProfileId", "SlotStart" });

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_OrganisationId_ProfileId_SlotStart_SlotEnd_Active",
                table: "Appointments",
                columns: new[] { "OrganisationId", "AppointmentProfileId", "SlotStart", "SlotEnd" },
                unique: true,
                filter: "[Status] <> 'Cancelled' AND [Status] <> 'Rescheduled'");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfiles_OrganisationId",
                table: "AppointmentProfiles",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "UX_AppointmentProfiles_OrganisationId_Name",
                table: "AppointmentProfiles",
                columns: new[] { "OrganisationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentProfiles");

            migrationBuilder.DropIndex(
                name: "UX_AppointmentScheduleRules_OrganisationId_ProfileId_DayOfWeek",
                table: "AppointmentScheduleRules");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_OrganisationId_ProfileId_SlotStart",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "UX_Appointments_OrganisationId_ProfileId_SlotStart_SlotEnd_Active",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentProfileId",
                table: "AppointmentScheduleRules");

            migrationBuilder.DropColumn(
                name: "AppointmentProfileId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "UX_AppointmentScheduleRules_OrganisationId_DayOfWeek",
                table: "AppointmentScheduleRules",
                columns: new[] { "OrganisationId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganisationId_SlotStart",
                table: "Appointments",
                columns: new[] { "OrganisationId", "SlotStart" });

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                table: "Appointments",
                columns: new[] { "OrganisationId", "SlotStart", "SlotEnd" },
                unique: true,
                filter: "[Status] <> 'Cancelled' AND [Status] <> 'Rescheduled'");
        }
    }
}
