using WaterProj.Models.Services;
using WaterProj.Models;
using WaterProj.DTOs;

namespace WaterProj.Services
{
    public interface ITransporterService
    {
        Task<ServiceResult> ChangePasswordAsync(int transporterId, string currentPassword, string newPassword);
        Task<TransporterAccountDto> GetAllAccountInfo(int transporterId);
        Task<Transporter> GetByIdAsync(int id);
        Task<ServiceResult> UpdateTransporterAsync(int userId, Transporter model);

        /// <summary>
        /// Изменение статуса активности маршрута перевозчиком
        /// </summary>
        Task<ServiceResult> ToggleRouteActivityAsync(int routeId, int transporterId, bool isActive);

        /// <summary>
        /// Получение маршрута по ID с проверкой прав перевозчика
        /// </summary>
        Task<Models.Route> GetRouteByIdWithAuthorizationAsync(int routeId, int transporterId);
        Task<Transporter> GetTransporterInfoByIdAsync(int id);
    }
}
