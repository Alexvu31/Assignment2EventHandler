using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Assignment1EventHandler.Migrations
{
    /// <inheritdoc />
    public partial class Assignment3AttendeeOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendees_EventId",
                table: "Attendees");

            migrationBuilder.AddColumn<string>(
                name: "OrganizerUserId",
                table: "Events",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Attendees",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizerUserId",
                table: "Events",
                column: "OrganizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_EventId_UserId",
                table: "Attendees",
                columns: new[] { "EventId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_UserId",
                table: "Attendees",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_AspNetUsers_UserId",
                table: "Attendees",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_OrganizerUserId",
                table: "Events",
                column: "OrganizerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_AspNetUsers_UserId",
                table: "Attendees");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_OrganizerUserId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_OrganizerUserId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Attendees_EventId_UserId",
                table: "Attendees");

            migrationBuilder.DropIndex(
                name: "IX_Attendees_UserId",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "OrganizerUserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Attendees");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_EventId",
                table: "Attendees",
                column: "EventId");
        }
    }
}
