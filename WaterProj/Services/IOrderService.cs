using Microsoft.AspNetCore.Mvc;
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
    }
}
