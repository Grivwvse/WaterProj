using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Authorization/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // задаём время жизни cookie
        options.SlidingExpiration = true; //  продление сессии при активности
        // options.LogoutPath = "/Authorization/Logout"; //  выход
    });

// Получаем строку подключения из конфигурации (appsettings.Development.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем DbContext с использованием Npgsql
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IConsumerService, ConsumerService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ITransporterService, TransporterService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IShipService, ShipService>();
builder.Services.AddScoped<IAdministratorService, AdministratorService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();


// Для Production - загружаем ключи из переменных окружения
if (builder.Environment.IsProduction())
{
    var yandexApiKey = Environment.GetEnvironmentVariable("YANDEX_MAPS_API_KEY");
    if (!string.IsNullOrEmpty(yandexApiKey))
    {
        builder.Configuration["ApiKeys:YandexMaps"] = yandexApiKey;
    }
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Route}/{action=FindRoutes}/{id?}");

app.Run();
