using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
using WaterProj.Models.WaterProj.Models;
using WaterProj.Services;
using Xunit;

namespace WaterProj.Tests.Services
{
    public class FeedbackServiceTests
    {
        private Mock<ApplicationDbContext> CreateMockDbContext()
        {
            var options = new DbContextOptions<ApplicationDbContext>();
            return new Mock<ApplicationDbContext>(options);
        }

        #region CheckIsFeedbackExist Tests

        [Fact]
        public async Task CheckIsFeedbackExist_OrderNotFound_ReturnsFalseResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            mockOrderService.Setup(s => s.GetOrderbyId(It.IsAny<int>()))
                .ReturnsAsync((Order)null);

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.CheckIsFeedbackExist(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Заказ не найден.", result.ErrorMessage);
        }

        [Fact]
        public async Task CheckIsFeedbackExist_FeedbackExists_ReturnsTrueResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            mockOrderService.Setup(s => s.GetOrderbyId(It.IsAny<int>()))
                .ReturnsAsync(new Order { OrderId = 1, IsFeedback = true });

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.CheckIsFeedbackExist(1);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task CheckIsFeedbackExist_FeedbackDoesNotExist_ReturnsFalseResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            mockOrderService.Setup(s => s.GetOrderbyId(It.IsAny<int>()))
                .ReturnsAsync(new Order { OrderId = 1, IsFeedback = false });

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.CheckIsFeedbackExist(1);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.ErrorMessage);
        }

        #endregion

        #region GetAvailableRouteAdvantages Tests

        [Fact]
        public async Task GetAvailableRouteAdvantages_ReturnsAllAdvantages()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var advantages = new List<Advantage>
            {
                new Advantage { AdvantageId = 1, Name = "Пунктуальность" },
                new Advantage { AdvantageId = 2, Name = "Чистота" },
                new Advantage { AdvantageId = 3, Name = "Комфорт" }
            };

            var mockAdvantagesDbSet = new Mock<DbSet<Advantage>>();
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.Provider)
                .Returns(advantages.AsQueryable().Provider);
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.Expression)
                .Returns(advantages.AsQueryable().Expression);
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.ElementType)
                .Returns(advantages.AsQueryable().ElementType);
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => advantages.AsQueryable().GetEnumerator());

            mockAdvantagesDbSet.Setup(m => m.ToListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(advantages);

            mockDbContext.Setup(c => c.Advantages).Returns(mockAdvantagesDbSet.Object);

            var service = new FeedbackService(mockDbContext.Object, Mock.Of<IOrderService>());

            // Act
            var result = await service.GetAvailableRouteAdvantages();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Пунктуальность", result[0].Name);
            Assert.Equal("Чистота", result[1].Name);
            Assert.Equal("Комфорт", result[2].Name);
        }

        [Fact]
        public async Task GetAvailableRouteAdvantages_ReturnsEmptyList_WhenNoAdvantages()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var advantages = new List<Advantage>();

            var mockAdvantagesDbSet = new Mock<DbSet<Advantage>>();
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.Provider)
                .Returns(advantages.AsQueryable().Provider);
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.Expression)
                .Returns(advantages.AsQueryable().Expression);
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.ElementType)
                .Returns(advantages.AsQueryable().ElementType);
            mockAdvantagesDbSet.As<IQueryable<Advantage>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => advantages.AsQueryable().GetEnumerator());

            mockAdvantagesDbSet.Setup(m => m.ToListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(advantages);

            mockDbContext.Setup(c => c.Advantages).Returns(mockAdvantagesDbSet.Object);

            var service = new FeedbackService(mockDbContext.Object, Mock.Of<IOrderService>());

            // Act
            var result = await service.GetAvailableRouteAdvantages();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region SaveFeedback Tests

        [Fact]
        public async Task SaveFeedback_SavesRating_WithoutPhotosAndAdvantages()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            var model = new FeedbackDto
            {
                OrderId = 1,
                RouteId = 10,
                TransporterId = 5,
                Stars = 4,
                Comment = "Хороший маршрут",
                PositiveComments = "Пунктуальность",
                NegativeComments = "Нет питания"
            };

            var mockRatingsDbSet = SetupMockDbSet<RouteRating>(new List<RouteRating>());
            mockDbContext.Setup(c => c.RouteRatings).Returns(mockRatingsDbSet.Object);

            var mockRatingAdvantagesDbSet = SetupMockDbSet<RouteRatingAdvantage>(new List<RouteRatingAdvantage>());
            mockDbContext.Setup(c => c.RouteRatingAdvantages).Returns(mockRatingAdvantagesDbSet.Object);

            var mockRoutesDbSet = SetupMockDbSet<Route>(new List<Route>
            {
                new Route { RouteId = 10, Rating = 0 }
            });
            mockDbContext.Setup(c => c.Routes).Returns(mockRoutesDbSet.Object);
            mockDbContext.Setup(c => c.Routes.FindAsync(10)).ReturnsAsync(new Route { RouteId = 10, Rating = 0 });

            var mockTransportersDbSet = SetupMockDbSet<Transporter>(new List<Transporter>
            {
                new Transporter { TransporterId = 5, Rating = 0 }
            });
            mockDbContext.Setup(c => c.Transporters).Returns(mockTransportersDbSet.Object);
            mockDbContext.Setup(c => c.Transporters.FindAsync(5)).ReturnsAsync(new Transporter { TransporterId = 5, Rating = 0 });

            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderService.Setup(s => s.GetOrderbyId(1))
                .ReturnsAsync(new Order { OrderId = 1, IsFeedback = false });

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.SaveFeedback(model, null, 1);

            // Assert
            Assert.True(result.Success);

            mockDbContext.Verify(c => c.RouteRatings.Add(It.Is<RouteRating>(r =>
                r.RouteId == 10 &&
                r.ConsumerId == 1 &&
                r.Stars == 4 &&
                r.Comment == "Хороший маршрут" &&
                r.PositiveComments == "Пунктуальность" &&
                r.NegativeComments == "Нет питания")), Times.Once);

            mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SaveFeedback_SavesRating_WithAdvantages()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            var model = new FeedbackDto
            {
                OrderId = 1,
                RouteId = 10,
                TransporterId = 5,
                Stars = 5,
                Comment = "Отличный маршрут",
                SelectedAdvantages = new List<int> { 1, 3 }
            };

            // Настройка для RouteRatings
            var ratings = new List<RouteRating>();
            var mockRatingsDbSet = SetupMockDbSet<RouteRating>(ratings);
            mockDbContext.Setup(c => c.RouteRatings).Returns(mockRatingsDbSet.Object);

            // Настройка для RouteRatingAdvantages
            var ratingAdvantages = new List<RouteRatingAdvantage>();
            var mockRatingAdvantagesDbSet = SetupMockDbSet<RouteRatingAdvantage>(ratingAdvantages);
            mockDbContext.Setup(c => c.RouteRatingAdvantages).Returns(mockRatingAdvantagesDbSet.Object);

            // Настройка для маршрута и транспортёра
            mockDbContext.Setup(c => c.Routes.FindAsync(10)).ReturnsAsync(new Route { RouteId = 10, Rating = 0 });
            mockDbContext.Setup(c => c.Transporters.FindAsync(5)).ReturnsAsync(new Transporter { TransporterId = 5, Rating = 0 });

            // Моки для SaveChanges (для добавления RouteRating в контекст)
            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(ct =>
                {
                    if (ratings.Count > 0 && ratings[0].RouteRatingId == 0)
                    {
                        ratings[0].RouteRatingId = 1; // Присваиваем ID при первом сохранении
                    }
                })
                .ReturnsAsync(1);

            mockOrderService.Setup(s => s.GetOrderbyId(1))
                .ReturnsAsync(new Order { OrderId = 1, IsFeedback = false });

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.SaveFeedback(model, null, 1);

            // Assert
            Assert.True(result.Success);

            // Проверяем, что рейтинг был добавлен
            mockDbContext.Verify(c => c.RouteRatings.Add(It.IsAny<RouteRating>()), Times.Once);

            // Проверяем, что преимущества были добавлены
            mockDbContext.Verify(c => c.RouteRatingAdvantages.Add(It.Is<RouteRatingAdvantage>(a =>
                a.AdvantageId == 1)), Times.Once);
            mockDbContext.Verify(c => c.RouteRatingAdvantages.Add(It.Is<RouteRatingAdvantage>(a =>
                a.AdvantageId == 3)), Times.Once);
        }

        [Fact]
        public async Task SaveFeedback_HandlesException_ReturnsErrorResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            var model = new FeedbackDto
            {
                OrderId = 1,
                RouteId = 10,
                Stars = 4
            };

            // Настройка выброса исключения при добавлении рейтинга
            mockDbContext.Setup(c => c.RouteRatings.Add(It.IsAny<RouteRating>()))
                .Throws(new Exception("Тестовое исключение"));

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.SaveFeedback(model, null, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Тестовое исключение", result.ErrorMessage);
        }

        [Fact]
        public async Task SaveFeedback_UpdatesOrderIsFeedback()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var mockOrderService = new Mock<IOrderService>();

            var model = new FeedbackDto
            {
                OrderId = 1,
                RouteId = 10,
                TransporterId = 5,
                Stars = 4
            };

            var order = new Order { OrderId = 1, IsFeedback = false };

            // Настройка всех необходимых DbSet
            var mockRatingsDbSet = SetupMockDbSet<RouteRating>(new List<RouteRating>());
            mockDbContext.Setup(c => c.RouteRatings).Returns(mockRatingsDbSet.Object);

            mockDbContext.Setup(c => c.Routes.FindAsync(10)).ReturnsAsync(new Route { RouteId = 10, Rating = 0 });
            mockDbContext.Setup(c => c.Transporters.FindAsync(5)).ReturnsAsync(new Transporter { TransporterId = 5, Rating = 0 });

            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderService.Setup(s => s.GetOrderbyId(1)).ReturnsAsync(order);

            var service = new FeedbackService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.SaveFeedback(model, null, 1);

            // Assert
            Assert.True(result.Success);
            Assert.True(order.IsFeedback); // Проверяем, что флаг IsFeedback был установлен
        }

        #endregion

        #region UpdateRouteAverageRating Test

        [Fact]
        public async Task UpdateRouteAverageRating_CalculatesCorrectAverage()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем рейтинги для маршрута
            var ratings = new List<RouteRating>
            {
                new RouteRating { RouteId = 10, Stars = 5 },
                new RouteRating { RouteId = 10, Stars = 3 },
                new RouteRating { RouteId = 10, Stars = 4 },
                new RouteRating { RouteId = 20, Stars = 5 } // Другой маршрут
            };

            // Настраиваем DbSet для RouteRatings
            var mockRatingsDbSet = new Mock<DbSet<RouteRating>>();
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.Provider)
                .Returns(ratings.AsQueryable().Provider);
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.Expression)
                .Returns(ratings.AsQueryable().Expression);
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.ElementType)
                .Returns(ratings.AsQueryable().ElementType);
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => ratings.AsQueryable().GetEnumerator());

            mockDbContext.Setup(c => c.RouteRatings).Returns(mockRatingsDbSet.Object);

            // Настраиваем маршрут
            var route = new Route { RouteId = 10, Rating = 0 };
            mockDbContext.Setup(c => c.Routes.FindAsync(10)).ReturnsAsync(route);

            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var service = new FeedbackService(mockDbContext.Object, Mock.Of<IOrderService>());

            // Act - вызываем приватный метод через рефлексию
            var methodInfo = typeof(FeedbackService).GetMethod("UpdateRouteAverageRating",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)methodInfo.Invoke(service, new object[] { 10 });

            // Assert - проверяем, что рейтинг обновлен правильно (среднее из 5, 3, 4 = 4.0)
            Assert.Equal(4.0f, route.Rating);
        }

        #endregion

        #region UpdateTransporterRating Test

        [Fact]
        public async Task UpdateTransporterRating_CalculatesCorrectAverage()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем маршруты для транспортера
            var routes = new List<Route>
            {
                new Route { RouteId = 1, TransporterId = 5 },
                new Route { RouteId = 2, TransporterId = 5 },
                new Route { RouteId = 3, TransporterId = 6 } // Другой транспортер
            };

            // Настраиваем DbSet для Routes
            var mockRoutesDbSet = new Mock<DbSet<Route>>();
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.Provider)
                .Returns(routes.AsQueryable().Provider);
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.Expression)
                .Returns(routes.AsQueryable().Expression);
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.ElementType)
                .Returns(routes.AsQueryable().ElementType);
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => routes.AsQueryable().GetEnumerator());

            mockDbContext.Setup(c => c.Routes).Returns(mockRoutesDbSet.Object);

            // Создаем рейтинги для маршрутов
            var ratings = new List<RouteRating>
            {
                new RouteRating { RouteRatingId = 1, RouteId = 1, Stars = 5 },
                new RouteRating { RouteRatingId = 2, RouteId = 1, Stars = 5 },
                new RouteRating { RouteRatingId = 3, RouteId = 2, Stars = 3 },
                new RouteRating { RouteRatingId = 4, RouteId = 3, Stars = 1 } // Для другого транспортера
            };

            // Настраиваем DbSet для RouteRatings
            var mockRatingsDbSet = new Mock<DbSet<RouteRating>>();
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.Provider)
                .Returns(ratings.AsQueryable().Provider);
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.Expression)
                .Returns(ratings.AsQueryable().Expression);
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.ElementType)
                .Returns(ratings.AsQueryable().ElementType);
            mockRatingsDbSet.As<IQueryable<RouteRating>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => ratings.AsQueryable().GetEnumerator());

            mockDbContext.Setup(c => c.RouteRatings).Returns(mockRatingsDbSet.Object);

            // Настраиваем транспортер
            var transporter = new Transporter { TransporterId = 5, Rating = 0 };
            mockDbContext.Setup(c => c.Transporters.FindAsync(5)).ReturnsAsync(transporter);

            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var service = new FeedbackService(mockDbContext.Object, Mock.Of<IOrderService>());

            // Act
            await service.UpdateTransporterRating(5);

            // Assert - проверяем, что рейтинг обновлен правильно (среднее из 5, 5, 3 = 4.3, округленное до 4.3)
            Assert.Equal(4.3f, transporter.Rating);
        }

        [Fact]
        public async Task UpdateTransporterRating_NoRoutes_DoesNothing()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Пустой список маршрутов
            var routes = new List<Route>();

            var mockRoutesDbSet = new Mock<DbSet<Route>>();
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.Provider)
                .Returns(routes.AsQueryable().Provider);
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.Expression)
                .Returns(routes.AsQueryable().Expression);
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.ElementType)
                .Returns(routes.AsQueryable().ElementType);
            mockRoutesDbSet.As<IQueryable<Route>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => routes.AsQueryable().GetEnumerator());

            mockDbContext.Setup(c => c.Routes).Returns(mockRoutesDbSet.Object);

            var service = new FeedbackService(mockDbContext.Object, Mock.Of<IOrderService>());

            // Act
            await service.UpdateTransporterRating(5);

            // Assert - проверяем, что метод FindAsync не вызывался
            mockDbContext.Verify(c => c.Transporters.FindAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        // Вспомогательный метод для настройки мока DbSet
        private Mock<DbSet<T>> SetupMockDbSet<T>(List<T> data) where T : class
        {
            var mockDbSet = new Mock<DbSet<T>>();

            mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.AsQueryable().Provider);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.AsQueryable().Expression);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.AsQueryable().GetEnumerator());

            mockDbSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(item => data.Add(item));

            return mockDbSet;
        }
    }
}