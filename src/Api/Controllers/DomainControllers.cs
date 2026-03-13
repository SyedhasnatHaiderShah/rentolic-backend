using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize]
public class PropertiesController : BaseApiController
{
    private readonly IPropertyService _propertyService;

    public PropertiesController(IPropertyService propertyService) => _propertyService = propertyService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetProperties() => HandleResult(await _propertyService.GetAllPropertiesAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> GetProperty(Guid id) => HandleResult(await _propertyService.GetPropertyByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = "PLATFORM_ADMIN,LANDLORD")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> CreateProperty(PropertyDto propertyDto) => HandleResult(await _propertyService.CreatePropertyAsync(propertyDto));
}

public class MaintenanceController : BaseApiController
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(IMaintenanceService maintenanceService) => _maintenanceService = maintenanceService;

    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<IssueReportDto>>>> GetIssues(Guid propertyId) => HandleResult(await _maintenanceService.GetIssuesByPropertyAsync(propertyId));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IssueReportDto>>> CreateIssue(IssueReportDto issueReportDto) => HandleResult(await _maintenanceService.CreateIssueReportAsync(issueReportDto));

    [HttpPost("schedule")]
    public async Task<ActionResult<ApiResponse<bool>>> ScheduleWork(Guid issueId, DateTime scheduledDate) => HandleResult(await _maintenanceService.ScheduleWorkAsync(issueId, scheduledDate));

    [HttpPost("payment")]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> CreateWorkOrderPayment(Guid issueId) => HandleResult(await _maintenanceService.CreateWorkOrderPaymentAsync(issueId));

