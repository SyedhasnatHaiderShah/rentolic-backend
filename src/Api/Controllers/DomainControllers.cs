using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize]
public class PropertiesController : BaseApiController
{
    private readonly IPropertyService _propertyService;

    public PropertiesController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetProperties()
    {
        return HandleResult(await _propertyService.GetAllPropertiesAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> GetProperty(Guid id)
    {
        return HandleResult(await _propertyService.GetPropertyByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "PLATFORM_ADMIN,LANDLORD")]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> CreateProperty(PropertyDto propertyDto)
    {
        return HandleResult(await _propertyService.CreatePropertyAsync(propertyDto));
    }
}

public class MaintenanceController : BaseApiController
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<IssueReportDto>>>> GetIssues(Guid propertyId)
    {
        return HandleResult(await _maintenanceService.GetIssuesByPropertyAsync(propertyId));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IssueReportDto>>> CreateIssue(IssueReportDto issueReportDto)
    {
        return HandleResult(await _maintenanceService.CreateIssueReportAsync(issueReportDto));
    }
}

public class LeasesController : BaseApiController
{
    private readonly ILeaseService _leaseService;

    public LeasesController(ILeaseService leaseService)
    {
        _leaseService = leaseService;
    }

    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaseDto>>>> GetTenantLeases(Guid tenantId)
    {
        return HandleResult(await _leaseService.GetLeasesByTenantAsync(tenantId));
    }

    [HttpPost]
    [Authorize(Roles = "PLATFORM_ADMIN,LANDLORD")]
    public async Task<ActionResult<ApiResponse<LeaseDto>>> CreateLease(LeaseDto leaseDto)
    {
        return HandleResult(await _leaseService.CreateLeaseAsync(leaseDto));
    }
}

public class FinanceController : BaseApiController
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("invoices/tenant/{tenantId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDto>>>> GetTenantInvoices(Guid tenantId)
    {
        return HandleResult(await _financeService.GetInvoicesByTenantAsync(tenantId));
    }

    [HttpPost("invoices")]
    [Authorize(Roles = "PLATFORM_ADMIN,LANDLORD")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice(InvoiceDto invoiceDto)
    {
        return HandleResult(await _financeService.CreateInvoiceAsync(invoiceDto));
    }
}

public class SecurityController : BaseApiController
{
    private readonly ISecurityService _securityService;

    public SecurityController(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    [HttpGet("permits/property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VisitorPermitDto>>>> GetPermits(Guid propertyId)
    {
        return HandleResult(await _securityService.GetPermitsByPropertyAsync(propertyId));
    }

    [HttpPost("permits")]
    public async Task<ActionResult<ApiResponse<VisitorPermitDto>>> CreatePermit(VisitorPermitDto permitDto)
    {
        return HandleResult(await _securityService.CreatePermitAsync(permitDto));
    }

    [HttpGet("incidents/property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<IncidentDto>>>> GetIncidents(Guid propertyId)
    {
        return HandleResult(await _securityService.GetIncidentsByPropertyAsync(propertyId));
    }
}

public class ServiceProvidersController : BaseApiController
{
    private readonly IServiceProviderService _providerService;

    public ServiceProvidersController(IServiceProviderService providerService)
    {
        _providerService = providerService;
    }

    [HttpGet("listings")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceListingDto>>>> GetListings()
    {
        return HandleResult(await _providerService.GetAllListingsAsync());
    }

    [HttpPost("bookings")]
    public async Task<ActionResult<ApiResponse<ServiceBookingDto>>> CreateBooking(ServiceBookingDto bookingDto)
    {
        return HandleResult(await _providerService.CreateBookingAsync(bookingDto));
    }
}
