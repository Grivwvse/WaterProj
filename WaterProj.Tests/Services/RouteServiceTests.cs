using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class RouteServiceTests
    {
        private Mock<ApplicationDbContext> CreateMockDbContext()
        {
            // Создаем опции для контекста
            var options = new DbContextOptions<ApplicationDbContext>();

            // Создаем мок контекста
            var mockContext = new Mock<ApplicationDbContext>(options);

            // Возвращаем созданный мок
            return mockContext;
        }


        // Тестирование получения маршрутов по ID
        #region GetRoutesByIdsAsync Tests
        [Fact]
        public async Task GetRoutesByIdsAsync_RoutesExist_ReturnsRoutes()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var routes = new List<Models.Route>
            {
                new Models.Route { RouteId = 1, Name = "Маршрут 1" },
                new Models.Route { RouteId = 2, Name = "Маршрут 2" },
                new Models.Route { RouteId = 3, Name = "Маршрут 3" }
            };

            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(routes);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRoutesByIdsAsync(new List<int> { 1, 2 });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.RouteId == 1);
            Assert.Contains(result, r => r.RouteId == 2);
            Assert.DoesNotContain(result, r => r.RouteId == 3);
        }

        [Fact]
        public async Task GetRoutesByIdsAsync_NoMatchingRoutes_ReturnsEmptyList()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var routes = new List<Models.Route>
            {
                new Models.Route { RouteId = 1, Name = "Маршрут 1" },
                new Models.Route { RouteId = 2, Name = "Маршрут 2" }
            };

            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(routes);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRoutesByIdsAsync(new List<int> { 3, 4 });

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        #endregion

        // Тестирование получения маршрутов по кораблю
        #region GetRoutesByShip Tests
        [Fact]
        public async Task GetRoutesByShip_RoutesExist_ReturnsRoutes()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var routes = new List<Models.Route>
            {
                new Models.Route { RouteId = 1, Name = "Маршрут 1", ShipId = 1 },
                new Models.Route { RouteId = 2, Name = "Маршрут 2", ShipId = 1 },
                new Models.Route { RouteId = 3, Name = "Маршрут 3", ShipId = 2 }
            };

            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(routes);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRoutesByShip(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.RouteId == 1);
            Assert.Contains(result, r => r.RouteId == 2);
            Assert.DoesNotContain(result, r => r.RouteId == 3);
        }

        [Fact]
        public async Task GetRoutesByShip_NoRoutesForShip_ReturnsEmptyList()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var routes = new List<Models.Route>
            {
                new Models.Route { RouteId = 1, Name = "Маршрут 1", ShipId = 1 },
                new Models.Route { RouteId = 2, Name = "Маршрут 2", ShipId = 1 }
            };

            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(routes);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRoutesByShip(99);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        #endregion

        // Тестирование получения всех остановок для активных маршрутов
        #region GetAllStopsAsync Tests
        [Fact]
        public async Task GetAllStopsAsync_ActiveRoutes_ReturnsStops()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем активные маршруты
            var routes = new List<Models.Route>
            {
                new Models.Route { RouteId = 1, IsActive = true, IsBlocked = false },
                new Models.Route { RouteId = 2, IsActive = true, IsBlocked = false },
                new Models.Route { RouteId = 3, IsActive = false, IsBlocked = false } // Неактивный маршрут
            };

            // Создаем связи маршрутов с остановками
            var routeStops = new List<RouteStop>
            {
                new RouteStop { RouteStopId = 1, RouteId = 1, StopId = 101 },
                new RouteStop { RouteStopId = 2, RouteId = 1, StopId = 102 },
                new RouteStop { RouteStopId = 3, RouteId = 2, StopId = 102 },
                new RouteStop { RouteStopId = 4, RouteId = 2, StopId = 103 },
                new RouteStop { RouteStopId = 5, RouteId = 3, StopId = 104 } // Остановка неактивного маршрута
            };

            // Создаем сами остановки
            var stops = new List<Stop>
            {
                new Stop { StopId = 101, Name = "Остановка 1" },
                new Stop { StopId = 102, Name = "Остановка 2" },
                new Stop { StopId = 103, Name = "Остановка 3" },
                new Stop { StopId = 104, Name = "Остановка 4" } // Эта остановка не должна быть возвращена
            };

            // Настраиваем моки используя Set<T>() вместо свойств
            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(routes);
            mockDbContext.Setup(m => m.Set<RouteStop>()).ReturnsDbSet(routeStops);
            mockDbContext.Setup(m => m.Set<Stop>()).ReturnsDbSet(stops);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetAllStopsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Должны быть возвращены только остановки активных маршрутов
            Assert.Contains(result, s => s.StopId == 101);
            Assert.Contains(result, s => s.StopId == 102);
            Assert.Contains(result, s => s.StopId == 103);
            Assert.DoesNotContain(result, s => s.StopId == 104); // Эта остановка только от неактивного маршрута
        }
        #endregion

        // Тестирование получения доступных остановок для маршрутов
        #region GetAvailableStopsForRoutes Tests
        [Fact]
        public async Task GetAvailableStopsForRoutes_ReturnsCorrectStops()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем тестовые данные для остановок маршрутов
            var routeStops = new List<RouteStop>
            {
                // Маршрут 1: 1 -> 2 -> 3
                new RouteStop { RouteStopId = 1, RouteId = 1, StopId = 1, StopOrder = 1 },
                new RouteStop { RouteStopId = 2, RouteId = 1, StopId = 2, StopOrder = 2 },
                new RouteStop { RouteStopId = 3, RouteId = 1, StopId = 3, StopOrder = 3 },
                
                // Маршрут 2: 1 -> 4 -> 5
                new RouteStop { RouteStopId = 4, RouteId = 2, StopId = 1, StopOrder = 1 },
                new RouteStop { RouteStopId = 5, RouteId = 2, StopId = 4, StopOrder = 2 },
                new RouteStop { RouteStopId = 6, RouteId = 2, StopId = 5, StopOrder = 3 },
            };

            // Создаем реальные объекты остановок
            var stops = new List<Stop>
            {
                new Stop { StopId = 1, Name = "Начальная остановка" },
                new Stop { StopId = 2, Name = "Остановка 2" },
                new Stop { StopId = 3, Name = "Остановка 3" },
                new Stop { StopId = 4, Name = "Остановка 4" },
                new Stop { StopId = 5, Name = "Остановка 5" },
            };

            // Настраиваем моки используя Set<T>() вместо свойств
            mockDbContext.Setup(m => m.Set<RouteStop>()).ReturnsDbSet(routeStops);
            mockDbContext.Setup(m => m.Set<Stop>()).ReturnsDbSet(stops);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetAvailableStopsForRoutes(1, new List<int> { 1, 2 });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count); // Должны быть возвращены остановки 2, 3, 4, 5
            Assert.Contains(result, s => s.StopId == 2);
            Assert.Contains(result, s => s.StopId == 3);
            Assert.Contains(result, s => s.StopId == 4);
            Assert.Contains(result, s => s.StopId == 5);
            Assert.DoesNotContain(result, s => s.StopId == 1); // Начальная остановка не должна возвращаться
        }
        #endregion

        // Тестирование поиска маршрутов по имени
        #region SearchRoutesAsync Tests
        [Fact]
        public async Task SearchRoutesAsync_ReturnsFilteredRoutes()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем тестовые данные
            var routes = new List<Models.Route>
            {
                new Models.Route {
                    RouteId = 1,
                    Name = "Первый маршрут",
                    Description = "Описание первого маршрута",
                    IsActive = true,
                    IsBlocked = false,
                    TransporterId = 1,
                    Transporter = new Transporter { TransporterId = 1, Name = "Перевозчик 1" },
                    RouteDays = new List<RouteDay>()
                },
                new Models.Route {
                    RouteId = 2,
                    Name = "Второй маршрут",
                    Description = "Описание второго маршрута",
                    IsActive = true,
                    IsBlocked = false,
                    TransporterId = 2,
                    Transporter = new Transporter { TransporterId = 2, Name = "Перевозчик 2" },
                    RouteDays = new List<RouteDay>()
                },
                new Models.Route {
                    RouteId = 3,
                    Name = "Неактивный маршрут",
                    Description = "Этот маршрут не должен быть возвращен",
                    IsActive = false,
                    IsBlocked = false,
                    RouteDays = new List<RouteDay>()
                }
            };

            // Настраиваем моки используя Set<T>() 
            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(routes);

            var mockOrderService = new Mock<IOrderService>();
            mockOrderService.Setup(s => s.GetOrderCountsForRoutesAsync(It.IsAny<List<int>>(), It.IsAny<bool>()))
                .ReturnsAsync(new Dictionary<int, int> { { 1, 5 }, { 2, 3 } });

            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.SearchRoutesAsync(new RouteSearchDto { RouteName = "маршрут" });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Только два активных маршрута
            Assert.Contains(result, r => r.RouteId == 1);
            Assert.Contains(result, r => r.RouteId == 2);
            Assert.Equal(5, result.First(r => r.RouteId == 1).RouteOrderStats);
            Assert.Equal(3, result.First(r => r.RouteId == 2).RouteOrderStats);
        }
        #endregion

        #region GetRouteDetails Tests

        [Fact]
        public async Task GetRouteDetails_RouteExists_ReturnsDto()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем навигационные сущности с правильными связями
            var transporter = new Transporter { TransporterId = 1, Name = "Тестовый перевозчик" };
            var shipType = new ShipType { ShipTypesId = 1, Name = "Тип корабля" };

            // Создаем связь между удобствами и кораблем
            var shipConveniences = new List<ShipСonvenience>
    {
        new ShipСonvenience { Id = 1, ShipId = 1, СonvenienceId = 1 }
    };

            var conveniences = new List<Сonvenience>
    {
        new Сonvenience
        {
            ShipСonvenienceId = 1,
            Name = "Удобство 1",
            ShipСonveniences = shipConveniences
        }
    };

            var ship = new Ship
            {
                ShipId = 1,
                Name = "Тестовый корабль",
                ShipType = shipType,
                ShipTypeId = 1,
                ShipImages = new List<ShipImage>(),
                ShipСonveniences = shipConveniences
            };

            var routeDays = new List<RouteDay>
    {
        new RouteDay { Id = 1, RouteId = 1, DayOfWeek = DayOfWeek.Monday },
        new RouteDay { Id = 2, RouteId = 1, DayOfWeek = DayOfWeek.Wednesday }
    };

            var route = new Models.Route
            {
                RouteId = 1,
                Name = "Тестовый маршрут",
                ShipId = 1,
                Ship = ship,
                Transporter = transporter,
                TransporterId = 1,
                RouteDays = routeDays,
                RouteStops = new List<RouteStop>()
            };

            var images = new List<Image>
    {
        new Image { ImageID = 1, EntityType = "Route", EntityId = 1, ImagePath = "/path/to/image.jpg" }
    };

            var routeRatingAdvantages = new List<RouteRatingAdvantage>();

            var routeRatings = new List<RouteRating>
    {
        new RouteRating
        {
            RouteRatingId = 1,
            RouteId = 1,
            Stars = 5,
            Consumer = new Consumer { ConsumerId = 1, Name = "Тестовый пользователь" },
            RouteRatingAdvantages = routeRatingAdvantages,
            ReviewImages = new List<ReviewImage>()
        }
    };

            var advantages = new List<Advantage>
    {
        new Advantage { AdvantageId = 1, Name = "Удобное расположение" }
    };

            // Настраиваем моки для DbSet с поддержкой async
            mockDbContext.Setup(m => m.Set<Models.Route>()).ReturnsDbSet(new List<Models.Route> { route });
            mockDbContext.Setup(m => m.Set<Ship>()).ReturnsDbSet(new List<Ship> { ship });
            mockDbContext.Setup(m => m.Set<Image>()).ReturnsDbSet(images);
            mockDbContext.Setup(m => m.Set<RouteRating>()).ReturnsDbSet(routeRatings);
            mockDbContext.Setup(m => m.Set<Сonvenience>()).ReturnsDbSet(conveniences);
            mockDbContext.Setup(m => m.Set<Advantage>()).ReturnsDbSet(advantages);
            mockDbContext.Setup(m => m.Set<ShipСonvenience>()).ReturnsDbSet(shipConveniences);
            mockDbContext.Setup(m => m.Set<RouteRatingAdvantage>()).ReturnsDbSet(routeRatingAdvantages);

            // Мок для OrderService
            var mockOrderService = new Mock<IOrderService>();
            mockOrderService.Setup(s => s.GetOrderCountForRouteAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(10);

            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRouteDetails(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Route.RouteId);
            Assert.Equal("Тестовый маршрут", result.Route.Name);
            Assert.Equal(1, result.Ship.ShipId);
            Assert.Equal("Тестовый корабль", result.Ship.Name);
            Assert.Equal("Тестовый перевозчик", result.Transporter.Name);
            Assert.Equal(2, result.RouteDays.Count);
            Assert.Contains(DayOfWeek.Monday, result.RouteDays);
            Assert.Contains(DayOfWeek.Wednesday, result.RouteDays);
            Assert.Equal(10, result.RouteOrderStats);
            Assert.Single(result.Image);
            Assert.Equal("/path/to/image.jpg", result.Image[0].ImagePath);
            Assert.Single(result.RouteRatings);
            Assert.Equal(5, result.RouteRatings[0].Stars);
        }

        #endregion




        // Тестирование изменения маршрута 
        #region EditRoute Tests
        [Fact]
        public async Task EditRoute_RouteExists_UpdatesRouteSuccessfully()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            var route = new Models.Route
            {
                RouteId = 1,
                Name = "Тестовый маршрут",
                ShipId = 1,
                Schedule = "Старое расписание",
                RouteDays = new List<RouteDay> {
            new RouteDay { Id = 1, RouteId = 1, DayOfWeek = DayOfWeek.Monday }
        }
            };

            // Мокируем FindAsync для маршрута
            var mockRoutesSet = new Mock<DbSet<Models.Route>>();
            mockRoutesSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync(route);
            mockDbContext.Setup(m => m.Set<Models.Route>()).Returns(mockRoutesSet.Object);

            // Мокируем RemoveRange для RouteDay
            var mockRouteDaysDbSet = new Mock<DbSet<RouteDay>>();
            mockRouteDaysDbSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<RouteDay>>()));
            mockDbContext.Setup(m => m.Set<RouteDay>()).Returns(mockRouteDaysDbSet.Object);

            // Мокируем SaveChangesAsync
            mockDbContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.EditRoute(
                1, // routeId
                2, // новый shipId
                "Новое расписание", // новое расписание
                new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Friday } // новые дни
            );

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, route.ShipId);
            Assert.Equal("Новое расписание", route.Schedule);
        }
        // Изменение несуществующего маршрута
        [Fact]
        public async Task EditRoute_RouteDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Настраиваем моки для ненайденного маршрута, используя Set<T>() вместо свойств
            var mockRoutesSet = new Mock<DbSet<Models.Route>>();
            mockRoutesSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((Models.Route)null);

            mockDbContext.Setup(m => m.Set<Models.Route>()).Returns(mockRoutesSet.Object);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.EditRoute(
                999, // несуществующий routeId
                1,
                "Расписание",
                new List<DayOfWeek> { DayOfWeek.Monday }
            );

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Маршрут не найден", result.ErrorMessage);
        }
        #endregion

        //
        #region GetRouteEndpointsAsync Tests
        [Fact]
        public async Task GetRouteEndpointsAsync_RouteHasStops_ReturnsEndpoints()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            var firstStop = new Stop { StopId = 1, Name = "Первая остановка" };
            var lastStop = new Stop { StopId = 3, Name = "Последняя остановка" };

            var routeStops = new List<RouteStop>
            {
                new RouteStop { RouteStopId = 1, RouteId = 1, StopId = 1, StopOrder = 1, Stop = firstStop },
                new RouteStop { RouteStopId = 2, RouteId = 1, StopId = 2, StopOrder = 2, Stop = new Stop { StopId = 2, Name = "Промежуточная остановка" } },
                new RouteStop { RouteStopId = 3, RouteId = 1, StopId = 3, StopOrder = 3, Stop = lastStop },
            };

            // Настраиваем моки
            mockDbContext.Setup(m => m.Set<RouteStop>())
                .ReturnsDbSet(routeStops);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRouteEndpointsAsync(1);

            // Assert
            Assert.NotNull(result.StartStop);
            Assert.NotNull(result.EndStop);
            Assert.Equal(1, result.StartStop.StopId);
            Assert.Equal(3, result.EndStop.StopId);
            Assert.Equal("Первая остановка", result.StartStop.Name);
            Assert.Equal("Последняя остановка", result.EndStop.Name);
        }

        [Fact]
        public async Task GetRouteEndpointsAsync_RouteHasNoStops_ReturnsNull()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            var routeStops = new List<RouteStop>();

            // Настраиваем моки
            mockDbContext.Setup(m => m.Set<RouteStop>())
                .ReturnsDbSet(routeStops);

            var mockOrderService = new Mock<IOrderService>();
            var service = new RouteService(mockDbContext.Object, mockOrderService.Object);

            // Act
            var result = await service.GetRouteEndpointsAsync(1);

            // Assert
            Assert.Null(result.StartStop);
            Assert.Null(result.EndStop);
        }
        #endregion
    }

    // Вспомогательный класс для работы с мокированными DbSet
    public static class MockDbSetExtensions
    {
        public static Mock<DbSet<T>> ReturnsDbSet<T>(List<T> entities) where T : class
        {
            var queryable = entities.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return mockSet;
        }
    }
}