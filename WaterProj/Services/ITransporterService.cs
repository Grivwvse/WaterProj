using WaterProj.Models.Services;
using WaterProj.Models;

namespace WaterProj.Services
{
    public interface ITransporterService
    {
        Task<Transporter> GetByIdAsync(int id);
        Task<ServiceResult> UpdateTransporterAsync(int userId, Transporter model);
    }
}
