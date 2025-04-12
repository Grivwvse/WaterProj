using Dprog.Models;
using Microsoft.AspNetCore.Mvc;
using WaterProj.DB;

namespace Dprog.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegistrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(Consumer model)
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

                _context.Consumers.Add(model);
                _context.SaveChanges();

                // После успешной регистрации перенаправляем пользователя на страницу авторизации.
                return RedirectToAction("Index", "Authorization");
           }
            // Если валидация не проходит, добавляем общее сообщение об ошибке
            ModelState.AddModelError(string.Empty, "Пожалуйста, проверьте введенные данные.");
            return View(model);
        }
    }
}
