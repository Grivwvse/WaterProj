using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IRegistrationService
    {
        Task<ServiceResult> RegisterTransporterAsync(Transporter model);
        Task<ServiceResult> RegisterCounsumerAsync(Consumer model);
    }
}
