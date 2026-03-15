using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppointmentActiveIndexForReschedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                table: "Appointments",
                columns: new[] { "OrganisationId", "SlotStart", "SlotEnd" },
                unique: true,
                filter: "[Status] <> 'Cancelled' AND [Status] <> 'Rescheduled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                table: "Appointments",
                columns: new[] { "OrganisationId", "SlotStart", "SlotEnd" },
                unique: true,
                filter: "[Status] <> 'Cancelled'");
        }
    }
}
