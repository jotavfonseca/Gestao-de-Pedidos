using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Renames the Portuguese columns to English and converts the stored status
    /// values (Pendente → Pending, Processando → Processing, Finalizado → Completed).
    /// </summary>
    public partial class RenameToEnglish : Migration
    {
        // Status values are stored as text: translate them along with the columns.
        private static readonly (string Portuguese, string English)[] Statuses =
        [
            ("Pendente", "Pending"),
            ("Processando", "Processing"),
            ("Finalizado", "Completed")
        ];

        private static void ConvertStatuses(MigrationBuilder migrationBuilder, bool toEnglish)
        {
            foreach (var (portuguese, english) in Statuses)
            {
                var (from, to) = toEnglish ? (portuguese, english) : (english, portuguese);
                migrationBuilder.Sql($"""UPDATE "Orders" SET "Status" = '{to}' WHERE "Status" = '{from}';""");
                migrationBuilder.Sql($"""UPDATE "OrderStatusHistory" SET "Status" = '{to}' WHERE "Status" = '{from}';""");
            }
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Data",
                table: "OrderStatusHistory",
                newName: "ChangedAt");

            migrationBuilder.RenameIndex(
                name: "IX_OrderStatusHistory_OrderId_Data",
                table: "OrderStatusHistory",
                newName: "IX_OrderStatusHistory_OrderId_ChangedAt");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "Orders",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Produto",
                table: "Orders",
                newName: "Product");

            migrationBuilder.RenameColumn(
                name: "DataCriacao",
                table: "Orders",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Cliente",
                table: "Orders",
                newName: "Customer");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DataCriacao",
                table: "Orders",
                newName: "IX_Orders_CreatedAt");

            ConvertStatuses(migrationBuilder, toEnglish: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ConvertStatuses(migrationBuilder, toEnglish: false);

            migrationBuilder.RenameColumn(
                name: "ChangedAt",
                table: "OrderStatusHistory",
                newName: "Data");

            migrationBuilder.RenameIndex(
                name: "IX_OrderStatusHistory_OrderId_ChangedAt",
                table: "OrderStatusHistory",
                newName: "IX_OrderStatusHistory_OrderId_Data");

            migrationBuilder.RenameColumn(
                name: "Product",
                table: "Orders",
                newName: "Produto");

            migrationBuilder.RenameColumn(
                name: "Customer",
                table: "Orders",
                newName: "Cliente");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Orders",
                newName: "DataCriacao");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Orders",
                newName: "Valor");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                newName: "IX_Orders_DataCriacao");
        }
    }
}
