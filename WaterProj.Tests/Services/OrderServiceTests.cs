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
    }
}