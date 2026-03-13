using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Rentolic.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        // ⚡ Bolt: Use FirstOrDefaultAsync with predicate to avoid loading multiple users or list materialization
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<LoginResponse>.FailureResponse(new List<string> { "Invalid email or password" });
        }

        // ⚡ Bolt: Fix N+1 query by batch fetching roles based on IDs from UserRoles
        var userRoles = await _unitOfWork.Repository<UserRole>().FindAsync(ur => ur.UserId == user.Id);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _unitOfWork.Repository<Role>().FindAsync(r => roleIds.Contains(r.Id));
        var roleNames = roles.Select(r => r.Name).ToList();

        var token = GenerateJwtToken(user, roleNames);
        return ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse { Token = token, User = _mapper.Map<UserDto>(user) });
    }

    public async Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request)
    {
        // ⚡ Bolt: Use AnyAsync for O(1) existence check in SQL (EXISTS) instead of loading matching entities
        if (await _unitOfWork.Repository<User>().AnyAsync(u => u.Email == request.Email))
            return ApiResponse<UserDto>.FailureResponse(new List<string> { "Email already exists" });

        var user = new User { Email = request.Email, Name = request.Name, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), Status = UserStatus.ACTIVE };
        await _unitOfWork.Repository<User>().AddAsync(user);

        // ⚡ Bolt: Use FirstOrDefaultAsync to fetch a single role directly
        var role = await _unitOfWork.Repository<Role>().FirstOrDefaultAsync(r => r.Name == request.Role)
                   ?? await _unitOfWork.Repository<Role>().FirstOrDefaultAsync(r => r.Name == "TENANT");

        if (role != null) await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });

        await _unitOfWork.SaveAsync();
        return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), "User registered successfully");
    }

    public async Task<ApiResponse<UserDto>> SignupAsync(RegisterRequest request)
    {
        // Similar to register but handles email verification requirement
        return await RegisterAsync(request);
    }

    public async Task<ApiResponse<bool>> VerifyEmailAsync(OtpRequest request)
    {
        var otps = await _unitOfWork.Repository<OtpCode>().FindAsync(o => o.Email == request.Email && o.Code == request.Code && o.Type == "email_verification" && o.ExpiresAt > DateTime.UtcNow);
        var otp = otps.FirstOrDefault();
        if (otp == null) return ApiResponse<bool>.FailureResponse(new List<string> { "Invalid or expired OTP" });

        otp.VerifiedAt = DateTime.UtcNow;
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Email verified successfully");
    }

    public async Task<ApiResponse<bool>> SendVerificationEmailAsync(string email)
    {
        // Mock sending email
        var otp = new OtpCode { Email = email, Code = "123456", Type = "email_verification", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
        await _unitOfWork.Repository<OtpCode>().AddAsync(otp);
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Verification email sent");
    }

    public async Task<ApiResponse<bool>> SendPasswordResetOtpAsync(string email)
    {
        var otp = new OtpCode { Email = email, Code = "654321", Type = "password_reset", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
        await _unitOfWork.Repository<OtpCode>().AddAsync(otp);
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Password reset OTP sent");
    }

    public async Task<ApiResponse<bool>> ValidatePasswordResetOtpAsync(OtpRequest request)
    {
        var otps = await _unitOfWork.Repository<OtpCode>().FindAsync(o => o.Email == request.Email && o.Code == request.Code && o.Type == "password_reset" && o.ExpiresAt > DateTime.UtcNow);
        return ApiResponse<bool>.SuccessResponse(otps.Any(), otps.Any() ? "OTP validated" : "Invalid OTP");
    }

    public async Task<ApiResponse<bool>> ResetPasswordWithOtpAsync(string email, string newPassword)
    {
        var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();
        if (user == null) return ApiResponse<bool>.FailureResponse(new List<string> { "User not found" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Password reset successful");
    }

    public async Task<ApiResponse<bool>> SendLoginOtpAsync(string emailOrPhone)
    {
        var otp = new OtpCode { Email = emailOrPhone, Code = "111222", Type = "login_verification", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
        await _unitOfWork.Repository<OtpCode>().AddAsync(otp);
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Login OTP sent");
    }

    public async Task<ApiResponse<LoginResponse>> VerifyLoginOtpAsync(OtpRequest request)
    {
        var result = await VerifyEmailAsync(request); // Reuse generic OTP verification
        if (!result.Success) return ApiResponse<LoginResponse>.FailureResponse(result.Errors);

        var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == request.Email);
        var user = users.FirstOrDefault();
        return ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse { Token = "mock_token", User = _mapper.Map<UserDto>(user) });
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId)
    {
        // Mock permission logic based on documentation functions
        return ApiResponse<IEnumerable<string>>.SuccessResponse(new List<string> { "VIEW_PROPERTIES", "CREATE_ISSUES" });
    }

    public async Task<ApiResponse<bool>> UserHasPermissionAsync(Guid userId, string permissionCode)
    {
        // Mock check logic
        return ApiResponse<bool>.SuccessResponse(true);
    }

    private string GenerateJwtToken(User user, IEnumerable<string> roles)
    {
        var jwtSecret = _configuration["Jwt:Key"] ?? "default_development_key_for_rentolic_system_12345";
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email) };
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(tokenDescriptor));
    }
}
