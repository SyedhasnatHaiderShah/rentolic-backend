using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;

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
