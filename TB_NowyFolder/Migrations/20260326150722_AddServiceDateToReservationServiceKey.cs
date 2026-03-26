using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB_NowyFolder.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDateToReservationServiceKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationServices",
                table: "ReservationServices");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationServices",
                table: "ReservationServices",
                columns: new[] { "ReservationID", "ServiceID", "ServiceDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationServices",
                table: "ReservationServices");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationServices",
                table: "ReservationServices",
                columns: new[] { "ReservationID", "ServiceID" });
        }
    }
}
