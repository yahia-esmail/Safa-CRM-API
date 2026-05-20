using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaaSArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultPlanId = new Guid("11111111-1111-1111-1111-111111111111");
            var defaultTenantId = new Guid("22222222-2222-2222-2222-222222222222");

            // 1. Create SubscriptionPlans
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxAdmins = table.Column<int>(type: "int", nullable: false),
                    MaxSales = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            // 2. Create Tenants
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubscriptionEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 3. Insert Default Data to prevent FK conflicts with existing records
            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Name", "MaxAdmins", "MaxSales", "Price", "CreatedAt" },
                values: new object[] { defaultPlanId, "Legacy Plan", 999, 999, 0m, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Name", "Industry", "SubscriptionPlanId", "SubscriptionStart", "SubscriptionEnd", "IsActive", "CreatedAt" },
                values: new object[] { defaultTenantId, "System Tenant", "Technology", defaultPlanId, DateTime.UtcNow, DateTime.UtcNow.AddYears(100), true, DateTime.UtcNow });

            // 4. Add Columns to existing tables with the new DefaultTenantId
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ImportLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CompanyContacts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Activities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultTenantId);

            // 5. Create UserSmtpSettings
            migrationBuilder.CreateTable(
                name: "UserSmtpSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Host = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Encryption = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSmtpSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSmtpSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 6. Create Indexes
            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantId",
                table: "SalesOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_TenantId",
                table: "ImportLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContacts_TenantId",
                table: "CompanyContacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId",
                table: "Companies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId",
                table: "Activities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SubscriptionPlanId",
                table: "Tenants",
                column: "SubscriptionPlanId");

            // 7. Add Foreign Keys
            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Tenants_TenantId",
                table: "Activities",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyContacts_Tenants_TenantId",
                table: "CompanyContacts",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportLogs_Tenants_TenantId",
                table: "ImportLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Tenants_TenantId",
                table: "SalesOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Tenants_TenantId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyContacts_Tenants_TenantId",
                table: "CompanyContacts");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportLogs_Tenants_TenantId",
                table: "ImportLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Tenants_TenantId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "UserSmtpSettings");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TenantId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_ImportLogs_TenantId",
                table: "ImportLogs");

            migrationBuilder.DropIndex(
                name: "IX_CompanyContacts_TenantId",
                table: "CompanyContacts");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Activities_TenantId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CompanyContacts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Activities");
        }
    }
}
