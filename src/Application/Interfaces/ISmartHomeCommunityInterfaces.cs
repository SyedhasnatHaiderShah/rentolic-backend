using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface ISmartHomeService
{
    Task<ApiResponse<bool>> ProcessVoiceCommandAsync(string command);
    Task<ApiResponse<bool>> HandleDeviceAlertAsync(Guid deviceId, string alertType);
}

public interface ICommunityService
{
    Task<ApiResponse<IEnumerable<CommunityChannelDto>>> GetChannelsByPropertyAsync(Guid propertyId);
}

public class CommunityChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
}
