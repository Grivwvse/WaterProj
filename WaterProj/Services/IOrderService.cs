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
    }
}
