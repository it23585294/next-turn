using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueServingUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_QueueEntries_QueueId_OneServing",
                table: "QueueEntries",
                column: "QueueId",
                unique: true,
                filter: "[Status] = 'Serving'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_QueueEntries_QueueId_OneServing",
                table: "QueueEntries");
        }
    }
}
