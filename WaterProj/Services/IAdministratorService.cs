using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IAdministratorService
    {
        /// <summary>
        /// Получение администратора по ID
        /// </summary>
        Task<Administrator> GetByIdAsync(int id);

        /// <summary>
        /// Проверка наличия администраторов в системе
        /// </summary>
        Task<bool> HasAdminsAsync();

        /// <summary>
        /// Создание первого администратора
        /// </summary>
        Task<ServiceResult> CreateFirstAdminAsync(string name, string login, string password);

        /// <summary>
        /// Обновление данных администратора
        /// </summary>
        Task<ServiceResult> UpdateAdminAsync(int adminId, Administrator model);

        /// <summary>
        /// Изменение пароля администратора
        /// </summary>
        Task<ServiceResult> ChangePasswordAsync(int adminId, string currentPassword, string newPassword);

        /// <summary>
        /// Получение списка всех пользователей системы
        /// </summary>
        Task<(List<Consumer> Consumers, List<Transporter> Transporters)> GetAllUsersAsync();

        /// <summary>
        /// Получение списка всех маршрутов в системе
        /// </summary>
        Task<List<Models.Route>> GetAllRoutesAsync();

        // WaterProj/Services/IAdministratorService.cs
        /// <summary>
        /// Блокировка/разблокировка маршрута администратором
        /// </summary>
        Task<ServiceResult> ToggleRouteBlockStatusAsync(int routeId, bool isBlocked, string blockReason = null);

        /// <summary>
        /// Получение детальной информации о маршруте
        /// </summary>
        Task<Models.Route> GetRouteDetailsByIdAsync(int routeId);

        /// <summary>
        /// Блокировка перевозчика администратором
        /// </summary>
        Task<ServiceResult> BlockTransporterAsync(int transporterId, string blockReason);

        /// <summary>
        /// Разблокировка перевозчика администратором
        /// </summary>
        Task<ServiceResult> UnblockTransporterAsync(int transporterId);

    }
}