using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
namespace WaterProj.Services
{
    public interface IConsumerService
    {
        Task<Consumer> GetByIdAsync(int id);
        Task<ServiceResult> UpdateConsumerAsync(int userId, Consumer model);
        Task<bool> IsLoginExistsAsync(string login, int currentUserId);
        Task<ConsumerAccontDto> GetAllAccountInfo(int id);

        Task<ServiceResult> UpdateProfileImage(int consumerId, IFormFile imageFile);
        Task<ServiceResult> ChangePasswordAsync(int consumerId, string currentPassword, string newPassword);
    }
}