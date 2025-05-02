using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;

namespace WaterProj.Services
{
    public class ShipService : IShipService
    {
        private readonly IRouteService _routeService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ShipService> _logger;

        public ShipService(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ShipService> logger, IRouteService routeService)
        {
            _routeService = routeService;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public Task<bool> AddImageToShipAsync(ShipImage shipImage)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Ship>> GetActiveShipsForTransporter(int transporterId)
        {
            return await _context.Ships
                 .Where(s => s.TransporterId == transporterId && s.Status == ShipStatus.Active)
                 .Include(s => s.ShipType) 
                 .ToListAsync();
        }

        public async Task<List<Сonvenience>> GetAllConveniences()
        {
            var conveniences = await _context.Сonveniences.ToListAsync();
            return conveniences;
        }

        public async Task<ServiceResult> CreateConvenience(Сonvenience convenience)
        {
            try
            {
                _context.Add(convenience);
                await _context.SaveChangesAsync();
                return new ServiceResult{ Success = true};
            }
            catch (Exception ex)
            {

                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "Error creating ship" + ex.Message
                };
            }
        }

        public async Task<ServiceResult> CreateShip(ShipCreateDto dto)
        {
            try
            {
                // 1. Создаем корабль на основе DTO
                var ship = new Ship
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    IMO = dto.IMO,
                    Status = dto.Status,
                    ShipTypeId = dto.SelectedShipTypeId,
                    TransporterId = dto.TransporterId,
                    ShipImages = new List<ShipImage>(),
                    ShipСonveniences = new List<ShipСonvenience>()
                };

                // 2. Сохраняем и добавляем основное изображение
                if (dto.MainImage != null)
                {
                    var mainImagePath = await SaveImageFile(dto.MainImage);
                    ship.ShipImages.Add(new ShipImage
                    {
                        ImagePath = mainImagePath,
                        Title = "Основное изображение"
                    });
                }

                // 3. Сохраняем и добавляем дополнительные изображения
                if (dto.AdditionalImages != null && dto.AdditionalImages.Count > 0)
                {
                    foreach (var image in dto.AdditionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imagePath = await SaveImageFile(image);
                            ship.ShipImages.Add(new ShipImage
                            {
                                ImagePath = imagePath,
                                Title = "Дополнительное изображение"
                            });
                        }
                    }
                }


                // 5. Сохраняем в базу данных
                _context.Ships.Add(ship);
                await _context.SaveChangesAsync();

                // Добавляем выбранные удобства
                if (dto.SelectedConvenienceIds != null && dto.SelectedConvenienceIds.Count > 0)
                {
                    foreach (var convenienceId in dto.SelectedConvenienceIds)
                    {
                        _context.ShipСonveniences.Add(new ShipСonvenience
                        {
                            ShipId = ship.ShipId,
                            СonvenienceId = convenienceId
                        });
                    }

                    // Сохраняем изменения
                    await _context.SaveChangesAsync();
                }

                return new ServiceResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании корабля");
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при создании корабля: {ex.Message}"
                };
            }
        }

        private async Task<string> SaveImageFile(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return null;

            try
            {
                // Создаем уникальное имя файла
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "ships");

                // Создаем директорию, если она не существует
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Сохраняем файл
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Возвращаем путь к файлу
                return $"/images/ships/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении изображения");
                throw;
            }
        }


        public async Task<ServiceResult> ChangeShipStatus(int shipId, int status, int transporterId)
        {
            // Проверяем корректность статуса
            if (!Enum.IsDefined(typeof(ShipStatus), status))
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "Некорректный статус."
                };
            }

            // Получаем корабль
            var ship = await _context.Ships.FirstOrDefaultAsync(s => s.ShipId == shipId && s.TransporterId == transporterId);
            if (ship == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "Корабль не найден."
                };
            }

            if (ship.TransporterId != transporterId)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "У вас нет прав для изменения статуса этого корабля."
                };
            }

            // Обновляем статус корабля
            var oldStatus = ship.Status;
            ship.Status = (ShipStatus)status;
            _context.Ships.Update(ship);

            // Проверяем, если статус меняется на неактивный или на обслуживание
            if ((ShipStatus)status == ShipStatus.Inactive || (ShipStatus)status == ShipStatus.Maintenance)
            {
                // Получаем все маршруты, связанные с этим кораблем
                var routes = await _routeService.GetRoutesByShip(shipId);

                // Деактивируем все связанные маршруты
                foreach (var route in routes)
                {
                    route.IsActive = false;
                    await _routeService.UpdateRoute(route);
                }

                // Логируем количество деактивированных маршрутов
                _logger.LogInformation($"Деактивировано {routes.Count} маршрутов для корабля ID: {shipId}");
            }

            // Сохраняем все изменения
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }
        public Task<bool> DeleteImageFromShipAsync(int imageId)
        {
            throw new NotImplementedException();
        }



        public Task<bool> DeleteShipAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<ShipImage>> GetAllImagesByShipIdAsync(int shipId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Ship>> GetAllShipsAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<ShipType>> GetAllShipTypes()
        {

            var shipTypes = await _context.ShipTypes.ToListAsync();
            return shipTypes;
        }

        public Task<Ship> GetShipByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateShipAsync(Ship ship)
        {
            throw new NotImplementedException();
        }
    }
}
