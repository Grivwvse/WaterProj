using WaterProj.Models.Services;
using WaterProj.Models;
using WaterProj.DTOs;

namespace WaterProj.Services
{
    public interface ITransporterService
    {
        Task<TransporterAccountDto> GetAllAccountInfo(int transporterId);
        Task<Transporter> GetByIdAsync(int id);
        Task<ServiceResult> UpdateTransporterAsync(int userId, Transporter model);
    }
}
