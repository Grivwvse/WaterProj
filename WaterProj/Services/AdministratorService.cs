using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WaterProj.DB;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class AdministratorService : IAdministratorService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Administrator> _passwordHasher;

        public AdministratorService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Administrator>();
        }

        public async Task<Administrator> GetByIdAsync(int id)
        {
            return await _context.Administrators.FindAsync(id);
        }

        public async Task<bool> HasAdminsAsync()
        {
            return await _context.Administrators.AnyAsync();
        }

        public async Task<ServiceResult> ToggleRouteBlockStatusAsync(int routeId, bool isBlocked, string blockReason = null)
        {
            var route = await _context.Routes.FindAsync(routeId);

            if (route == null)
                return new ServiceResult { Success = false, ErrorMessage = "Маршрут не найден." };

            route.IsBlocked = isBlocked;

            // Если маршрут блокируется, сохраняем причину
            if (isBlocked)
            {
                route.BlockReason = blockReason;
            }
            else
            {
                route.BlockReason = null;
            }

            _context.Routes.Update(route);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }

        public async Task<Models.Route> GetRouteDetailsByIdAsync(int routeId)
        {
            var route = await _context.Routes
                .Include(r => r.Ship)
                .Include(r => r.Transporter)
                .Include(r => r.RouteStops)
                    .ThenInclude(rs => rs.Stop)
                .FirstOrDefaultAsync(r => r.RouteId == routeId);

            if (route == null)
                throw new NotFoundException("Маршрут не найден.");

            return route;
        }

        public async Task<ServiceResult> BlockTransporterAsync(int transporterId, string blockReason)
        {
            var transporter = await _context.Transporters.FindAsync(transporterId);

            if (transporter == null)
                return new ServiceResult { Success = false, ErrorMessage = "Перевозчик не найден." };

            // Блокируем перевозчика
            transporter.IsBlocked = true;
            transporter.BlockReason = blockReason;
            transporter.BlockedAt = DateTime.UtcNow; // Используем UTC время вместо локального

            // Деактивируем все маршруты перевозчика
            var routes = await _context.Routes
                .Where(r => r.TransporterId == transporterId && r.IsActive)
                .ToListAsync();

            foreach (var route in routes)
            {
                route.IsActive = false;
            }

            _context.Transporters.Update(transporter);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> UnblockTransporterAsync(int transporterId)
        {
            var transporter = await _context.Transporters.FindAsync(transporterId);

            if (transporter == null)
                return new ServiceResult { Success = false, ErrorMessage = "Перевозчик не найден." };

            // Разблокируем перевозчика
            transporter.IsBlocked = false;
            transporter.BlockReason = null;
            transporter.BlockedAt = null;

            _context.Transporters.Update(transporter);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> CreateFirstAdminAsync(string name, string login, string password)
        {
            // Проверяем, есть ли уже администраторы
            if (await HasAdminsAsync())
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "Невозможно создать первого администратора, так как администраторы уже существуют"
                };
            }

            // Проверяем, что все обязательные поля заполнены
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "Все поля обязательны для заполнения"
                };
            }

            try
            {
                // Хешируем пароль
                var newAdmin = new Administrator
                {
                    Name = name,
                    Login = login,
                    PasswordHash = _passwordHasher.HashPassword(null, password)
                };

                // Добавляем в базу данных
                _context.Administrators.Add(newAdmin);
                await _context.SaveChangesAsync();

                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при создании администратора: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult> UpdateAdminAsync(int adminId, Administrator model)
        {
            var admin = await _context.Administrators.FindAsync(adminId);
            if (admin == null)
                return new ServiceResult { Success = false, ErrorMessage = "Администратор не найден" };

            // Обновляем только разрешенные поля
            admin.Name = model.Name;
            admin.Login = model.Login;

            _context.Administrators.Update(admin);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> ChangePasswordAsync(int adminId, string currentPassword, string newPassword)
        {
            var admin = await _context.Administrators.FindAsync(adminId);
            if (admin == null)
                return new ServiceResult { Success = false, ErrorMessage = "Администратор не найден" };

            // Проверяем текущий пароль
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                currentPassword);

            if (verificationResult == PasswordVerificationResult.Failed)
                return new ServiceResult { Success = false, ErrorMessage = "Текущий пароль указан неверно" };

            // Хешируем новый пароль
            admin.PasswordHash = _passwordHasher.HashPassword(admin, newPassword);

            _context.Administrators.Update(admin);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }

        public async Task<(List<Consumer> Consumers, List<Transporter> Transporters)> GetAllUsersAsync()
        {
            var consumers = await _context.Consumers.ToListAsync();
            var transporters = await _context.Transporters.ToListAsync();

            return (consumers, transporters);
        }

        public async Task<List<Models.Route>> GetAllRoutesAsync()
        {
            return await _context.Routes
                .Include(r => r.Ship)
                .Include(r => r.Transporter)
                .ToListAsync();
        }
    }
}