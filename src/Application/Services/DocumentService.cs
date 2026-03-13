using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;

namespace Rentolic.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DocumentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<DocumentDto>>> GetDocumentsByPropertyAsync(Guid propertyId)
    {
        var documents = await _unitOfWork.Repository<Document>().FindAsync(d => d.PropertyId == propertyId);
        return ApiResponse<IEnumerable<DocumentDto>>.SuccessResponse(_mapper.Map<IEnumerable<DocumentDto>>(documents));
    }
}
