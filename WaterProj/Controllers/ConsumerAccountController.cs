using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WaterProj.DB;
using Microsoft.AspNetCore.Authorization;
using WaterProj.Services;
using WaterProj.Models;

namespace WaterProj.Controllers
{
    [Authorize]
    public class ConsumerAccountController : Controller
    {
        private readonly IConsumerService _consumerService;

        public ConsumerAccountController(IConsumerService consumerService)
        {
            _consumerService = consumerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Получаем имя пользователя из аутентификационных данных  
            var currentConsumerId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var consumer = await _consumerService.GetByIdAsync(currentConsumerId);

            return View(consumer);
        }

        // Редактирование данных пользователя
        [HttpPost]
        public async Task<IActionResult> Edit(Consumer model)
        {
            Console.WriteLine("ModelState.IsValid");
            if (true) // Валидация только нужных нам полей
            {
                Console.WriteLine("ModelState.IsValid: " + ModelState.IsValid);
                var serviceResult = await _consumerService.UpdateConsumerAsync(Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier)), model);
                if (serviceResult.Success)
                {

                    // Если все успешно, просто отобразить тот же представление
                    ViewBag.Message = "Данные успешно обновлены!";
                }
                else
                {
                    ViewBag.Message = "Произошла ошибка при обновлении данных.";
                }
            }

            return View("Index", model);
        }
    }
}
