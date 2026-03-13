using Moq;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Application.Services;

namespace Rentolic.UnitTests;

public class SystemTaskServiceTests
{
    private readonly SystemTaskService _systemTaskService;

    public SystemTaskServiceTests()
    {
        _systemTaskService = new SystemTaskService();
    }

    [Fact]
    public async Task ExtractMrzData_ShouldReturnMockedData()
    {
        // Arrange
        var request = new MrzExtractionRequest { ImageBase64 = "base64", DocumentType = "PASSPORT" };

        // Act
        var result = await _systemTaskService.ExtractMrzDataAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
