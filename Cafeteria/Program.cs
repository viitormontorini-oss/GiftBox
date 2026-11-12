using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Repository.EF;
using Repository.Repositories;
using Services;
using Cafeteria.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

//referencia para entity FrameWork Bando de dados.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection com a conexão MySQL.");
builder.Services.AddDbContext<Context>(options => options.UseMySQL(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

// login do gestor com cookie quem não entrou é mandado para /Conta/Login
builder.Services.AddScoped<UsuarioCookieEvents>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)

    // cookie gestor 
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.AccessDeniedPath = "/Admin/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.EventsType = typeof(UsuarioCookieEvents);
    })

    //cookie cliente
    .AddCookie("Cliente", options =>
     {
         options.LoginPath = "/Conta/Login";
         options.ExpireTimeSpan = TimeSpan.FromHours(2);
         options.Cookie.Name = "GiftBox.Cliente";
         options.Cookie.HttpOnly = true;
         options.Cookie.SameSite = SameSiteMode.Lax;
     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// O proxy do Render termina HTTPS; o container recebe HTTP na rede interna.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER")))
    app.UseHttpsRedirection();
// preço com vírgula (R$ 12,50) nos formulários
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("pt-BR")
    .AddSupportedCultures("pt-BR")
    .AddSupportedUICultures("pt-BR"));
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
// fotos enviadas no cadastro de produto (wwwroot/uploads)
app.UseStaticFiles();

app.MapStaticAssets();
app.MapGet("/health", async (Context context) =>
    await context.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(503));
// rotas do gestor escritas em cima de cada controller ([Route("Admin/Pedidos")] etc.)
app.MapControllers();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<Context>();
    DBinitializer.initialize(context);
    // A senha inicial vem exclusivamente da configuração privada do ambiente.
    var usuarios = new UsuarioServices(new UsuarioRepository(context));
    if (usuarios.GarantirAdministradorInicial(builder.Configuration["BootstrapAdmin:Password"]))
        services.GetRequiredService<ILogger<Program>>().LogInformation("Primeiro administrador do GiftBox criado.");
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Um erro ocorreu ao tentar criar o banco de dados");
    if (!app.Environment.IsDevelopment()) throw;
}

app.Run();
