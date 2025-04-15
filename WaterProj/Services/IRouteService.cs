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

        // Добавляем метод для поиска маршрутов
        Task<List<RouteSearchResultDto>> SearchRoutesAsync(RouteSearchDto searchParams);

        // Метод для поиска по остановкам
        Task<List<RouteSearchResultDto>> FindRoutesByStopsAsync(List<int> startStopIds, List<int> endStopIds);
    }
}
