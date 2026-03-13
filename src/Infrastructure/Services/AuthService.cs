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
        var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == request.Email);
        var user = users.FirstOrDefault();

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<LoginResponse>.FailureResponse(new List<string> { "Invalid email or password" });
        }

        // Fetch roles for the user to include in the token
        var userRoles = await _unitOfWork.Repository<UserRole>().FindAsync(ur => ur.UserId == user.Id);
        var roleNames = new List<string>();
        foreach (var ur in userRoles)
        {
            var role = await _unitOfWork.Repository<Role>().GetByIdAsync(ur.RoleId);
            if (role != null) roleNames.Add(role.Name);
        }

        var token = GenerateJwtToken(user, roleNames);
        var userDto = _mapper.Map<UserDto>(user);

        return ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            Token = token,
            User = userDto
        });
    }

    public async Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request)
    {
        var existingUsers = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == request.Email);
        if (existingUsers.Any())
        {
            return ApiResponse<UserDto>.FailureResponse(new List<string> { "Email already exists" });
        }

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = UserStatus.ACTIVE
        };

        await _unitOfWork.Repository<User>().AddAsync(user);

        // Assign role if it exists, else default to TENANT
        var roles = await _unitOfWork.Repository<Role>().FindAsync(r => r.Name == request.Role);
        var role = roles.FirstOrDefault();
        if (role == null)
        {
            roles = await _unitOfWork.Repository<Role>().FindAsync(r => r.Name == "TENANT");
            role = roles.FirstOrDefault();
        }

        if (role != null)
        {
            await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _unitOfWork.SaveAsync();

        var userDto = _mapper.Map<UserDto>(user);
        return ApiResponse<UserDto>.SuccessResponse(userDto, "User registered successfully");
    }

    private string GenerateJwtToken(User user, IEnumerable<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSecret = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtSecret) || jwtSecret == "YOUR_JWT_SECRET_KEY")
        {
             jwtSecret = "default_development_key_for_rentolic_system_12345";
        }
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
