using EXE202_BE.Data.DTOS;
using EXE202_BE.Data.DTOS.Auth;
using EXE202_BE.Data.DTOS.User;
using EXE202_BE.Data.Models;
using Microsoft.AspNetCore.Http;

namespace EXE202_BE.Service.Interface;

public interface IUserProfilesService
{
    Task<UserProfiles> AddAsync(UserProfiles userProfile);
    Task<PageListResponse<UserProfileResponse>> GetUsersAsync(string? searchTerm, int page, int pageSize);
    Task<UserProfileResponse> GetUserProfileAsync(int upId);
    Task<AdminProfileResponse> GetAdminProfileAsync(int upId);
    Task<AdminProfileResponse> UpdateAdminProfileAsync(int upId, AdminProfileResponse request);
    Task<UserProfileResponse> CreateUserAsync(CreateUserRequestDTO model);
    Task<ProfileImageResponseDTO> UploadProfileImageAsync(int upId, IFormFile image);
    Task<UserProfileResponse> UpdateUserProfileAsync(int upId, UpdateUserProfileRequestDTO model);
    Task<UserProfiles> CreateUserProfilesAsync(SignUpRequest model, ModifyIdentityUser modifyIdentityUser);
    
}