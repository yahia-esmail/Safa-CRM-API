using Application.Features.Dashboard;

namespace Application.Common.Interfaces;

public record CompanyExportRow(
    string EnglishName,
    string ArabicName,
    string Country,
    string Phone,
    string Email,
    string Stage,
    string AccountType,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime? LastActivity);

public record OrderExportRow(
    string InvoiceNumber,
    string Company,
    string OrderType,
    string Status,
    string Currency,
    decimal OriginalAmount,
    decimal UsdAmount,
    string CreatedBy,
    DateTime CreatedAt);

public interface IExportService
{
    byte[] ExportCompanies(IEnumerable<CompanyExportRow> companies);
    byte[] ExportOrders(IEnumerable<OrderExportRow> orders);
    byte[] GenerateDashboardPdf(AdminDashboardDto adminData, DateTime from, DateTime to);
    byte[] GenerateDashboardPdf(SalesDashboardDto salesData, string repName, DateTime from, DateTime to);
}
