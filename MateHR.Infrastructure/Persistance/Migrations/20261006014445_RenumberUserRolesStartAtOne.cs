using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MateHR.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RenumberUserRolesStartAtOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UserRole paso a empezar en 1 (SuperAdmin=1 .. Employee=4) para que el
            // valor default del enum no sea un rol valido. EF no ve este cambio
            // porque el enum se persiste como int, asi que el remapeo va en SQL.
            //
            // Todas las filas se desplazan en una sola sentencia: sumar 1 a cada
            // valor no puede colisionar con una fila todavia sin migrar.
            migrationBuilder.Sql("UPDATE [Users] SET [Role] = [Role] + 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [Role] = [Role] - 1;");
        }
    }
}