using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface IPropertyService
{
    Task<ApiResponse<IEnumerable<PropertyDto>>> GetAllPropertiesAsync();
    Task<ApiResponse<PropertyDto>> GetPropertyByIdAsync(Guid id);
    Task<ApiResponse<PropertyDto>> CreatePropertyAsync(PropertyDto propertyDto);
}

public interface ILeaseService
{
    Task<ApiResponse<IEnumerable<LeaseDto>>> GetLeasesByTenantAsync(Guid tenantId);
    Task<ApiResponse<LeaseDto>> CreateLeaseAsync(LeaseDto leaseDto);
}

public interface IFinanceService
{
    Task<ApiResponse<IEnumerable<InvoiceDto>>> GetInvoicesByTenantAsync(Guid tenantId);
    Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(InvoiceDto invoiceDto);
}

public interface IMaintenanceService
{
    Task<ApiResponse<IEnumerable<IssueReportDto>>> GetIssuesByPropertyAsync(Guid propertyId);
    Task<ApiResponse<IssueReportDto>> CreateIssueReportAsync(IssueReportDto issueReportDto);
}
