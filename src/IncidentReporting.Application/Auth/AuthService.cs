using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Dtos;
using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
public AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
{
    _userRepository = userRepository;
    _passwordHasher = passwordHasher;
    _jwtTokenGenerator = jwtTokenGenerator;
}

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(
            request.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Email already exists.",
                Errors = new List<string> { "A user with this email already exists." }
            };
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(
            user,
            cancellationToken);

        return new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "User registered successfully.",
            Data = new AuthResponse
            {
                UserId = createdUser.Id,
                FullName = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.Role
            }
        };
    }
    public async Task<ApiResponse<string>> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByEmailAsync(
        request.Email,
        cancellationToken);

    if (user is null)
    {
        return new ApiResponse<string>
        {
            Success = false,
            Message = "Invalid credentials."
        };
    }

    var isPasswordValid = _passwordHasher.VerifyPassword(
        request.Password,
        user.PasswordHash);

    if (!isPasswordValid)
    {
        return new ApiResponse<string>
        {
            Success = false,
            Message = "Invalid credentials."
        };
    }

    var token = _jwtTokenGenerator.GenerateToken(user);

    return new ApiResponse<string>
    {
        Success = true,
        Message = "Login successful.",
        Data = token
    };
}
}