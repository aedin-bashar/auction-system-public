using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM [AdminSystemSettings] WHERE [Key] = N'security.require2FaForAdmins';");

            migrationBuilder.DropTable(
                name: "UserTwoFactors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTwoFactors",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnabledSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PendingSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserTwoFactors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM [AdminSystemSettings]
                    WHERE [Key] = N'security.require2FaForAdmins')
                BEGIN
                    INSERT INTO [AdminSystemSettings] ([Key], [Value], [UpdatedAtUtc], [UpdatedByUserId])
                    SELECT TOP (1)
                        N'security.require2FaForAdmins',
                        N'true',
                        SYSUTCDATETIME(),
                        [Id]
                    FROM [Users]
                    WHERE [Role] = 1
                    ORDER BY [CreatedAtUtc], [Id];
                END
                """);
        }
    }
}
