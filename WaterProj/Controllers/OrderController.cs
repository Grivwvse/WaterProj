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

        public async Task<IActionResult> QuickOrder(int routeId)
        {
            // 1. Получаем ID текущего пользователя из аутентификационного контекста
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var serviceResult = await _orderService.CreateOrder(routeId, userId);

            if (!serviceResult.Success)
            {
                TempData["ErrorMessage"] = serviceResult.ErrorMessage;
                return RedirectToAction("Index", "ConsumerAccount");
            }


            TempData["SuccessMessage"] = "Заказ успешно завершен!";
            return RedirectToAction("Index", "ConsumerAccount");
        }

        /// <summary>
        /// Функция завершения заказа и перенаправление на форму обратной связи.
        /// </summary>
        /// <param name="routeId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> CompleteOrder(int routeId)
        {
            var consumerStrId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(consumerStrId))
            {
                TempData["ErrorMessage"] = "Пользователь не найден!";
                return RedirectToAction("RouteDetails", "Route", new { id = routeId });
            }

            var order = await _orderService.GetOrderByConsumerAndRouteAsync(Convert.ToInt32(consumerStrId), routeId);

            if (order == null || order.Status!= OrderStatus.Active)
            {
                TempData["ErrorMessage"] = "Заказ не найден или уже был завершен!";
                return RedirectToAction("RouteDetails", "Route", new { id = routeId });
            }


            var result = await _orderService.CompleteOrderAsync(order.OrderId);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("RouteDetails", "Route", new { id = routeId });
            }

            TempData["SuccessMessage"] = "Заказ успешно завершен! Пожалуйста оставьте отзыв :)";
            return RedirectToAction("FeedbackForm", "Feedback", new { order.OrderId });
        }

    }
}
