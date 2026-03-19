using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganisationSlugLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Organisations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Use dynamic SQL so generated idempotent scripts don't fail SQL Server
            // compile-time name resolution when Slug was added earlier in the same batch.
            migrationBuilder.Sql("""
                DECLARE @sql nvarchar(max) = N'
                UPDATE [Organisations]
                SET [Slug] = CONCAT(''org-'', LOWER(SUBSTRING(CONVERT(varchar(36), [Id]), 1, 8)))
                WHERE [Slug] IS NULL OR LTRIM(RTRIM([Slug])) = ''''';
                EXEC sp_executesql @sql;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Organisations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_Slug",
                table: "Organisations",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organisations_Slug",
                table: "Organisations");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Organisations");
        }
    }
}
