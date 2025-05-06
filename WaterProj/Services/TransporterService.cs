using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace WaterProj.Services
{
    public class TransporterService : ITransporterService 
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;

        public TransporterService(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }


        public async Task<ServiceResult> ChangePasswordAsync(int transporterId, string currentPassword, string newPassword)
        {
            var transporter = await _context.Transporters.FindAsync(transporterId);
            if (transporter == null)
                return new ServiceResult { Success = false, ErrorMessage = "Перевозчик не найден." };

            // Создаем экземпляр PasswordHasher для проверки и хеширования паролей
            var passwordHasher = new PasswordHasher<Transporter>();

            // Проверяем текущий пароль
            var verificationResult = passwordHasher.VerifyHashedPassword(
                transporter,
                transporter.PasswordHash,
                currentPassword);

            if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                return new ServiceResult { Success = false, ErrorMessage = "Текущий пароль указан неверно." };

            // Хешируем новый пароль
            transporter.PasswordHash = passwordHasher.HashPassword(transporter, newPassword);

            _context.Transporters.Update(transporter);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }

        // Метод для шифрования пароля (должен соответствовать методу, используемому при регистрации)
        private string EncryptPassword(string password)
        {
            // Здесь следует использовать тот же метод шифрования, который применяется при регистрации
            // Например, хеширование пароля с использованием BCrypt или другой библиотеки
            return password; // Заглушка - замените на реальное шифрование
        }

        public async Task<TransporterAccountDto> GetAllAccountInfo(int transporterId)
        {
            //Берем перевозчика по ID из БД
            var transporter = await _context.Transporters
                .FirstOrDefaultAsync(t => t.TransporterId == transporterId);

            if (transporter == null)
                throw new NotFoundException("Транспортёр не найден");

            //Беерем маршруты по ID перевозчика
            var routes = await _context.Routes
                .Include(r => r.Ship)
                .Where(r => r.TransporterId == transporterId)
                .ToListAsync();

            //Беерем Корабли по ID перевозчика 
            var ships = await _context.Ships
                .Include(s => s.Transporter)
                .Include(s => s.ShipType)
                .Include(s => s.ShipImages)
                .Where(s => s.TransporterId == transporterId)
                .OrderBy(s => s.Status)
                .ToListAsync();


            var dto = new TransporterAccountDto
            {
                Transporter = transporter,
                Routes = routes,
                Ships = ships,
                RouteStats = new Dictionary<int, RouteOrderStatsDto>()
            };


            // Заполняем статистику заказов для каждого маршрута
            foreach (var route in routes)
            {
                var stats = await _orderService.GetOrderStatsForRouteAsync(route.RouteId);
                dto.RouteStats[route.RouteId] = stats;
            }

            return dto;
        }

        public async Task<Transporter> GetByIdAsync(int id)
        {
            return await _context.Transporters.FindAsync(id);
        }

        public async Task<Transporter> GetTransporterInfoByIdAsync(int id)
        {
            // Получаем перевозчика с его маршрутами и кораблями
            var transporter = await _context.Transporters
                .Include(t => t.Routes)
                    .ThenInclude(r => r.Ship) // Включаем информацию о корабле для каждого маршрута
                .Include(t => t.Ships)
                    .ThenInclude(s => s.ShipType) // Включаем тип корабля
                .Include(t => t.Ships)
                    .ThenInclude(s => s.ShipImages) // Включаем изображения кораблей
                .FirstOrDefaultAsync(t => t.TransporterId == id);

            if (transporter == null)
                throw new NotFoundException("Перевозчик не найден");

            return transporter;
        }

        public async Task<ServiceResult> UpdateTransporterAsync(int userId, Transporter model)
        {
            var transporter = await _context.Transporters.FindAsync(userId);
            if (transporter == null)
                return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден." };

            
            transporter.Name = model.Name;
            transporter.Email = model.Email;
            transporter.Phone = model.Phone;
            transporter.Description = model.Description;

            _context.Transporters.Update(transporter);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }

        private readonly IRouteService _routeService;


        public async Task<ServiceResult> ToggleRouteActivityAsync(int routeId, int transporterId, bool isActive)
        {
            try
            {
                Console.WriteLine($"ToggleRouteActivityAsync: routeId={routeId}, transporterId={transporterId}, isActive={isActive}");

                var route = await _context.Routes
                    .Include(r => r.Ship)
                    .FirstOrDefaultAsync(r => r.RouteId == routeId && r.TransporterId == transporterId);

                if (route == null)
                {
                    Console.WriteLine("Маршрут не найден или нет прав для его изменения");
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Маршрут не найден или вы не имеете прав для его изменения."
                    };
                }

                Console.WriteLine($"Текущий статус маршрута: IsActive={route.IsActive}, IsBlocked={route.IsBlocked}");

                // Если маршрут заблокирован администратором, перевозчик не может изменить его статус
                if (route.IsBlocked)
                {
                    Console.WriteLine("Маршрут заблокирован администратором");
                    return new ServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Маршрут заблокирован администратором. Причина: " + (route.BlockReason ?? "Не указана")
                    };
                }

                // Если пытаемся активировать маршрут, проверяем статус корабля
                if (isActive)
                {
                    // Проверяем статус корабля
                    if (route.Ship == null)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            ErrorMessage = "Невозможно активировать маршрут: к нему не привязан корабль."
                        };
                    }

                    if (route.Ship.Status != ShipStatus.Active)
                    {
                        string statusName = route.Ship.Status == ShipStatus.Maintenance
                            ? "находится на обслуживании"
                            : "неактивен";

                        return new ServiceResult
                        {
                            Success = false,
                            ErrorMessage = $"Невозможно активировать маршрут: корабль {route.Ship.Name} {statusName}. Сначала активируйте корабль."
                        };
                    }
                }

                Console.WriteLine($"Изменение статуса маршрута с {route.IsActive} на {isActive}");
                route.IsActive = isActive;

                // Явно отслеживаем изменение
                _context.Entry(route).State = EntityState.Modified;
                _context.Entry(route).Property(r => r.IsActive).IsModified = true;

                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"Сохранено изменений: {saveResult}");

                // Повторное получение маршрута для проверки сохранения
                var updatedRoute = await _context.Routes.FindAsync(routeId);
                Console.WriteLine($"Статус после сохранения: IsActive={updatedRoute?.IsActive}");

                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ToggleRouteActivityAsync: {ex.Message}");
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при изменении статуса маршрута: {ex.Message}"
                };
            }
        }

        public async Task<Models.Route> GetRouteByIdWithAuthorizationAsync(int routeId, int transporterId)
        {
            var route = await _context.Routes
                .Include(r => r.Ship)
                .FirstOrDefaultAsync(r => r.RouteId == routeId && r.TransporterId == transporterId);

            if (route == null)
                throw new NotFoundException("Маршрут не найден или вы не имеете прав для его просмотра.");

            return route;
        }
    }



    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
