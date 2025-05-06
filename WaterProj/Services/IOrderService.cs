using Microsoft.AspNetCore.Mvc;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public interface IOrderService 
    {
        Task<ServiceResult> CreateOrder(int routeID, int consumerId);
        Task<List<Order>> GetOrdersByConsumerId(int consumerId);
        Task<bool> IsRouteInActiveOrdersAsync(int routeId, int userId);
        Task<Order> GetOrderByConsumerAndRouteAsync(int consumerId, int routeId);
        Task<ServiceResult> CompleteOrderAsync(int orderId);
        Task<Order> GetOrderWithDetailsAsync(int orderId);
        Task<Order> GetOrderbyId(int orderId);
        Task<ServiceResult> CancelOrderAsync(int orderId);
        /// <summary>
        /// Получение количества заказов для указанного маршрута
        /// </summary>
        /// <param name="routeId">ID маршрута</param>
        /// <param name="includeOnlyActiveOrders">Учитывать только активные заказы (если true)</param>
        /// <returns>Количество заказов</returns>
        Task<int> GetOrderCountForRouteAsync(int routeId, bool includeOnlyActiveOrders = false);
        Task<Dictionary<int, int>> GetOrderCountsForRoutesAsync(IEnumerable<int> routeIds, bool includeOnlyActiveOrders = false);
        /// <summary>
        /// Получение статистики заказов для указанного маршрута
        /// </summary>
        /// <param name="routeId">ID маршрута</param>
        /// <returns>Объект со статистикой заказов</returns>
        Task<RouteOrderStatsDto> GetOrderStatsForRouteAsync(int routeId);
    }
}
