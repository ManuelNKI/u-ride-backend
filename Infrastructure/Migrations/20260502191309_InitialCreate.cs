using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    FirebaseUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Career = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Zone = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    RatingSum = table.Column<int>(type: "int", nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    TripsCount = table.Column<int>(type: "int", nullable: false),
                    DriverTripsCount = table.Column<int>(type: "int", nullable: false),
                    PassengerTripsCount = table.Column<int>(type: "int", nullable: false),
                    SuspendedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.FirebaseUid);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserUid",
                        column: x => x.UserUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OriginZone = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DestinationZone = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OriginLat = table.Column<double>(type: "float", nullable: true),
                    OriginLng = table.Column<double>(type: "float", nullable: true),
                    DestinationLat = table.Column<double>(type: "float", nullable: true),
                    DestinationLng = table.Column<double>(type: "float", nullable: true),
                    DepartureAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeatsTotal = table.Column<int>(type: "int", nullable: false),
                    SeatsAvailable = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfirmedPassengerUids = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vehicle_Plate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Vehicle_Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Vehicle_Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Vehicle_Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rules_Punctuality = table.Column<bool>(type: "bit", nullable: false),
                    Rules_Respect = table.Column<bool>(type: "bit", nullable: false),
                    Rules_NoSensitiveData = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Users_DriverUid",
                        column: x => x.DriverUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReportedUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReportedUid",
                        column: x => x.ReportedUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterUid",
                        column: x => x.ReporterUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ToUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Stars = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_FromUid",
                        column: x => x.FromUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ToUid",
                        column: x => x.ToUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassengerUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PassengerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DriverRated = table.Column<bool>(type: "bit", nullable: false),
                    DriverReported = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRequests_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripRequests_Users_PassengerUid",
                        column: x => x.PassengerUid,
                        principalTable: "Users",
                        principalColumn: "FirebaseUid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserUid",
                table: "Notifications",
                column: "UserUid");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserUid_Read",
                table: "Notifications",
                columns: new[] { "UserUid", "Read" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedUid",
                table: "Reports",
                column: "ReportedUid");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUid",
                table: "Reports",
                column: "ReporterUid");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TripId",
                table: "Reports",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_FromUid",
                table: "Reviews",
                column: "FromUid");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ToUid",
                table: "Reviews",
                column: "ToUid");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TripId",
                table: "Reviews",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_PassengerUid",
                table: "TripRequests",
                column: "PassengerUid");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_TripId_PassengerUid",
                table: "TripRequests",
                columns: new[] { "TripId", "PassengerUid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DepartureAt",
                table: "Trips",
                column: "DepartureAt");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverUid",
                table: "Trips",
                column: "DriverUid");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OriginZone_DestinationZone",
                table: "Trips",
                columns: new[] { "OriginZone", "DestinationZone" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status",
                table: "Trips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "TripRequests");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
