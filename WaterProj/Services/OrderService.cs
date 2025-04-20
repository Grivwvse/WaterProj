using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Проверяет, добавлен ли конкретный заказ в список задазов конкретного пользователя.
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> IsRouteInActiveOrdersAsync(int routeId, int userId)
        {

            return await _context.Orders
                .AnyAsync(o => o.RouteId == routeId &&
                               o.ConsumerId == userId &&
                               o.Status == OrderStatus.Active);
        }

        public async Task<List<Order>> GetOrdersByConsumerId(int consumerId)
        {
            return await _context.Orders
                .Where(o => o.ConsumerId == consumerId)
                .ToListAsync();
        }
        public async Task<ServiceResult> CreateOrder(int routeId, int consumerId)
        {

            try
            {
                // Логика создания заказа
                var order = new Order
                {
                    RouteId = routeId,
                    ConsumerId = consumerId,
                    Status = OrderStatus.Active
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

    }
}
