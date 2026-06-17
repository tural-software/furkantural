using FurkanTural_Blog;
using FurkanTural_Blog.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// API erişilemez olduğunda sayfaların asılı kalmaması için kısa bağlantı zaman aşımı.
static SocketsHttpHandler FastFailHandler() => new()
{
    ConnectTimeout = TimeSpan.FromSeconds(2),
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
};

// API entegrasyonu — uygulama token servisi
builder.Services.AddHttpClient("AppTokenClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler);

builder.Services.AddSingleton<IAppTokenService, AppTokenService>();
builder.Services.AddTransient<DefaultTokenHandler>();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler)
  .AddHttpMessageHandler<DefaultTokenHandler>();

// Blog services
builder.Services.AddScoped<IBlogApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<BlogApiService>>();
    return new BlogApiService(client, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
