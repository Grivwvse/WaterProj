using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WaterProj.Models;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class OrderController : Controller
    {

        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize(Roles = "consumer")]
        public async Task<IActionResult> QuickOrderAsync(int routeId)
        {
            // 1. Получаем ID текущего пользователя из аутентификационного контекста
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var serviceResult = await _orderService.CreateOrder(routeId, userId);



            return RedirectToAction("Index", "ConsumerAccount");
        }

    }
}
