using WaterProj.DB;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        public RegistrationService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ServiceResult> RegisterTransporterAsync(Transporter model)
        {
            try
            {
                await _context.Transporters.AddAsync(model);
                await _context.SaveChangesAsync();
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ServiceResult> RegisterCounsumerAsync(Consumer model)
        {
            try
            {
                await _context.Consumers.AddAsync(model);
                await _context.SaveChangesAsync();
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

}
