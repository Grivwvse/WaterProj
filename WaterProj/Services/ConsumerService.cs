using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class ConsumerService : IConsumerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IRouteService _routeService;

        public ConsumerService(ApplicationDbContext context, IOrderService orderService, IRouteService routeService)
        {
            _context = context;
            _orderService = orderService;
            _routeService = routeService;
        }

        public async Task<Consumer> GetByIdAsync(int id)
        {
            return await _context.Consumers.FindAsync(id);
        }

        public async Task<ConsumerAccontDto> GetAllAccountInfo(int id)
        {
            var consumer = await GetByIdAsync(id);
            if (consumer == null)
                throw new NotFoundException("Пользователь не найден.");

            List<Order> orders = await _orderService.GetOrdersByConsumerId(id);

            if (orders == null)
                throw new NotFoundException("Заказы не найдены.");

            
            var routeIds = orders.Select(o => o.RouteId).ToList();
            var routes = await _routeService.GetRoutesByIdsAsync(routeIds);


            var ordersRoutesDto = orders.Select(order =>
            {
                var route = routes.FirstOrDefault(r => r.RouteId == order.RouteId);
                return new OrdersRoutesDto
                {
                    Order = order,
                    Route = route
                };
            }).ToList();

            ConsumerAccontDto consumerAccountDto = new ConsumerAccontDto
            {
                OrdersRoutesDto = ordersRoutesDto,
                Consumer = consumer
            };

            return consumerAccountDto;
        }

        public async Task<ServiceResult> UpdateConsumerAsync(int userId, Consumer model)
        {
            var consumer = await _context.Consumers.FindAsync(userId);
            if (consumer == null)
                return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден." };

            consumer.Login = model.Login;
            consumer.Name = model.Name;

            _context.Consumers.Update(consumer);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }

    }


}
