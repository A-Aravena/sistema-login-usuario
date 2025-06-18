using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Inicio_Sesion_API.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    usuario_username = table.Column<string>(type: "longtext", nullable: false),
                    usuario_password = table.Column<string>(type: "longtext", nullable: false),
                    usuario_nombres = table.Column<string>(type: "longtext", nullable: false),
                    usuario_apellidos = table.Column<string>(type: "longtext", nullable: true),
                    usuario_rut = table.Column<string>(type: "longtext", nullable: true),
                    usuario_email = table.Column<string>(type: "longtext", nullable: true),
                    usuario_fono = table.Column<string>(type: "longtext", nullable: true),
                    usuario_url = table.Column<string>(type: "longtext", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.usuario_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
