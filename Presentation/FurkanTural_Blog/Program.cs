using FurkanTural_Blog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// API entegrasyonu — uygulama token servisi
builder.Services.AddHttpClient("AppTokenClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
});

builder.Services.AddSingleton<IAppTokenService, AppTokenService>();
builder.Services.AddTransient<DefaultTokenHandler>();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
}).AddHttpMessageHandler<DefaultTokenHandler>();

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
