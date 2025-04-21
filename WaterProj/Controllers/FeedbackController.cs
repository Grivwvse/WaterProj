using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ITransporterService _transporterService;
        private readonly IRouteService _routeService;
        private readonly IOrderService _orderService;

        public FeedbackController(IFeedbackService feedbackService, ITransporterService transporterService, IRouteService routeService, IOrderService orderService)
        {
            _feedbackService = feedbackService;
            _transporterService = transporterService;
            _routeService = routeService;
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> FeedbackForm(int orderID)
        {


            if (orderID == null)
            {
                TempData["ErrorMessage"] = "Некорректный ID заказа!";
                return RedirectToAction("Index", "ConsumerAccount");
            }

            var orderHasFeedback = await _feedbackService.CheckIsFeedbackExist(orderID);
            if (orderHasFeedback.Success)
            {
                TempData["ErrorMessage"] = "Вы уже оставили отзыв на этот заказ!";
                return RedirectToAction("Index", "ConsumerAccount");
            }
            else if (!orderHasFeedback.Success && orderHasFeedback.ErrorMessage != null)
            {
                TempData["ErrorMessage"] = orderHasFeedback.ErrorMessage;
                return RedirectToAction("Index", "ConsumerAccount");
            }

            var feedbackDataForDto = await _orderService.GetOrderWithDetailsAsync(orderID);

            var feedbackDto = new FeedbackDto
            {
                OrderId = orderID,
                RouteId = feedbackDataForDto.RouteId,
                TransporterId = feedbackDataForDto.Route.TransporterId,
                TransporterName = feedbackDataForDto.Route.Transporter.Name,
                RouteName = feedbackDataForDto.Route.Name,
                AvailableAdvantages = await _feedbackService.GetAvailableRouteAdvantages(),
            };

            return View(feedbackDto);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FeedbackDto model, IFormFileCollection photos)
        {
            // TODO : Валидация модели


            // Проверяем, что выбран рейтинг
            if (model.Stars < 1)
            {
                ModelState.AddModelError("Stars", "Пожалуйста, выберите оценку от 1 до 5 звезд");
                return View("FeedbackForm", model);
            }

            var currentConsumerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentConsumerId == null)
            {
                TempData["ErrorMessage"] = "Пользователь не найден!";
                return RedirectToAction("FeedbackForm", "model");
            }

            var result = await _feedbackService.SaveFeedback(model, photos, Convert.ToInt32(currentConsumerId));
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Отзыв успешно оставлен! Спасибо :)";
                return RedirectToAction("Index", "ConsumerAccount");
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Произошла ошибка при сохранении отзыва.";
                return RedirectToAction("FeedbackForm", (model));
            }
        }
    }
}
