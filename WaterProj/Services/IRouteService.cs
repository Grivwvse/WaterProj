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

        Task<IEnumerable<Stop>> GetAvailableStops(int startStopId);


        Task<List<Models.Route>> GetRoutesByShip(int shipId);

        // Добавляем метод для поиска маршрутов
        Task<List<RouteSearchResultDto>> SearchRoutesAsync(RouteSearchDto searchParams);

        // Метод для поиска по остановкам
        Task<List<RouteSearchResultDto>> FindRoutesByStopsAsync(List<int> startStopIds, List<int> endStopIds);
        Task<(Stop? StartStop, Stop? EndStop)> GetRouteEndpointsAsync(int routeId);



    }
}
