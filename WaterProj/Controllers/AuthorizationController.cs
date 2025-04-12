using Dprog.Models;
using Microsoft.AspNetCore.Mvc;
using WaterProj.DB;

namespace Dprog.Controllers
{
    public class AuthorizationController : Controller
    {

        private readonly ApplicationDbContext _context;

        public AuthorizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

 [HttpPost]
        public IActionResult Index(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ViewBag.AuthData = "Пожалуйста, укажите логин и пароль.";
                return View();
            }

            // Поиск пользователя по логину
            var consumer = _context.Consumers.FirstOrDefault(c => c.Login == login);
            if (consumer == null)
            {
                ViewBag.AuthData = "Пользователь не найден.";
                return View();
            }

            // Здесь можно добавить логику проверки хешированного пароля
            if (consumer.PasswordHash == password)
            {
                // Авторизация успешна. Обычно здесь происходит установка куки/claims.
                ViewBag.AuthData = "Успешная авторизация.";
                // Можно сделать RedirectToAction на страницу личного кабинета.
            }
            else
            {
                ViewBag.AuthData = "Неверный пароль.";
            }

            return View();
        }

    }
}
