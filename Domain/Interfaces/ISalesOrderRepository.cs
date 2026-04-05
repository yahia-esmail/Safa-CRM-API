using Domain.Entities;

namespace Domain.Interfaces;

public interface ISalesOrderRepository : IGenericRepository<SalesOrder>
{
    Task<IEnumerable<SalesOrder>> GetByCompanyAsync(Guid companyId);
    Task<IEnumerable<SalesOrder>> GetByUserAsync(Guid userId);
    Task<string> GenerateInvoiceNumberAsync(int year);
}
