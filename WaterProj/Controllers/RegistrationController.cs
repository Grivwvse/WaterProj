using WaterProj.Models;
using Microsoft.AspNetCore.Mvc;
using WaterProj.DB;
using WaterProj.Services;

namespace WaterProj.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly IConsumerService _consumerService;

        public RegistrationController(IConsumerService consumerService)
        {
            _consumerService = consumerService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(Consumer model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();
                // Здесь можно временно записать ошибки в лог или передать их в ViewBag для отладки
                ViewBag.ValidationErrors = errors;
                return View(model);
            }

           if (ModelState.IsValid)
            {

                // Можно добавить логику хеширования пароля, например:
                // model.PasswordHash = PasswordHasher.Hash(model.PasswordHash);
                var serviceResult = await _consumerService.AddCounsumerAsync(model);
                if (!serviceResult.Success)
                {
                    // Если произошла ошибка, можно добавить сообщение в ModelState
                    ModelState.AddModelError(string.Empty, serviceResult.ErrorMessage);
                    return View(model);
                }

                // После успешной регистрации перенаправляем пользователя на страницу основную.
                return RedirectToAction("Index", "HomeController");
           }
            // Если валидация не проходит, добавляем общее сообщение об ошибке
            ModelState.AddModelError(string.Empty, "Пожалуйста, проверьте введенные данные.");
            return View(model);
        }
    }
}
