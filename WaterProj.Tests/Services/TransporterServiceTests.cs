using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using WaterProj.DB;
using WaterProj.DTOs;
using WaterProj.Models;
using WaterProj.Services;
using WaterProj.Tests.Services;


namespace WaterProj.Tests.Services;
public class TransporterServiceTests
{
    private Mock<ApplicationDbContext> CreateMockDbContext()
    {
        var options = new DbContextOptions<ApplicationDbContext>();
        return new Mock<ApplicationDbContext>(options);
    }

    // Вспомогательный метод для создания мока DbSet
    private static Mock<DbSet<T>> MockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());
        return mockSet;
    }

    [Fact]
    public async Task GetByIdAsync_TransporterExists_ReturnsTransporter()
    {
        var mockDbContext = CreateMockDbContext();
        var transporter = new Transporter { TransporterId = 1, Name = "Test" };
        var mockSet = new Mock<DbSet<Transporter>>();
        mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(transporter);
        mockDbContext.Setup(m => m.Set<Transporter>()).Returns(mockSet.Object);

        var service = new TransporterService(mockDbContext.Object, Mock.Of<IOrderService>());
        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.TransporterId);
    }

    [Fact]
    public async Task GetByIdAsync_TransporterNotFound_ReturnsNull()
    {
        var mockDbContext = CreateMockDbContext();
        var mockSet = new Mock<DbSet<Transporter>>();
        mockSet.Setup(m => m.FindAsync(99)).ReturnsAsync((Transporter)null);
        mockDbContext.Setup(m => m.Set<Transporter>()).Returns(mockSet.Object);

        var service = new TransporterService(mockDbContext.Object, Mock.Of<IOrderService>());
        var result = await service.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTransporterAsync_TransporterExists_UpdatesAndReturnsSuccess()
    {
        var mockDbContext = CreateMockDbContext();
        var transporter = new Transporter { TransporterId = 1, Name = "Old", Email = "old@mail.com", Phone = "123", Description = "desc" };
        var mockSet = new Mock<DbSet<Transporter>>();
        mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(transporter);
        mockDbContext.Setup(m => m.Set<Transporter>()).Returns(mockSet.Object);

        // Мокируем Update на DbContext
        mockDbContext.Setup(m => m.Update(It.IsAny<Transporter>()));

        // Мокируем SaveChangesAsync
        mockDbContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TransporterService(mockDbContext.Object, Mock.Of<IOrderService>());
        var updated = new Transporter { Name = "New", Email = "new@mail.com", Phone = "456", Description = "newdesc" };

        var result = await service.UpdateTransporterAsync(1, updated);

        Assert.True(result.Success);
        Assert.Equal("New", transporter.Name);
        Assert.Equal("new@mail.com", transporter.Email);
    }

    [Fact]
    public async Task UpdateTransporterAsync_TransporterNotFound_ReturnsFailure()
    {
        var mockDbContext = CreateMockDbContext();
        var mockSet = new Mock<DbSet<Transporter>>();
        mockSet.Setup(m => m.FindAsync(99)).ReturnsAsync((Transporter)null);
        mockDbContext.Setup(m => m.Set<Transporter>()).Returns(mockSet.Object);

        var service = new TransporterService(mockDbContext.Object, Mock.Of<IOrderService>());
        var result = await service.UpdateTransporterAsync(99, new Transporter());

        Assert.False(result.Success);
        Assert.Equal("Пользователь не найден.", result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordAsync_SuccessfulChange_ReturnsSuccess()
    {
        // Arrange
        var mockDbContext = CreateMockDbContext();

        // Создаем реальный хеш для тестирования
        var passwordHasher = new PasswordHasher<Transporter>();
        var transporter = new Transporter
        {
            TransporterId = 1,
            Name = "Test"
        };
        // Генерируем правильный хеш пароля
        transporter.PasswordHash = passwordHasher.HashPassword(transporter, "OldPassword");

        var mockSet = new Mock<DbSet<Transporter>>();
        mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(transporter);
        mockDbContext.Setup(m => m.Set<Transporter>()).Returns(mockSet.Object);

        // Мокируем Update и SaveChanges
        mockDbContext.Setup(m => m.Update(It.IsAny<Transporter>()));
        mockDbContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new TransporterService(mockDbContext.Object, Mock.Of<IOrderService>());

        // Act
        var result = await service.ChangePasswordAsync(1, "OldPassword", "NewPassword");

        // Assert
        Assert.True(result.Success);

        // Проверяем, что пароль действительно изменился
        var newPasswordCheck = new PasswordHasher<Transporter>()
            .VerifyHashedPassword(transporter, transporter.PasswordHash, "NewPassword");
        Assert.Equal(PasswordVerificationResult.Success, newPasswordCheck);
    }
  
}
