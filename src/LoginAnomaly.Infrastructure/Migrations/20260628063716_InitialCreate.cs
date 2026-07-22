using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginAnomaly.Infrastructure.Migrations
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnownDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    FirstSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnownDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoginEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoginSucceeded = table.Column<bool>(type: "bit", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    IsSimulated = table.Column<bool>(type: "bit", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginEventId = table.Column<long>(type: "bigint", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_LoginEvents_LoginEventId",
                        column: x => x.LoginEventId,
                        principalTable: "LoginEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleHits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginEventId = table.Column<long>(type: "bigint", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleHits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleHits_LoginEvents_LoginEventId",
                        column: x => x.LoginEventId,
                        principalTable: "LoginEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsAcknowledged",
                table: "Alerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_LoginEventId",
                table: "Alerts",
                column: "LoginEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnownDevices_UserId_DeviceFingerprint",
                table: "KnownDevices",
                columns: new[] { "UserId", "DeviceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_IpAddress",
                table: "LoginEvents",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_TimestampUtc",
                table: "LoginEvents",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_UserId",
                table: "LoginEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_Username",
                table: "LoginEvents",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_Username_TimestampUtc",
                table: "LoginEvents",
                columns: new[] { "Username", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleHits_LoginEventId",
                table: "RuleHits",
                column: "LoginEventId");

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
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "KnownDevices");

            migrationBuilder.DropTable(
                name: "RuleHits");

            migrationBuilder.DropTable(
                name: "LoginEvents");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
