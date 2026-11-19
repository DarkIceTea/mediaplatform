using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaPlatform.Media.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoLikes",
                columns: table => new
                {
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoLikes", x => new { x.VideoId, x.UserId });
                    table.ForeignKey(
                        name: "FK_VideoLikes_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoLikes");
        }
    }
}
