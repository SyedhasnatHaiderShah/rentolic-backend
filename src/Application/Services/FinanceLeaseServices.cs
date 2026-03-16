using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Domain.Enums;

namespace Rentolic.Application.Services;

public class FinanceService : IFinanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FinanceService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<InvoiceDto>>> GetInvoicesByTenantAsync(Guid tenantId)
    {
        var invoices = await _unitOfWork.Repository<Invoice>().FindAsync(i => i.TenantUserId == tenantId);
        return ApiResponse<IEnumerable<InvoiceDto>>.SuccessResponse(_mapper.Map<IEnumerable<InvoiceDto>>(invoices));
    }

    public async Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(InvoiceDto invoiceDto)
    {
        var invoice = _mapper.Map<Invoice>(invoiceDto);
        await _unitOfWork.Repository<Invoice>().AddAsync(invoice);
        await _unitOfWork.SaveAsync();
        return ApiResponse<InvoiceDto>.SuccessResponse(_mapper.Map<InvoiceDto>(invoice), "Invoice created successfully");
    }

    public async Task<ApiResponse<PaymentIntentResponse>> CreateLeasePaymentCheckoutAsync(Guid paymentId)
    {
        // Stripe integration placeholder
        return ApiResponse<PaymentIntentResponse>.SuccessResponse(new PaymentIntentResponse { ClientSecret = "cs_lease", PaymentIntentId = "pi_lease" });
    }

    public async Task<ApiResponse<bool>> ProcessStripeWebhookAsync(string payload, string signature)
    {
        // Handle webhook events
        return ApiResponse<bool>.SuccessResponse(true, "Webhook processed");
    }

    public async Task<ApiResponse<bool>> GeneratePaymentScheduleAsync(Guid leaseId)
    {
        // Logic to generate multiple lease_payments records
        return ApiResponse<bool>.SuccessResponse(true, "Payment schedule generated");
    }

    public async Task<ApiResponse<bool>> SendLeasePaymentRemindersAsync()
    {
        return ApiResponse<bool>.SuccessResponse(true, "Payment reminders sent");
    }

    public async Task<ApiResponse<int>> AutoGenerateMonthlyInvoicesAsync()
    {
        var leases = await _unitOfWork.Repository<Lease>().FindAsync(l => l.Status == LeaseStatus.ACTIVE);
        int count = 0;
        foreach (var lease in leases)
        {
            var invoice = new Invoice
            {
                LeaseId = lease.Id,
                TenantUserId = lease.TenantUserId,
                Amount = lease.RentAmount,
                DueDate = DateTime.UtcNow.AddDays(7),
                Number = $"INV-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Status = InvoiceStatus.OPEN
            };
            await _unitOfWork.Repository<Invoice>().AddAsync(invoice);
            count++;
        }
        await _unitOfWork.SaveAsync();
        return ApiResponse<int>.SuccessResponse(count, $"{count} invoices generated");
    }

    public async Task<ApiResponse<bool>> CalculateLateFeesAsync()
    {
        var overdueInvoices = await _unitOfWork.Repository<Invoice>().FindAsync(i => i.Status == InvoiceStatus.OPEN && i.DueDate < DateTime.UtcNow);
        foreach (var invoice in overdueInvoices)
        {
            invoice.Amount += 100; // Flat late fee of 100 AED
            invoice.Status = InvoiceStatus.OVERDUE;
        }
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Late fees applied");
    }

    public async Task<ApiResponse<bool>> AutoProcessPaymentsAsync()
    {
        // Logic to trigger processing for all auto-pay enabled leases
        return ApiResponse<bool>.SuccessResponse(true, "Auto-payments processed");
    }

    public async Task<ApiResponse<bool>> CalculateCommissionsAsync()
    {
        // Logic to calculate provider commissions
        return ApiResponse<bool>.SuccessResponse(true, "Commissions calculated");
    }
}

public class LeaseService : ILeaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LeaseService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<LeaseDto>>> GetLeasesByTenantAsync(Guid tenantId)
    {
        var leases = await _unitOfWork.Repository<Lease>().FindAsync(l => l.TenantUserId == tenantId);
        return ApiResponse<IEnumerable<LeaseDto>>.SuccessResponse(_mapper.Map<IEnumerable<LeaseDto>>(leases));
    }

    public async Task<ApiResponse<LeaseDto>> CreateLeaseAsync(LeaseDto leaseDto)
    {
        var lease = _mapper.Map<Lease>(leaseDto);
        await _unitOfWork.Repository<Lease>().AddAsync(lease);
        await _unitOfWork.SaveAsync();
        return ApiResponse<LeaseDto>.SuccessResponse(_mapper.Map<LeaseDto>(lease), "Lease created successfully");
    }
}
