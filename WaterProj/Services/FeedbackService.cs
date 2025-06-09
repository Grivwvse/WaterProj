using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
using WaterProj.Models.WaterProj.Models;

namespace WaterProj.Services
{
    public class FeedbackService : IFeedbackService
    {

        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        public FeedbackService(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        /// <summary>
        /// Проввряет, существует ли отзыв на заказ.
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public async Task<ServiceResult> CheckIsFeedbackExist(int orderID)
        {
            var order = await _orderService.GetOrderbyId(orderID);
            if (order == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "Заказ не найден."
                };
            }

            if (order.IsFeedback)
            {
                return new ServiceResult
                {
                    Success = true
                };
            }

            return new ServiceResult
            {
                Success = false
            };
        }

        public async Task<List<Advantage>> GetAvailableRouteAdvantages()
        {
            return await _context.Set<Advantage>().ToListAsync();
        }

        public async Task<ServiceResult> SaveFeedback( FeedbackDto model, IFormFileCollection photos, int consumerId)
        {
            try
            {
                var routeRating = new RouteRating
                {
                    RouteId = model.RouteId,
                    ConsumerId = consumerId,
                    Stars = model.Stars,
                    CreatedAt = DateTime.UtcNow,
                    Comment = model.Comment,
                    PositiveComments = model.PositiveComments,
                    NegativeComments = model.NegativeComments,
                };

                // Добавляем рейтинг в базу
                _context.RouteRatings.Add(routeRating);
                await _context.SaveChangesAsync();

                // Если выбраны преимущества, сохраняем их
                if (model.SelectedAdvantages != null && model.SelectedAdvantages.Any())
                {
                    foreach (var advantageId in model.SelectedAdvantages)
                    {
                        var ratingAdvantage = new RouteRatingAdvantage
                        {
                            RouteRatingId = routeRating.RouteRatingId,
                            AdvantageId = advantageId
                        };
                        _context.RouteRatingAdvantages.Add(ratingAdvantage);
                    }
                    await _context.SaveChangesAsync();
                }

                // Если загружены фотографии, сохраняем их
                if (photos != null && photos.Count > 0)
                {
                    var routeFolder = Path.Combine("wwwroot", "images", "ratings", routeRating.RouteRatingId.ToString());
                    if (!Directory.Exists(routeFolder))
                    {
                        Directory.CreateDirectory(routeFolder);
                    }

                    foreach (var file in photos)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(routeFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var ReviewImage = new ReviewImage
                            {
                                RouteRatingId = routeRating.RouteRatingId,
                                ImagePath = $"/images/ratings/{routeRating.RouteRatingId}/{fileName}"

                            };

                            _context.ReviewImages.Add(ReviewImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Обновляем статус заказа (отмечаем, что отзыв оставлен)
                var order = await _orderService.GetOrderbyId(model.OrderId);
                if (order != null)
                {
                    order.IsFeedback = true;
                    await _context.SaveChangesAsync();
                }

                // Обновляем средний рейтинг маршрута
                await UpdateRouteAverageRating(model.RouteId);
                await UpdateTransporterRating(model.TransporterId);

                return new ServiceResult
                {
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
           
        }


        /// <summary>
        /// Обновляет средний рейтинг маршрута на основе всех его отзывов
        /// </summary>
        /// <param name="routeId"></param>
        /// <returns></returns>
        private async Task UpdateRouteAverageRating(int routeId)
        {
            // Получаем все рейтинги маршрута
            var ratings = await _context.RouteRatings
                .Where(rr => rr.RouteId == routeId)
                .Select(rr => rr.Stars)
                .ToListAsync();

            // Если есть рейтинги, вычисляем средний и обновляем маршрут
            if (ratings.Any())
            {
                var averageRating = ratings.Average();
                var route = await _context.Routes.FindAsync(routeId);
                if (route != null)
                {
                    route.Rating = (float)Math.Round(averageRating, 1);
                    await _context.SaveChangesAsync();
                }
            }
        }


        /// <summary>
        /// Обновляет рейтинг транспортера на основе рейтингов его маршрутов
        /// </summary>
        /// <param name="transporterId">ID транспортера</param>
        /// <returns>Task</returns>
        public async Task UpdateTransporterRating(int transporterId)
        {
            // Получаем все маршруты
            var routes = await _context.Routes
                .Where(r => r.TransporterId == transporterId)
                .Select(r => r.RouteId)
                .ToListAsync();

            if (!routes.Any())
            {
                return; // У транспортера нет маршрутов
            }

            // Получаем все рейтинги маршрутов этого транспортера
            var routeRatings = await _context.RouteRatings
                .Where(rr => routes.Contains(rr.RouteId))
                .Select(rr => rr.Stars)
                .ToListAsync();

            if (routeRatings.Any())
            {
                // Вычисляем средний рейтинг
                float averageRating = (float)Math.Round(routeRatings.Average(),1);

                // Обновляем рейтинг транспортера
                var transporter = await _context.Transporters.FindAsync(transporterId);
                if (transporter != null)
                {
                    transporter.Rating = averageRating;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
