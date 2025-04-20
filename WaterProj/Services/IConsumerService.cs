using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
namespace WaterProj.Services
{
    public interface IConsumerService
    {
        Task<Consumer> GetByIdAsync(int id);
        Task<ServiceResult> UpdateConsumerAsync(int userId, Consumer model);

        Task<ConsumerAccontDto> GetAllAccountInfo(int id);  
    }
}