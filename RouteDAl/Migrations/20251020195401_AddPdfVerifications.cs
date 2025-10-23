using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvenDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfVerifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsBroadcast",
                table: "Events",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateTable(
                name: "PdfVerifications",
                columns: table => new
                {
                    PdfVerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PdfType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfVerifications", x => x.PdfVerificationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PdfVerifications_PdfVerificationId",
                table: "PdfVerifications",
                column: "PdfVerificationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdfVerifications");

            migrationBuilder.AlterColumn<bool>(
                name: "IsBroadcast",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
