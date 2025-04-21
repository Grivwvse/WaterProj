using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WaterProj.DB;
using Microsoft.AspNetCore.Authorization;
using WaterProj.Services;
using WaterProj.Models;
using WaterProj.DTOs;

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
           ConsumerAccontDto consumerAccontDto = await _consumerService.GetAllAccountInfo(currentConsumerId);

           return View(consumerAccontDto);
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
                    TempData["SuccessMessage"] = "Данные успешно обновлены!";
                }
                else
                {
                    TempData["ErrorMessage"] = serviceResult.ErrorMessage ?? "Произошла ошибка при обновлении данных.";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Authorization");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage(IFormFile profileImage)
        {
            if (profileImage == null || profileImage.Length == 0)
            {
                TempData["ErrorMessage"] = "Пожалуйста, выберите файл";
                return RedirectToAction("Index");
            }

            // Проверка типа файла
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Допустимы только изображения (.jpg, .jpeg, .png)";
                return RedirectToAction("Index");
            }

            if (profileImage.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Размер файла не должен превышать 5MB";
                return RedirectToAction("Index");
            }

            var consumerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Обновляем изображение
            var result = await _consumerService.UpdateProfileImage(consumerId, profileImage);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Аватар профиля успешно обновлен";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Не удалось обновить аватар профиля";
            }

            return RedirectToAction("Index");
        }
    }
}
