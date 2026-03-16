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
    Task<ApiResponse<PaymentIntentResponse>> CreateLeasePaymentCheckoutAsync(Guid paymentId);
    Task<ApiResponse<bool>> ProcessStripeWebhookAsync(string payload, string signature);
    Task<ApiResponse<bool>> GeneratePaymentScheduleAsync(Guid leaseId);
    Task<ApiResponse<bool>> SendLeasePaymentRemindersAsync();
    Task<ApiResponse<int>> AutoGenerateMonthlyInvoicesAsync();
    Task<ApiResponse<bool>> CalculateLateFeesAsync();
    Task<ApiResponse<bool>> AutoProcessPaymentsAsync();
    Task<ApiResponse<bool>> CalculateCommissionsAsync();
}

public interface IMaintenanceService
{
    Task<ApiResponse<IEnumerable<IssueReportDto>>> GetIssuesByPropertyAsync(Guid propertyId);
    Task<ApiResponse<IssueReportDto>> CreateIssueReportAsync(IssueReportDto issueReportDto);
    Task<ApiResponse<bool>> ScheduleWorkAsync(Guid issueId, DateTime scheduledDate);
    Task<ApiResponse<PaymentIntentResponse>> CreateWorkOrderPaymentAsync(Guid issueId);
    Task<ApiResponse<decimal>> CalculateAssignmentScoreAsync(Guid issueId, Guid teamId);
    Task<ApiResponse<bool>> ApplyWorkOrderBusinessRulesAsync(Guid issueId);
}
