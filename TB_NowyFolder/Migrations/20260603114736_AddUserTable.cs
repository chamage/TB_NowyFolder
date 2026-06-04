using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TB_NowyFolder.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GuestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Guests_GuestID",
                        column: x => x.GuestID,
                        principalTable: "Guests",
                        principalColumn: "GuestID");
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "GuestID", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, null, "AQAAAAIAAYagAAAAEDIxCxLk7cO67wzbcIxZEhSNWwO3N7OB3apVA/gpSSaDEx9E2cO0kFL8kaMZmlw3qA==", "Administrator", "admin" },
                    { 2, null, "AQAAAAIAAYagAAAAEML12Nj+jhhywZ/TBEuyFOCAoQWcbiIiZXnp8fkBYkYBdViiElzI/uHC6vI3OqpAHA==", "Receptionist", "reception" },
                    { 3, 1, "AQAAAAIAAYagAAAAEIXuk4hfcIORPrlAC3EANB5kTeEiXf/QpfoTuRSCfUVNFqzvGgXCYsc8gzDjMyKiPg==", "Client", "client" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_GuestID",
                table: "Users",
                column: "GuestID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
