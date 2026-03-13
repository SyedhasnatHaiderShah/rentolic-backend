using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Domain.Enums;

namespace Rentolic.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserDto>> CreateSubUserAsync(SubUserCreateRequest request, string parentRole)
    {
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = UserStatus.ACTIVE
        };

        await _unitOfWork.Repository<User>().AddAsync(user);

        // Logic to assign sub-user record based on parentRole
        if (parentRole == "LANDLORD")
        {
            await _unitOfWork.Repository<LandlordSubUser>().AddAsync(new LandlordSubUser
            {
                LandlordId = request.ParentId ?? Guid.Empty,
                SubUserId = user.Id,
                AccessLevel = request.Role,
                Permissions = request.Permissions
            });
        }
        // ... similar logic for other roles

        await _unitOfWork.SaveAsync();
        return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), "Sub-user created successfully");
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null) return ApiResponse<bool>.FailureResponse(new List<string> { "User not found" });

        user.Status = UserStatus.DELETED;
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully");
    }
}

public class PaymentService : IPaymentService
{
    public async Task<ApiResponse<PaymentIntentResponse>> CreatePaymentIntentAsync(PaymentIntentRequest request)
    {
        // Placeholder for Stripe integration
        return ApiResponse<PaymentIntentResponse>.SuccessResponse(new PaymentIntentResponse
        {
            ClientSecret = "pi_placeholder_secret",
            PaymentIntentId = "pi_placeholder_id"
        });
    }

    public async Task<ApiResponse<bool>> VerifyPaymentAsync(Guid paymentId, bool verified, string? notes)
    {
        // Logic to verify manual payments
        return ApiResponse<bool>.SuccessResponse(true, "Payment verification processed");
    }
}
