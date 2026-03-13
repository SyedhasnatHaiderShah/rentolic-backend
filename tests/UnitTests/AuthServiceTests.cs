using Moq;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Infrastructure.Services;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Rentolic.Domain.Enums;
using System.Linq.Expressions;

namespace Rentolic.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationMock = new Mock<IConfiguration>();
        _authService = new AuthService(_unitOfWorkMock.Object, _mapperMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new RegisterRequest { Email = "test@example.com", Password = "Password123", Name = "Test User" };
        _unitOfWorkMock.Setup(u => u.Repository<User>().FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());
        _unitOfWorkMock.Setup(u => u.SaveAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User registered successfully", result.Message);
        _unitOfWorkMock.Verify(u => u.Repository<User>().AddAsync(It.IsAny<User>()), Times.Once);
    }
}
