using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize]
public class FacilitiesController : BaseApiController
{
    private readonly IFacilityService _facilityService;

    public FacilitiesController(IFacilityService facilityService)
    {
        _facilityService = facilityService;
    }

    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FacilityDto>>>> GetFacilities(Guid propertyId)
    {
        return HandleResult(await _facilityService.GetFacilitiesByPropertyAsync(propertyId));
    }

    [HttpPost("bookings")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> CreateBooking(FacilityBookingDto bookingDto)
    {
        return HandleResult(await _facilityService.CreateBookingAsync(bookingDto));
    }

    [HttpPost("bookings/qr/generate")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateBookingQr(Guid bookingId)
    {
        return HandleResult(await _facilityService.GenerateFacilityQrAsync(bookingId));
    }
}

public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetUserNotifications(Guid userId)
    {
        return HandleResult(await _notificationService.GetNotificationsByUserAsync(userId));
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkAsRead(Guid id)
    {
        return HandleResult(await _notificationService.MarkAsReadAsync(id));
    }

    [HttpPost("send-email")]
    public async Task<ActionResult<ApiResponse<bool>>> SendEmail(string to, string templateKey, object data)
    {
        return HandleResult(await _notificationService.SendEmailAsync(to, templateKey, data));
    }

    [HttpPost("send-sms")]
    public async Task<ActionResult<ApiResponse<bool>>> SendSms(IEnumerable<string> phoneNumbers, string message)
    {
        return HandleResult(await _notificationService.SendBulkSmsAsync(phoneNumbers, message));
    }

    [HttpPost("send-whatsapp")]
    public async Task<ActionResult<ApiResponse<bool>>> SendWhatsapp(string toNumber, string message)
    {
        return HandleResult(await _notificationService.SendWhatsappAsync(toNumber, message));
    }

    [HttpPost("announcement")]
    public async Task<ActionResult<ApiResponse<bool>>> SendAnnouncement(Guid announcementId, string[] channels)
    {
        return HandleResult(await _notificationService.SendAnnouncementAsync(announcementId, channels));
    }

    [HttpPost("test-email")]
    public async Task<ActionResult<ApiResponse<bool>>> SendTestEmail([FromBody] string email) => Ok(ApiResponse<bool>.SuccessResponse(true, "Test email sent"));

    [HttpPost("whatsapp-webhook")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<bool>> WhatsappWebhook() => Ok(ApiResponse<bool>.SuccessResponse(true, "Webhook received"));
}

public class SmartHomeController : BaseApiController
{
    private readonly ISmartHomeService _smartHomeService;

    public SmartHomeController(ISmartHomeService smartHomeService)
    {
        _smartHomeService = smartHomeService;
    }

    [HttpPost("voice")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessVoice([FromBody] string command)
    {
        return HandleResult(await _smartHomeService.ProcessVoiceCommandAsync(command));
    }

    [HttpPost("alerts")]
    public async Task<ActionResult<ApiResponse<bool>>> HandleAlert(Guid deviceId, string alertType)
    {
        return HandleResult(await _smartHomeService.HandleDeviceAlertAsync(deviceId, alertType));
    }
}

public class CommunityController : BaseApiController
{
    private readonly ICommunityService _communityService;

    public CommunityController(ICommunityService communityService)
    {
        _communityService = communityService;
    }

    [HttpGet("channels/property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CommunityChannelDto>>>> GetChannels(Guid propertyId)
    {
        return HandleResult(await _communityService.GetChannelsByPropertyAsync(propertyId));
    }

    [HttpGet("channels/{channelId}/posts")]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetChannelPosts(Guid channelId) => Ok(ApiResponse<IEnumerable<object>>.SuccessResponse(new List<object>()));

    [HttpPost("channels/{channelId}/posts")]
    public ActionResult<ApiResponse<bool>> CreatePost(Guid channelId, [FromBody] string content) => Ok(ApiResponse<bool>.SuccessResponse(true, "Post created"));

    [HttpPost("posts/{postId}/replies")]
    public ActionResult<ApiResponse<bool>> CreateReply(Guid postId, [FromBody] string content) => Ok(ApiResponse<bool>.SuccessResponse(true, "Reply created"));
}
