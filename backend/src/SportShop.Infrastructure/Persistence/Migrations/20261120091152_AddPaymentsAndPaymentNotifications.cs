using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace SportShop.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPaymentsAndPaymentNotifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Midtrans"),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                snap_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                snap_redirect_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                payment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                fraud_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                transaction_time = table.Column<DateTime>(type: "timestamptz", nullable: true),
                settled_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                last_notification_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                raw_last_payload = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_payments", x => x.id);
                table.CheckConstraint("ck_payments_gross_amount_positive", "gross_amount > 0");
                table.CheckConstraint("ck_payments_status", "status IN ('Pending', 'Settlement', 'Capture', 'Deny', 'Cancel', 'Expire', 'Failure', 'Refund')");
                table.ForeignKey(
                    name: "fk_payments_orders_order_id",
                    column: x => x.order_id,
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "payment_notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                transaction_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                status_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                signature_valid = table.Column<bool>(type: "boolean", nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                received_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                dedup_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_payment_notifications", x => x.id);
                table.ForeignKey(
                    name: "fk_payment_notifications_payments_payment_id",
                    column: x => x.payment_id,
                    principalTable: "payments",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_payment_notifications_dedup_key",
            table: "payment_notifications",
            column: "dedup_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_payment_notifications_order_number",
            table: "payment_notifications",
            column: "order_number");

        migrationBuilder.CreateIndex(
            name: "ix_payment_notifications_payment_id",
            table: "payment_notifications",
            column: "payment_id");

        migrationBuilder.CreateIndex(
            name: "ix_payments_order_active",
            table: "payments",
            column: "order_id",
            unique: true,
            filter: "status IN ('Pending', 'Settlement', 'Capture')");

        migrationBuilder.CreateIndex(
            name: "ix_payments_status_created_at",
            table: "payments",
            columns: new[] { "status", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ix_payments_transaction_id",
            table: "payments",
            column: "transaction_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "payment_notifications");

        migrationBuilder.DropTable(
            name: "payments");
    }
}
