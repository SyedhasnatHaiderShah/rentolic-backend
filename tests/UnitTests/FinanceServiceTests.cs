using Moq;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Application.Services;
using AutoMapper;
using Rentolic.Domain.Enums;
using System.Linq.Expressions;

namespace Rentolic.UnitTests;

public class FinanceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly FinanceService _financeService;

    public FinanceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _financeService = new FinanceService(_unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task AutoGenerateMonthlyInvoices_ShouldGenerateInvoicesForActiveLeases()
    {
        // Arrange
        var leases = new List<Lease>
        {
            new Lease { Id = Guid.NewGuid(), Status = LeaseStatus.ACTIVE, RentAmount = 1000, TenantUserId = Guid.NewGuid() }
        };
        _unitOfWorkMock.Setup(u => u.Repository<Lease>().FindAsync(It.IsAny<Expression<Func<Lease, bool>>>())).ReturnsAsync(leases);
        _unitOfWorkMock.Setup(u => u.Repository<Invoice>().AddAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveAsync()).ReturnsAsync(1);

        // Act
        var result = await _financeService.AutoGenerateMonthlyInvoicesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Data);
        _unitOfWorkMock.Verify(u => u.Repository<Invoice>().AddAsync(It.IsAny<Invoice>()), Times.Once);
    }
}
