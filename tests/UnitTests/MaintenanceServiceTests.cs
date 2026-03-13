using Moq;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Application.Services;
using AutoMapper;
using Rentolic.Domain.Enums;
using System.Linq.Expressions;

namespace Rentolic.UnitTests;

public class MaintenanceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly MaintenanceService _maintenanceService;

    public MaintenanceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _maintenanceService = new MaintenanceService(_unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task ApplyWorkOrderBusinessRules_ShouldSetSlaAndApprovalStatus()
    {
        // Arrange
        var issueId = Guid.NewGuid();
        var issue = new IssueReport
        {
            Id = issueId,
            Priority = Priority.HIGH,
            CostEstimate = 1500,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWorkMock.Setup(u => u.Repository<IssueReport>().GetByIdAsync(issueId)).ReturnsAsync(issue);
        _unitOfWorkMock.Setup(u => u.SaveAsync()).ReturnsAsync(1);

        // Act
        var result = await _maintenanceService.ApplyWorkOrderBusinessRulesAsync(issueId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("PENDING", issue.ApprovalStatus);
        Assert.NotNull(issue.SlaDueDate);
        Assert.True(issue.SlaDueDate > issue.CreatedAt);
    }
}
