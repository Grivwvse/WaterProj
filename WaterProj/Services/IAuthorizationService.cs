using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IAuthorizationService
    {
        Task<Consumer> AuthConsumer(string login, string password);
        Task<Transporter> AuthTransporter(string login, string password);
        Task<Administrator> AuthAdmin(string login, string password);
        Task<ServiceResult> CommonAuth(string login, string password, string userType, HttpContext httpContext);
    }
}
