using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalNodesToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonalNodeId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessKeyHash",
                table: "Robots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "Robots",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "Robots",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Robots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetNodeId",
                table: "Robots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalNodeId",
                table: "Users",
                column: "PersonalNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_SerialNumber",
                table: "Robots",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Robots_TargetNodeId",
                table: "Robots",
                column: "TargetNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Nodes_TargetNodeId",
                table: "Robots",
                column: "TargetNodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Nodes_PersonalNodeId",
                table: "Users",
                column: "PersonalNodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Nodes_TargetNodeId",
                table: "Robots");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Nodes_PersonalNodeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonalNodeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Robots_SerialNumber",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_Robots_TargetNodeId",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "PersonalNodeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccessKeyHash",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "TargetNodeId",
                table: "Robots");
        }
    }
}