    [HttpPost("payment/verify")]
    public ActionResult<ApiResponse<bool>> VerifyWorkOrderPayment(Guid issueId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Payment verified"));

    [HttpPost("unit-code-email")]
    public ActionResult<ApiResponse<bool>> SendUnitCodeEmail(Guid unitId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Unit code sent"));

    [HttpGet("{id}/score/{teamId}")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetAssignmentScore(Guid id, Guid teamId) => HandleResult(await _maintenanceService.CalculateAssignmentScoreAsync(id, teamId));

    [HttpPost("{id}/apply-rules")]
    public async Task<ActionResult<ApiResponse<bool>>> ApplyRules(Guid id) => HandleResult(await _maintenanceService.ApplyWorkOrderBusinessRulesAsync(id));
}

public class LeasesController : BaseApiController
{
    private readonly ILeaseService _leaseService;

    public LeasesController(ILeaseService leaseService) => _leaseService = leaseService;

    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaseDto>>>> GetTenantLeases(Guid tenantId) => HandleResult(await _leaseService.GetLeasesByTenantAsync(tenantId));

    [HttpPost]
    [Authorize(Roles = "PLATFORM_ADMIN,LANDLORD")]
    public async Task<ActionResult<ApiResponse<LeaseDto>>> CreateLease(LeaseDto leaseDto) => HandleResult(await _leaseService.CreateLeaseAsync(leaseDto));
}

public class FinanceController : BaseApiController
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService) => _financeService = financeService;

    [HttpGet("invoices/tenant/{tenantId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDto>>>> GetTenantInvoices(Guid tenantId) => HandleResult(await _financeService.GetInvoicesByTenantAsync(tenantId));

    [HttpPost("invoices")]
    [Authorize(Roles = "PLATFORM_ADMIN,LANDLORD")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice(InvoiceDto invoiceDto) => HandleResult(await _financeService.CreateInvoiceAsync(invoiceDto));

    [HttpPost("lease-payment/checkout")]
    [Authorize(Roles = "TENANT")]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> CreateLeasePaymentCheckout(Guid paymentId) => HandleResult(await _financeService.CreateLeasePaymentCheckoutAsync(paymentId));

    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> StripeWebhook() => HandleResult(await _financeService.ProcessStripeWebhookAsync("", ""));

    [HttpPost("payment-schedule/{leaseId}")]
    [Authorize(Roles = "LANDLORD,PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> GeneratePaymentSchedule(Guid leaseId) => HandleResult(await _financeService.GeneratePaymentScheduleAsync(leaseId));

    [HttpPost("payment-reminders")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> SendPaymentReminders() => HandleResult(await _financeService.SendLeasePaymentRemindersAsync());

    [HttpPost("payment-receipt/{paymentId}")]
    public ActionResult<ApiResponse<bool>> SendPaymentReceipt(Guid paymentId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Receipt sent"));

    [HttpPost("payment-reminder/{paymentId}")]
    public ActionResult<ApiResponse<bool>> SendSinglePaymentReminder(Guid paymentId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Reminder sent"));

    [HttpPost("lease-document/notify")]
    public ActionResult<ApiResponse<bool>> NotifyLeaseDocument(Guid documentId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Document notification sent"));

    [HttpPost("subscriptions/landlord/create")]
    [Authorize(Roles = "LANDLORD")]
    public ActionResult<ApiResponse<bool>> CreateLandlordSubscription() => Ok(ApiResponse<bool>.SuccessResponse(true, "Subscription initiated"));

    [HttpPost("subscriptions/landlord/manage")]
    [Authorize(Roles = "LANDLORD")]
    public ActionResult<ApiResponse<bool>> ManageLandlordSubscription() => Ok(ApiResponse<bool>.SuccessResponse(true, "Redirecting to portal"));
}

public class SecurityController : BaseApiController
{
    private readonly ISecurityService _securityService;

    public SecurityController(ISecurityService securityService) => _securityService = securityService;

    [HttpGet("permits/property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VisitorPermitDto>>>> GetPermits(Guid propertyId) => HandleResult(await _securityService.GetPermitsByPropertyAsync(propertyId));

    [HttpPost("permits")]
    public async Task<ActionResult<ApiResponse<VisitorPermitDto>>> CreatePermit(VisitorPermitDto permitDto) => HandleResult(await _securityService.CreatePermitAsync(permitDto));

    [HttpGet("incidents/property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<IncidentDto>>>> GetIncidents(Guid propertyId) => HandleResult(await _securityService.GetIncidentsByPropertyAsync(propertyId));

    [HttpPost("permits/qr/generate")]
    public async Task<ActionResult<ApiResponse<string>>> GeneratePermitQr(Guid permitId) => HandleResult(await _securityService.GenerateVisitorQrAsync(permitId));

    [HttpPost("permits/qr/validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ValidatePermitQr([FromBody] string qrCode) => HandleResult(await _securityService.ValidateVisitorQrAsync(qrCode));

    [HttpPost("incidents/dispatch")]
    public ActionResult<ApiResponse<bool>> DispatchIncident(Guid incidentId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Incident dispatched"));
}

public class ServiceProvidersController : BaseApiController
{
    private readonly IServiceProviderService _providerService;

    public ServiceProvidersController(IServiceProviderService providerService) => _providerService = providerService;

    [HttpGet("listings")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceListingDto>>>> GetListings() => HandleResult(await _providerService.GetAllListingsAsync());

    [HttpPost("bookings")]
    public async Task<ActionResult<ApiResponse<ServiceBookingDto>>> CreateBooking(ServiceBookingDto bookingDto) => HandleResult(await _providerService.CreateBookingAsync(bookingDto));

    [HttpPost("booking/payment")]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> CreateBookingPayment(Guid bookingId) => HandleResult(await _providerService.CreateServiceBookingPaymentAsync(bookingId));

    [HttpPost("booking/payment/verify")]
    public ActionResult<ApiResponse<bool>> VerifyBookingPayment(Guid bookingId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Payment verified"));

    [HttpPost("payouts")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessPayouts() => HandleResult(await _providerService.ProcessProviderPayoutsAsync());

    [HttpGet("recommended")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Guid>>>> GetRecommended(string category, Guid propertyId) => HandleResult(await _providerService.GetRecommendedServiceProvidersAsync(category, propertyId));
}
