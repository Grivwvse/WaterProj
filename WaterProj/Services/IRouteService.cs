using WaterProj.DTOs;
using WaterProj.Models;

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

        /// <summary>
        /// Метод для получения списка маршрутов по их ID
        /// </summary>
        /// <param name="routeIds"></param>
        /// <returns></returns>
        Task<List<Models.Route>> GetRoutesByIdsAsync(List<int> routeIds);


        // Добавляем метод для поиска маршрутов
        Task<List<RouteSearchResultDto>> SearchRoutesAsync(RouteSearchDto searchParams);

        // Метод для поиска по остановкам
        Task<List<RouteSearchResultDto>> FindRoutesByStopsAsync(List<int> startStopIds, List<int> endStopIds);
        

    }
}
