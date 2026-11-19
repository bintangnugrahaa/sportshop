using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861

namespace SportShop.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddShippingRates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "shipping_rates",
            columns: table => new
            {
                province_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                province_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                base_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                per_kg_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                estimated_days_min = table.Column<short>(type: "smallint", nullable: false),
                estimated_days_max = table.Column<short>(type: "smallint", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_shipping_rates", x => x.province_code);
                table.CheckConstraint("ck_shipping_rates_base_fee", "base_fee >= 0");
                table.CheckConstraint("ck_shipping_rates_estimated_days_max", "estimated_days_max >= estimated_days_min");
                table.CheckConstraint("ck_shipping_rates_estimated_days_min", "estimated_days_min >= 0");
                table.CheckConstraint("ck_shipping_rates_per_kg_fee", "per_kg_fee >= 0");
            });

        migrationBuilder.InsertData(
            table: "shipping_rates",
            columns: new[] { "province_code", "base_fee", "estimated_days_max", "estimated_days_min", "is_active", "per_kg_fee", "province_name", "updated_at" },
            values: new object[,]
            {
                { "AC", 28000m, (short)6, (short)3, true, 17000m, "Aceh", null },
                { "BA", 20000m, (short)5, (short)3, true, 12000m, "Bali", null },
                { "BB", 24000m, (short)5, (short)3, true, 15000m, "Kepulauan Bangka Belitung", null },
                { "BE", 24000m, (short)5, (short)3, true, 14000m, "Bengkulu", null },
                { "BT", 11000m, (short)3, (short)1, true, 6000m, "Banten", null },
                { "GO", 34000m, (short)7, (short)4, true, 22000m, "Gorontalo", null },
                { "JA", 23000m, (short)5, (short)3, true, 14000m, "Jambi", null },
                { "JB", 11000m, (short)3, (short)1, true, 6000m, "Jawa Barat", null },
                { "JI", 16000m, (short)4, (short)2, true, 9000m, "Jawa Timur", null },
                { "JK", 10000m, (short)2, (short)1, true, 5000m, "DKI Jakarta", null },
                { "JT", 15000m, (short)4, (short)2, true, 8000m, "Jawa Tengah", null },
                { "KB", 26000m, (short)6, (short)3, true, 16000m, "Kalimantan Barat", null },
                { "KI", 29000m, (short)6, (short)3, true, 18000m, "Kalimantan Timur", null },
                { "KR", 26000m, (short)6, (short)3, true, 16000m, "Kepulauan Riau", null },
                { "KS", 27000m, (short)6, (short)3, true, 16000m, "Kalimantan Selatan", null },
                { "KT", 28000m, (short)6, (short)3, true, 17000m, "Kalimantan Tengah", null },
                { "KU", 32000m, (short)7, (short)4, true, 20000m, "Kalimantan Utara", null },
                { "LA", 18000m, (short)4, (short)2, true, 10000m, "Lampung", null },
                { "MA", 40000m, (short)8, (short)5, true, 28000m, "Maluku", null },
                { "MU", 42000m, (short)8, (short)5, true, 30000m, "Maluku Utara", null },
                { "NB", 25000m, (short)6, (short)3, true, 15000m, "Nusa Tenggara Barat", null },
                { "NT", 30000m, (short)7, (short)4, true, 18000m, "Nusa Tenggara Timur", null },
                { "PA", 50000m, (short)9, (short)5, true, 35000m, "Papua", null },
                { "PB", 50000m, (short)9, (short)5, true, 35000m, "Papua Barat", null },
                { "PD", 52000m, (short)9, (short)5, true, 36000m, "Papua Barat Daya", null },
                { "PE", 60000m, (short)12, (short)7, true, 42000m, "Papua Pegunungan", null },
                { "PS", 55000m, (short)10, (short)6, true, 38000m, "Papua Selatan", null },
                { "PT", 55000m, (short)10, (short)6, true, 38000m, "Papua Tengah", null },
                { "RI", 24000m, (short)5, (short)3, true, 14000m, "Riau", null },
                { "SA", 32000m, (short)7, (short)4, true, 20000m, "Sulawesi Utara", null },
                { "SB", 25000m, (short)5, (short)3, true, 15000m, "Sumatera Barat", null },
                { "SG", 32000m, (short)7, (short)4, true, 20000m, "Sulawesi Tenggara", null },
                { "SN", 28000m, (short)6, (short)3, true, 17000m, "Sulawesi Selatan", null },
                { "SR", 33000m, (short)7, (short)4, true, 21000m, "Sulawesi Barat", null },
                { "SS", 22000m, (short)4, (short)2, true, 13000m, "Sumatera Selatan", null },
                { "ST", 33000m, (short)7, (short)4, true, 21000m, "Sulawesi Tengah", null },
                { "SU", 25000m, (short)5, (short)3, true, 15000m, "Sumatera Utara", null },
                { "YO", 15000m, (short)4, (short)2, true, 8000m, "DI Yogyakarta", null }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "shipping_rates");
    }
}
