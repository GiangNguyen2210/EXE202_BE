using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EXE202_BE.Data.DTOS.Auth;
using EXE202_BE.Data.DTOS.User;
using EXE202_BE.Data.Models;
using EXE202_BE.Repository.Interface;
using EXE202_BE.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EXE202_BE.Service.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ModifyIdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IUserProfilesService _userProfilesService;
    private readonly IUserProfilesRepository _userProfilesRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;
    
    public AuthService(
        UserManager<ModifyIdentityUser> userManager,
        IConfiguration configuration,
        IUserProfilesService userProfilesService,
        IUserProfilesRepository userProfilesRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _userProfilesService = userProfilesService;
        _userProfilesRepository = userProfilesRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequestDTO model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            throw new Exception("Invalid email or password");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Admin") && !roles.Contains("Staff"))
        {
            throw new Exception("Your account is unauthorized.");
        }
        
        var userProfile = await _userProfilesRepository.GetAsync( 
            up => up.UserId == user.Id);

        if (userProfile == null)
        {
            throw new Exception("User profile not found.");
        }
        

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = roles.FirstOrDefault(),
            UPId = userProfile.UPId
        };
    }
    public async Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            _logger.LogInformation("Starting ChangePasswordAsync for request.");

            // Kiểm tra HttpContext
            if (_httpContextAccessor.HttpContext == null)
            {
                _logger.LogError("HttpContext is null in ChangePasswordAsync.");
                throw new InvalidOperationException("HttpContext is not available.");
            }

            // Lấy user ID từ token
            var userIdClaim = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                _logger.LogWarning("Missing or invalid user ID claim in token.");
                throw new UnauthorizedAccessException("User not authenticated or missing user ID claim.");
            }
            var userId = userIdClaim.Value;
            _logger.LogInformation("Retrieved user ID: {UserId}", userId);

            // Tìm user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                throw new KeyNotFoundException("User not found.");
            }

            // Kiểm tra mật khẩu cũ
            var passwordCheck = await _userManager.CheckPasswordAsync(user, request.OldPassword);
            if (!passwordCheck)
            {
                _logger.LogInformation("Invalid current password for user ID: {UserId}", userId);
                throw new ArgumentException("Invalid current password.");
            }

            // Đổi mật khẩu
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to change password for user ID: {UserId}. Errors: {Errors}", userId, errors);
                throw new InvalidOperationException($"Failed to change password: {errors}");
            }

            _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);
        }
}