using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZenBlog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class remove_blog_image_url : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Blogs"
                SET "CoverImageUrl" = "BlogImageUrl"
                WHERE "CoverImageUrl" IS NULL AND "BlogImageUrl" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "BlogImageUrl",
                table: "Blogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlogImageUrl",
                table: "Blogs",
                type: "text",
                nullable: true);
        }
    }
}
