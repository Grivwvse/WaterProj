using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.DTOs;
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

            return await _context.Set<Order>()
                .AnyAsync(o => o.RouteId == routeId &&
                               o.ConsumerId == userId &&
                               o.Status == OrderStatus.Active);
        }

        public async Task<ServiceResult> CompleteOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Set<Order>().FindAsync(orderId);
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

        public async Task<ServiceResult> CancelOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Set<Order>().FindAsync(orderId);
                if (order == null)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Заказ не найден."
                    };
                }
                order.Status = OrderStatus.Canceled;
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
            return await _context.Set<Order>()
                .Where(o => o.ConsumerId == consumerId)
                .ToListAsync();
        }
        public async Task<ServiceResult> CreateOrder(int routeId, int consumerId)
        {
            try
            {
                // Проверяем статус маршрута перед созданием заказа
                var route = await _context.Routes.FindAsync(routeId);

                if (route == null)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Маршрут не найден."
                    };
                }

                // Проверка активности и блокировки маршрута
                if (!route.IsActive)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Невозможно заказать маршрут, так как он недоступен в настоящее время."
                    };
                }

                if (route.IsBlocked)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = $"Невозможно заказать маршрут, так как он заблокирован администратором. Причина: {route.BlockReason ?? "не указана"}"
                    };
                }

                // Проверяем, есть ли уже активный заказ на этот маршрут у этого пользователя
                bool hasActiveOrder = await _context.Orders
                    .AnyAsync(o => o.RouteId == routeId &&
                                 o.ConsumerId == consumerId &&
                                 o.Status == OrderStatus.Active);

                if (hasActiveOrder)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "У вас уже есть активный заказ на этот маршрут."
                    };
                }

                // Логика создания заказа
                var order = new Order
                {
                    RouteId = routeId,
                    ConsumerId = consumerId,
                    Status = OrderStatus.Active,
                    CreatedAt = DateTime.UtcNow // Устанавливаем время создания заказа
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
                    ErrorMessage = $"Ошибка при создании заказа: {ex.Message}"
                };
            }
        }

        public async Task<Order> GetOrderbyId(int orderId)
        {
            return await _context.Set<Order>().Where(o => o.OrderId == orderId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Получение заказа с деталями маршрута и транспортёра
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<Order> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Set<Order>()
                .Include(o => o.Route)
                .ThenInclude(r => r.Transporter)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        /// <summary>
        /// Получение количества заказов для указанных маршрутов
        /// </summary>
        /// <param name="routeIds"></param>
        /// <param name="includeOnlyActiveOrders"></param>
        /// <returns></returns>
        public async Task<Dictionary<int, int>> GetOrderCountsForRoutesAsync(IEnumerable<int> routeIds, bool includeOnlyActiveOrders = false)
        {
            IQueryable<Order> query = _context.Set<Order>().Where(o => routeIds.Contains(o.RouteId));

                query = query.Where(o => o.Status == OrderStatus.Completed);


            return await query
                .GroupBy(o => o.RouteId)
                .Select(g => new { RouteId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RouteId, x => x.Count);
        }

        /// <summary>
        /// Получение количества заказов для указанного маршрута
        /// </summary>
        /// <param name="routeId">ID маршрута</param>
        /// <param name="includeOnlyActiveOrders">Учитывать только активные заказы (если true)</param>
        /// <returns>Количество заказов</returns>
        public async Task<int> GetOrderCountForRouteAsync(int routeId, bool includeOnlyActiveOrders = false)
        {
            if (includeOnlyActiveOrders)
            {
                return await _context.Set<Order>()
                    .CountAsync(o => o.RouteId == routeId && o.Status == OrderStatus.Active);
            }

            return await _context.Set<Order>()
                .CountAsync(o => o.RouteId == routeId && o.Status == OrderStatus.Completed);
        }

        /// <summary>
        /// Получение статистики заказов для указанного маршрута
        /// </summary>
        /// <param name="routeId">ID маршрута</param>
        /// <returns>Объект со статистикой заказов</returns>
        public async Task<RouteOrderStatsDto> GetOrderStatsForRouteAsync(int routeId)
        {
            // Получаем статистику одним запросом для лучшей производительности
            var orderStats = await _context.Orders
                .Where(o => o.RouteId == routeId)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Заполняем статистику
            var stats = new RouteOrderStatsDto
            {
                TotalOrders = orderStats.Sum(x => x.Count),
                ActiveOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Active)?.Count ?? 0,
                CompletedOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Completed)?.Count ?? 0,
                CanceledOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Canceled)?.Count ?? 0
            };

            return stats;
        }
    }
}
