using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Models.Services;
using WaterProj.Services;
using Xunit;

namespace WaterProj.Tests.Services
{
    public class OrderServiceTests
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

        // Тестирование завершения заказа 
        //Тестирование изменения статуса невалидного  заказа на завершен с выводом ошибки 

        #region CompleteOrderAsync Tests
        [Fact]
        public async Task CompleteOrderAsync_OrderNotFound_ReturnsFalseResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            // Создаем и настраиваем мок для DbSet<Order>
            var mockOrdersSet = new Mock<DbSet<Order>>();
            mockOrdersSet.Setup(m => m.FindAsync(It.IsAny<int>()))
                .ReturnsAsync((Order)null);

            mockDbContext.Setup(m => m.Set<Order>()).Returns(mockOrdersSet.Object);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.CompleteOrderAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Заказ не найден.", result.ErrorMessage);
        }

        //Тестирование изменения статуса валидного  заказа на завершен

        [Fact]
        public async Task CompleteOrderAsync_ValidOrder_ReturnsSuccessResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var order = new Order { OrderId = 1, Status = OrderStatus.Active };

            // Создаем и настраиваем мок для DbSet<Order>
            var mockOrdersSet = new Mock<DbSet<Order>>();
            mockOrdersSet.Setup(m => m.FindAsync(It.IsAny<int>()))
                .ReturnsAsync(order);

            mockDbContext.Setup(m => m.Set<Order>()).Returns(mockOrdersSet.Object);
            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.CompleteOrderAsync(1);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(OrderStatus.Completed, order.Status);
        }
        #endregion

        // Тестирование Отмены заказа 
        //Тестирование изменения статуса невалидного  заказа на отменен с выводом ошибки 
        #region CancelOrderAsync Tests
        [Fact]
        public async Task CancelOrderAsync_OrderNotFound_ReturnsFalseResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            var mockOrdersSet = new Mock<DbSet<Order>>();
            mockOrdersSet.Setup(m => m.FindAsync(It.IsAny<int>()))
                .ReturnsAsync((Order)null);

            mockDbContext.Setup(m => m.Set<Order>()).Returns(mockOrdersSet.Object);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.CancelOrderAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Заказ не найден.", result.ErrorMessage);
        }

        //Тестирование изменения статуса валидного  заказа на отменен
        [Fact]
        public async Task CancelOrderAsync_ValidOrder_ReturnsSuccessResult()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var order = new Order { OrderId = 1, Status = OrderStatus.Active };

            var mockOrdersSet = new Mock<DbSet<Order>>();
            mockOrdersSet.Setup(m => m.FindAsync(It.IsAny<int>()))
                .ReturnsAsync(order);

            mockDbContext.Setup(m => m.Set<Order>()).Returns(mockOrdersSet.Object);
            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.CancelOrderAsync(1);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(OrderStatus.Canceled, order.Status);
        }
        #endregion

        // Тестируем получение заказов по идентификатору потребителя
        // Тестирование получения заказов по идентификатору потребителя, когда есть заказы
        #region GetOrdersByConsumerId Tests
        [Fact]
        public async Task GetOrdersByConsumerId_ReturnsExpectedOrders()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, ConsumerId = 1 },
                new Order { OrderId = 2, ConsumerId = 1 },
                new Order { OrderId = 3, ConsumerId = 2 }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrdersByConsumerId(1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].OrderId);
            Assert.Equal(2, result[1].OrderId);
        }

        // Тестирование получения заказов по идентификатору потребителя, когда нет заказов
        [Fact]
        public async Task GetOrdersByConsumerId_ReturnsEmptyListWhenNoOrders()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, ConsumerId = 2 },
                new Order { OrderId = 2, ConsumerId = 3 }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrdersByConsumerId(1);

            // Assert
            Assert.Empty(result);
        }
        #endregion

        // Тестируем получение заказов по их ID 
        // Тестирование получения заказа по ID, когда заказ существует
        #region GetOrderbyId Tests
        [Fact]
        public async Task GetOrderbyId_ReturnsExpectedOrder()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, ConsumerId = 1, RouteId = 10 },
                new Order { OrderId = 2, ConsumerId = 2, RouteId = 20 }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrderbyId(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
        }
        #endregion


        // Тестирование получения заказа с деталями по ID закз
        #region GetOrderWithDetailsAsync Tests
        [Fact]
        public async Task GetOrderWithDetailsAsync_ReturnsOrderWithDetails()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();

            var transporter = new Transporter { TransporterId = 1, Name = "Тестовый перевозчик" };
            var route = new Route { RouteId = 10, TransporterId = 1, Transporter = transporter };
            var order = new Order { OrderId = 1, RouteId = 10, Route = route };

            var orders = new List<Order> { order };
            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrderWithDetailsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
            Assert.NotNull(result.Route);
            Assert.Equal(10, result.Route.RouteId);
            Assert.NotNull(result.Route.Transporter);
            Assert.Equal("Тестовый перевозчик", result.Route.Transporter.Name);
        }
        #endregion

        // Тестирование получения количества заказов для конкретного маршрута со статусом (активные)
        #region GetOrderCountForRouteAsync Tests
        [Fact]
        public async Task GetOrderCountForRouteAsync_ActiveOrders_ReturnsCorrectCount()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, RouteId = 1, Status = OrderStatus.Active },
                new Order { OrderId = 2, RouteId = 1, Status = OrderStatus.Active },
                new Order { OrderId = 3, RouteId = 1, Status = OrderStatus.Completed },
                new Order { OrderId = 4, RouteId = 2, Status = OrderStatus.Active }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrderCountForRouteAsync(1, true);

            // Assert
            Assert.Equal(2, result);
        }

        // Тестирование получения количества заказов для конкретного маршрута со статусом (завершенные)
        [Fact]
        public async Task GetOrderCountForRouteAsync_CompletedOrders_ReturnsCorrectCount()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, RouteId = 1, Status = OrderStatus.Active },
                new Order { OrderId = 2, RouteId = 1, Status = OrderStatus.Completed },
                new Order { OrderId = 3, RouteId = 1, Status = OrderStatus.Completed },
                new Order { OrderId = 4, RouteId = 2, Status = OrderStatus.Completed }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrderCountForRouteAsync(1, false);

            // Assert
            Assert.Equal(2, result);
        }
        #endregion

        // Тестирование получения количества заказов для маршрутов
        #region GetOrderCountsForRoutesAsync Tests
        [Fact]
        public async Task GetOrderCountsForRoutesAsync_ReturnsCorrectCounts()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, RouteId = 1, Status = OrderStatus.Completed },
                new Order { OrderId = 2, RouteId = 1, Status = OrderStatus.Completed },
                new Order { OrderId = 3, RouteId = 2, Status = OrderStatus.Completed },
                new Order { OrderId = 4, RouteId = 3, Status = OrderStatus.Completed },
                new Order { OrderId = 5, RouteId = 3, Status = OrderStatus.Completed }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.GetOrderCountsForRoutesAsync(new[] { 1, 2, 3, 4 });

            // Assert
            Assert.Equal(3, result.Count); // Только для маршрутов 1, 2, 3 есть заказы
            Assert.Equal(2, result[1]);    // 2 заказа для маршрута 1
            Assert.Equal(1, result[2]);    // 1 заказ для маршрута 2
            Assert.Equal(2, result[3]);    // 2 заказа для маршрута 3
            Assert.False(result.ContainsKey(4)); // Нет заказов для маршрута 4
        }
        #endregion

        #region IsRouteInActiveOrdersAsync Tests
        [Fact]
        public async Task IsRouteInActiveOrdersAsync_OrderExists_ReturnsTrue()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, RouteId = 10, ConsumerId = 5, Status = OrderStatus.Active },
                new Order { OrderId = 2, RouteId = 20, ConsumerId = 5, Status = OrderStatus.Completed }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.IsRouteInActiveOrdersAsync(10, 5);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsRouteInActiveOrdersAsync_NoActiveOrder_ReturnsFalse()
        {
            // Arrange
            var mockDbContext = CreateMockDbContext();
            var orders = new List<Order>
            {
                new Order { OrderId = 1, RouteId = 10, ConsumerId = 5, Status = OrderStatus.Completed },
                new Order { OrderId = 2, RouteId = 10, ConsumerId = 6, Status = OrderStatus.Active }
            };

            mockDbContext.Setup(m => m.Set<Order>()).ReturnsDbSet(orders);

            var service = new OrderService(mockDbContext.Object);

            // Act
            var result = await service.IsRouteInActiveOrdersAsync(10, 5);

            // Assert
            Assert.False(result);
        }
        #endregion
    }
}