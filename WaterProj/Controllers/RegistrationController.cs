using WaterProj.Models;
using Microsoft.AspNetCore.Mvc;
using WaterProj.DB;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly IRegistrationService _registrationService;

        public RegistrationController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        // Consumer
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        //Transporter
        [HttpGet]
        public IActionResult RegisterTransporter()
        {
            return View();
        }

        //Consumer

        [HttpPost]
        public async Task<IActionResult> Index(Consumer model)
        {

           if (ModelState.IsValid)
            {
                var serviceResult = await _registrationService.RegisterCounsumerAsync(model);
                if (serviceResult.Success)
                {
                    TempData["SuccessMessage"] = "Успешная регистрация!";
                }
                else
                {
                    TempData["ErrorMessage"] = serviceResult.ErrorMessage ?? "Произошла ошибка при регистрации.";
                }

                // После успешной регистрации перенаправляем пользователя на страницу основную.
                return RedirectToAction("Index", "Authorization");
           }
            // Если валидация не проходит, добавляем общее сообщение об ошибке
            ModelState.AddModelError(string.Empty, "Пожалуйста, проверьте введенные данные.");
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> CheckLoginAvailability(string login)
        {
            if (string.IsNullOrEmpty(login))
            {
                return Json(new { available = false, message = "Логин не может быть пустым" });
            }

            // Проверяем, соответствует ли логин требованиям формата
            if (login.Length < 3 || !System.Text.RegularExpressions.Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$"))
            {
                return Json(new { available = false, message = "Неверный формат логина" });
            }

            // Обращаемся к сервису для проверки доступности логина
            bool isAvailable = await _registrationService.IsLoginAvailable(login);

            return Json(new
            {
                available = isAvailable,
                message = isAvailable ? "Логин доступен" : "Этот логин уже используется"
            });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterTransporter(Transporter model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Пожалуйста, проверьте введенные данные.";
                return View(model);
            }
                

            var serviceResult = await _registrationService.RegisterTransporterAsync(model);

            if (serviceResult.Success)
            {
                TempData["SuccessMessage"] = "Успешная регистрация!";
            }
            else
            {
                TempData["ErrorMessage"] = serviceResult.ErrorMessage ?? "Произошла ошибка при регистрации.";
            }

            return RedirectToAction("Index", "Authorization");

        }
    }
}
