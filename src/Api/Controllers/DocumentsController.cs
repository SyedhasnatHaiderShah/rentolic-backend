using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize]
public class DocumentsController : BaseApiController
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService) => _documentService = documentService;

    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DocumentDto>>>> GetPropertyDocuments(Guid propertyId) => HandleResult(await _documentService.GetDocumentsByPropertyAsync(propertyId));

    [HttpGet("shared")]
    [Authorize(Roles = "TENANT")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DocumentDto>>>> GetSharedDocuments()
    {
        // For now, returning empty list as placeholder
        return Ok(ApiResponse<IEnumerable<DocumentDto>>.SuccessResponse(new List<DocumentDto>()));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> UploadDocument(IFormFile file, [FromForm] string title, [FromForm] Guid? propertyId)
    {
        // Mock upload logic
        var doc = new DocumentDto { Id = Guid.NewGuid(), Title = title, FilePath = $"uploads/{file.FileName}" };
        return Ok(ApiResponse<DocumentDto>.SuccessResponse(doc, "File uploaded successfully"));
    }
}
