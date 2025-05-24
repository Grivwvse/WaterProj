using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IRouteService
    {
        Task<Models.Route> GetByIdAsync(int id);
        Task<RouteDetailsDto> GetRouteDetails(int id);
        /// <summary>
        /// Получение доступных остановок для указанных маршрутов
        /// </summary>
        /// <param name="startStopId">ID начальной остановки</param>
        /// <param name="routeIds">Список ID маршрутов для фильтрации</param>
        /// <returns>Список доступных остановок</returns>
        Task<List<Stop>> GetAvailableStopsForRoutes(int startStopId, List<int> routeIds);


        /// <summary>
        /// Получение всех остановок из базы данных
        /// </summary>
        /// <returns>Список всех остановок</returns>
        Task<List<Stop>> GetAllStopsAsync();

        Task<ServiceResult> UpdateRoute(Models.Route route);

        /// <summary>
        /// Метод для получения списка маршрутов по их ID
        /// </summary>
        /// <param name="routeIds"></param>
        /// <returns></returns>
        Task<List<Models.Route>> GetRoutesByIdsAsync(List<int> routeIds);

        Task<List<Stop>> GetAvailableStops(int startStopId);


        Task<List<Models.Route>> GetRoutesByShip(int shipId);

        // Добавляем метод для поиска маршрутов
        Task<List<RouteSearchResultDto>> SearchRoutesAsync(RouteSearchDto searchParams);

        // Метод для поиска по остановкам
        Task<List<RouteSearchResultDto>> FindRoutesByStopsAsync(List<int> startStopIds, List<int> endStopIds);
        Task<(Stop? StartStop, Stop? EndStop)> GetRouteEndpointsAsync(int routeId);
        /// <summary>
        /// Обновление расписания, дней недели и корабля маршрута
        /// </summary>
        /// <param name="routeId">ID маршрута</param>
        /// <param name="shipId">ID назначаемого корабля</param>
        /// <param name="schedule">Новое расписание</param>
        /// <param name="routeDays">Новые дни недели</param>
        /// <returns>Результат операции</returns>
        Task<ServiceResult> EditRoute(int routeId, int shipId, string schedule, List<DayOfWeek> routeDays);



    }
}
