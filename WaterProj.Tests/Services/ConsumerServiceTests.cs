using Xunit;
using Moq;
using Moq.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using WaterProj.Models;
using WaterProj.Services;
using WaterProj.DB;
using WaterProj.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace WaterProj.Tests.Services;
public class ConsumerServiceTests
{
    private Mock<ApplicationDbContext> CreateMockDbContext()
    {
        var options = new DbContextOptions<ApplicationDbContext>();
        return new Mock<ApplicationDbContext>(options);
    }

    [Fact]
    public async Task GetByIdAsync_ConsumerExists_ReturnsConsumer()
    {
        var mockDbContext = CreateMockDbContext();
        var consumer = new Consumer { ConsumerId = 1, Name = "Test" };
        var mockSet = new Mock<DbSet<Consumer>>();
        mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(consumer);
        mockDbContext.Setup(m => m.Set<Consumer>()).Returns(mockSet.Object);

        var service = new ConsumerService(mockDbContext.Object, Mock.Of<IOrderService>(), Mock.Of<IRouteService>());
        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.ConsumerId);
    }

    [Fact]
    public async Task GetByIdAsync_ConsumerNotFound_ReturnsNull()
    {
        var mockDbContext = CreateMockDbContext();
        var mockSet = new Mock<DbSet<Consumer>>();
        mockSet.Setup(m => m.FindAsync(99)).ReturnsAsync((Consumer)null);
        mockDbContext.Setup(m => m.Set<Consumer>()).Returns(mockSet.Object);

        var service = new ConsumerService(mockDbContext.Object, Mock.Of<IOrderService>(), Mock.Of<IRouteService>());
        var result = await service.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateConsumerAsync_ConsumerExists_UpdatesAndReturnsSuccess()
    {
        // Arrange
        var mockDbContext = CreateMockDbContext();
        var consumer = new Consumer { ConsumerId = 1, Name = "Old", Login = "oldlogin" };

        // Создаем мок DbSet с нужными настройками
        var mockSet = new Mock<DbSet<Consumer>>();
        mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(consumer);

        // Мокируем DbSet через Set<Consumer>()
        mockDbContext.Setup(m => m.Set<Consumer>()).Returns(mockSet.Object);

        // Мокируем метод Update на DbContext
        mockDbContext.Setup(m => m.Update(It.IsAny<Consumer>()));

        // Мокируем SaveChangesAsync
        mockDbContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new ConsumerService(mockDbContext.Object, Mock.Of<IOrderService>(), Mock.Of<IRouteService>());
        var updated = new Consumer { Name = "New", Login = "newlogin" };

        // Act
        var result = await service.UpdateConsumerAsync(1, updated);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New", consumer.Name);
        Assert.Equal("newlogin", consumer.Login);
    }

    [Fact]
    public async Task UpdateConsumerAsync_ConsumerNotFound_ReturnsFailure()
    {
        var mockDbContext = CreateMockDbContext();
        var mockSet = new Mock<DbSet<Consumer>>();
        mockSet.Setup(m => m.FindAsync(99)).ReturnsAsync((Consumer)null);
        mockDbContext.Setup(m => m.Set<Consumer>()).Returns(mockSet.Object);

        var service = new ConsumerService(mockDbContext.Object, Mock.Of<IOrderService>(), Mock.Of<IRouteService>());
        var result = await service.UpdateConsumerAsync(99, new Consumer());

        Assert.False(result.Success);
        Assert.Equal("Пользователь не найден.", result.ErrorMessage);
    }
}