using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WaterProj.DB;
using WaterProj.Services;

var builder = WebApplication.CreateBuilder(args);

// !!! ������������� ��������� ������������ ���������� ��������
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Authorization/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(6); // ����� ����� ����� cookie
        options.SlidingExpiration = true; //  ��������� ������ ��� ����������
        // options.LogoutPath = "/Authorization/Logout"; //  �����
    });

// �������� ������ ����������� �� ������������ (appsettings.Development.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ������������ DbContext � �������������� Npgsql
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


// ��� Production - ��������� ����� �� ���������� ���������
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

// !!! ��������� � ������� ��������� HTTP-������� ��������� ������ � ������������� �����������
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Route}/{action=FindRoutes}/{id?}");

//!!! ��������� �������������� �������� �� ���� ��� �� ���������
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
