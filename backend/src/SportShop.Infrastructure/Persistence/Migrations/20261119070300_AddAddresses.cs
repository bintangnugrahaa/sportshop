using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace SportShop.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAddresses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "addresses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                recipient_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                province_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_addresses", x => x.id);
                table.ForeignKey(
                    name: "fk_addresses_shipping_rates_province_code",
                    column: x => x.province_code,
                    principalTable: "shipping_rates",
                    principalColumn: "province_code",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_addresses_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_addresses_province_code",
            table: "addresses",
            column: "province_code");

        migrationBuilder.CreateIndex(
            name: "ix_addresses_user_id_is_default",
            table: "addresses",
            column: "user_id",
            unique: true,
            filter: "is_default");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "addresses");
    }
}
