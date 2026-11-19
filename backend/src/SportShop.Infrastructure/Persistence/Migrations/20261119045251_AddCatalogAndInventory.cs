using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable
#pragma warning disable CA1861

namespace SportShop.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCatalogAndInventory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
            .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

        migrationBuilder.CreateTable(
            name: "brands",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_brands", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_categories_categories_parent_id",
                    column: x => x.parent_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "products",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                base_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', coalesce(name, '') || ' ' || coalesce(description, ''))", stored: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_products", x => x.id);
                table.CheckConstraint("ck_products_base_price", "base_price >= 0");
                table.ForeignKey(
                    name: "fk_products_brands_brand_id",
                    column: x => x.brand_id,
                    principalTable: "brands",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_products_categories_category_id",
                    column: x => x.category_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "product_images",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_product_images", x => x.id);
                table.ForeignKey(
                    name: "fk_product_images_products_product_id",
                    column: x => x.product_id,
                    principalTable: "products",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "product_variants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                option_size = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                option_color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                weight_gram = table.Column<int>(type: "integer", nullable: false),
                stock_quantity = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_product_variants", x => x.id);
                table.CheckConstraint("ck_product_variants_price", "price >= 0");
                table.CheckConstraint("ck_product_variants_stock_quantity", "stock_quantity >= 0");
                table.CheckConstraint("ck_product_variants_weight_gram", "weight_gram > 0");
                table.ForeignKey(
                    name: "fk_product_variants_products_product_id",
                    column: x => x.product_id,
                    principalTable: "products",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_movements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                change_quantity = table.Column<int>(type: "integer", nullable: false),
                reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: true),
                note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inventory_movements", x => x.id);
                table.CheckConstraint("ck_inventory_movements_reason", "reason IN ('Initial', 'Sale', 'Cancel', 'Expire', 'ManualAdjust')");
                table.ForeignKey(
                    name: "fk_inventory_movements_product_variants_variant_id",
                    column: x => x.variant_id,
                    principalTable: "product_variants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_inventory_movements_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_brands_name",
            table: "brands",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_brands_slug",
            table: "brands",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_categories_parent_id",
            table: "categories",
            column: "parent_id");

        migrationBuilder.CreateIndex(
            name: "ix_categories_slug",
            table: "categories",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_inventory_movements_created_by",
            table: "inventory_movements",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_inventory_movements_order_id",
            table: "inventory_movements",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "ix_inventory_movements_order_variant_cancel_expire",
            table: "inventory_movements",
            columns: new[] { "order_id", "variant_id", "reason" },
            unique: true,
            filter: "reason IN ('Cancel', 'Expire')");

        migrationBuilder.CreateIndex(
            name: "ix_inventory_movements_variant_id_created_at",
            table: "inventory_movements",
            columns: new[] { "variant_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ix_product_images_product_id_primary",
            table: "product_images",
            column: "product_id",
            unique: true,
            filter: "is_primary = true");

        migrationBuilder.CreateIndex(
            name: "ix_product_variants_low_stock",
            table: "product_variants",
            column: "stock_quantity",
            filter: "stock_quantity <= 5");

        migrationBuilder.CreateIndex(
            name: "ix_product_variants_product_id",
            table: "product_variants",
            column: "product_id");

        migrationBuilder.CreateIndex(
            name: "ix_product_variants_product_id_option_size_option_color",
            table: "product_variants",
            columns: new[] { "product_id", "option_size", "option_color" },
            unique: true)
            .Annotation("Npgsql:NullsDistinct", false);

        migrationBuilder.CreateIndex(
            name: "ix_product_variants_sku",
            table: "product_variants",
            column: "sku",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_products_brand_id",
            table: "products",
            column: "brand_id");

        migrationBuilder.CreateIndex(
            name: "ix_products_category_id",
            table: "products",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "ix_products_is_active_base_price",
            table: "products",
            columns: new[] { "is_active", "base_price" });

        migrationBuilder.CreateIndex(
            name: "ix_products_name",
            table: "products",
            column: "name")
            .Annotation("Npgsql:IndexMethod", "GIN")
            .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

        migrationBuilder.CreateIndex(
            name: "ix_products_search_vector",
            table: "products",
            column: "search_vector")
            .Annotation("Npgsql:IndexMethod", "GIN");

        migrationBuilder.CreateIndex(
            name: "ix_products_slug",
            table: "products",
            column: "slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inventory_movements");

        migrationBuilder.DropTable(
            name: "product_images");

        migrationBuilder.DropTable(
            name: "product_variants");

        migrationBuilder.DropTable(
            name: "products");

        migrationBuilder.DropTable(
            name: "brands");

        migrationBuilder.DropTable(
            name: "categories");

        migrationBuilder.AlterDatabase()
            .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
            .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
    }
}
