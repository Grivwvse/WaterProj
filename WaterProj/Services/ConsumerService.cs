using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class ConsumerService : IConsumerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IRouteService _routeService;

        public ConsumerService(ApplicationDbContext context, IOrderService orderService, IRouteService routeService)
        {
            _context = context;
            _orderService = orderService;
            _routeService = routeService;
        }

        public async Task<ServiceResult> ChangePasswordAsync(int consumerId, string currentPassword, string newPassword)
        {
            try
            {
                var consumer = await _context.Consumers.FindAsync(consumerId);
                if (consumer == null)
                    return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден." };

                // Создаем экземпляр PasswordHasher для проверки текущего пароля
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Consumer>();

                // Проверяем текущий пароль
                var verificationResult = passwordHasher.VerifyHashedPassword(
                    consumer,
                    consumer.PasswordHash,
                    currentPassword);

                if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                    return new ServiceResult { Success = false, ErrorMessage = "Текущий пароль указан неверно." };

                // Генерируем хеш для нового пароля
                consumer.PasswordHash = passwordHasher.HashPassword(consumer, newPassword);

                // Обновляем запись в базе данных
                _context.Consumers.Update(consumer);
                await _context.SaveChangesAsync();

                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, ErrorMessage = $"Ошибка при смене пароля: {ex.Message}" };
            }
        }

        public async Task<Consumer> GetByIdAsync(int id)
        {
            return await _context.Set<Consumer>().FindAsync(id);
        }

        public async Task<ConsumerAccontDto> GetAllAccountInfo(int id)
        {
            var consumer = await GetByIdAsync(id);
            if (consumer == null)
                throw new NotFoundException("Пользователь не найден.");

            List<Order> orders = await _orderService.GetOrdersByConsumerId(id);

            if (orders == null)
                throw new NotFoundException("Заказы не найдены.");

            
            var routeIds = orders.Select(o => o.RouteId).ToList();
            var routes = await _routeService.GetRoutesByIdsAsync(routeIds);


            var ordersRoutesDto = orders.Select(order =>
            {
                var route = routes.FirstOrDefault(r => r.RouteId == order.RouteId);
                return new OrdersRoutesDto
                {
                    Order = order,
                    Route = route
                };
            }).ToList();

            ConsumerAccontDto consumerAccountDto = new ConsumerAccontDto
            {
                OrdersRoutesDto = ordersRoutesDto,
                Consumer = consumer
            };

            return consumerAccountDto;
        }

        public async Task<ServiceResult> UpdateConsumerAsync(int userId, Consumer model)
        {
            var consumer = await _context.Set<Consumer>().FindAsync(userId);
            if (consumer == null)
                return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден." };

            consumer.Login = model.Login;
            consumer.Name = model.Name;
            consumer.Surname = model.Surname;
            consumer.Phone = model.Phone;

            _context.Set<Consumer>().Update(consumer);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }

        public async Task<bool> IsLoginExistsAsync(string login, int currentUserId)
        {
            // Проверяем, существует ли другой пользователь с таким логином
            return await _context.Consumers
                .AnyAsync(c => c.Login == login && c.ConsumerId != currentUserId);
        }

        public async Task<ServiceResult> UpdateProfileImage(int consumerId, IFormFile imageFile)
        {
            try
            {
                var consumer = await _context.Consumers.FindAsync(consumerId);
                if (consumer == null)
                {
                    return new ServiceResult { Success = false, ErrorMessage = "Пользователь не найден" };
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    // Создаем директорию для аватаров, если она не существует
                    var avatarsFolder = Path.Combine("wwwroot", "images", "avatars");
                    if (!Directory.Exists(avatarsFolder))
                    {
                        Directory.CreateDirectory(avatarsFolder);
                    }

                    // Удаляем старый файл, если он существует
                    if (!string.IsNullOrEmpty(consumer.ProfileImagePath))
                    {
                        var oldImagePath = Path.Combine("wwwroot", consumer.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Генерируем уникальное имя файла
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                    var tempFilePath = Path.Combine(avatarsFolder, "temp_" + fileName);
                    var finalFilePath = Path.Combine(avatarsFolder, fileName);

                    // Сначала сохраняем оригинальный файл во временное хранилище
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Обрабатываем изображение - делаем квадратное кадрирование
                    await CropImageToSquare(tempFilePath, finalFilePath);

                    // Удаляем временный файл
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }

                    // Обновляем путь к изображению в модели
                    consumer.ProfileImagePath = $"/images/avatars/{fileName}";

                    // Сохраняем изменения в БД
                    _context.Consumers.Update(consumer);
                    await _context.SaveChangesAsync();

                    return new ServiceResult { Success = true };
                }

                return new ServiceResult { Success = false, ErrorMessage = "Файл изображения не выбран или пуст" };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // Метод для создания квадратного изображения с центрированным кадрированием
        private async Task CropImageToSquare(string inputPath, string outputPath, int size = 300)
        {
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(inputPath))
            {
                // Определяем размеры для кадрирования (берем минимальную сторону)
                int minDimension = Math.Min(image.Width, image.Height);

                // Вычисляем отступы для центрирования
                int xOffset = (image.Width - minDimension) / 2;
                int yOffset = (image.Height - minDimension) / 2;

                // Кадрируем до квадрата
                image.Mutate(i => i
                    .Crop(new Rectangle(xOffset, yOffset, minDimension, minDimension))
                    .Resize(size, size)); // Изменяем размер до нужного

                // Сохраняем результат
                await image.SaveAsync(outputPath);
            }
        }

    }


}
