using Microsoft.EntityFrameworkCore.Migrations;

namespace Contoso.Construction.Migrations
{
    public partial class jobid_col_added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobSitePhotos_Jobs_JobId",
                table: "JobSitePhotos");

            migrationBuilder.AlterColumn<int>(
                name: "JobId",
                table: "JobSitePhotos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSitePhotos_Jobs_JobId",
                table: "JobSitePhotos",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobSitePhotos_Jobs_JobId",
                table: "JobSitePhotos");

            migrationBuilder.AlterColumn<int>(
                name: "JobId",
                table: "JobSitePhotos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_JobSitePhotos_Jobs_JobId",
                table: "JobSitePhotos",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
