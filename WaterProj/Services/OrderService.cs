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

        public async Task<ServiceResult> CompleteOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Заказ не найден."
                    };
                }
                order.Status = OrderStatus.Completed;
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

        public async Task<Order> GetOrderByConsumerAndRouteAsync(int consumerId, int routeId)
        {
            var orders = await GetOrdersByConsumerId(consumerId);
            return orders.FirstOrDefault(o => o.RouteId == routeId && o.Status == OrderStatus.Active);
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

        public async Task<Order> GetOrderbyId(int orderId)
        {
            return await _context.Orders.Where(o => o.OrderId == orderId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Получение заказа с деталями маршрута и транспортёра
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<Order> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Route)
                .ThenInclude(r => r.Transporter)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}
