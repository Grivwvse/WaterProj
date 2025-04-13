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

                // Можно добавить логику хеширования пароля, например:
                // model.PasswordHash = PasswordHasher.Hash(model.PasswordHash);
                var serviceResult = await _registrationService.RegisterCounsumerAsync(model);
                if (!serviceResult.Success)
                {
                    // Если произошла ошибка, можно добавить сообщение в ModelState
                    ModelState.AddModelError(string.Empty, serviceResult.ErrorMessage);
                    return View(model);
                }

                // После успешной регистрации перенаправляем пользователя на страницу основную.
                return RedirectToAction("Index", "");
           }
            // Если валидация не проходит, добавляем общее сообщение об ошибке
            ModelState.AddModelError(string.Empty, "Пожалуйста, проверьте введенные данные.");
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> RegisterTransporter(Transporter model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var serviceResult = await _registrationService.RegisterTransporterAsync(model);

            if (!serviceResult.Success)
            {
                // Если произошла ошибка, можно добавить сообщение в ModelState
                ModelState.AddModelError(string.Empty, serviceResult.ErrorMessage);
                ViewBag.Message = "Пожалуйста, проверьте введенные данные.";
                return View(model);
                
            }
            // TODO: Добавить регистрацию Transporter
            // Пример: await _userService.CreateTransporterAsync(model);

            ViewBag.Message = "Перевозчик успешно зарегистрирован!";
            return View();
        }
    }
}
