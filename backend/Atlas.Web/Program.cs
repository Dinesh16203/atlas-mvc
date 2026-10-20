using System.Text.Json.Serialization;
using Atlas.Web.Data;
using Atlas.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Atlas") ?? "Data Source=App_Data/atlas.db";
var dbPath = connection.Split(';').FirstOrDefault(x=>x.Trim().StartsWith("Data Source=",StringComparison.OrdinalIgnoreCase))?.Split('=',2).Last().Trim();
if(!string.IsNullOrWhiteSpace(dbPath) && !Path.IsPathRooted(dbPath))
{
    var absolute=Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,dbPath));
    Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
    connection=connection.Replace(dbPath,absolute,StringComparison.OrdinalIgnoreCase);
}
builder.Services.AddDbContext<AtlasDbContext>(options=>options.UseSqlite(connection));
builder.Services.AddIdentity<AppUser,IdentityRole>(options=>{
    options.Password.RequiredLength=10;
    options.Password.RequireDigit=true;
    options.Password.RequireUppercase=true;
    options.User.RequireUniqueEmail=true;
}).AddEntityFrameworkStores<AtlasDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options=>{
    options.Cookie.Name=".ATLAS.Local";
    options.Cookie.HttpOnly=true;
    options.Cookie.SameSite=SameSiteMode.Lax;
    options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan=TimeSpan.FromHours(8);
    options.SlidingExpiration=true;
    options.Events.OnRedirectToLogin=context=>{context.Response.StatusCode=StatusCodes.Status401Unauthorized;return Task.CompletedTask;};
    options.Events.OnRedirectToAccessDenied=context=>{context.Response.StatusCode=StatusCodes.Status403Forbidden;return Task.CompletedTask;};
});
builder.Services.AddControllers().AddJsonOptions(options=>options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddScoped<SubmissionMapper>();
builder.Services.AddProblemDetails();

var app=builder.Build();
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if(File.Exists(Path.Combine(app.Environment.WebRootPath??"wwwroot","index.html"))) app.MapFallbackToFile("index.html");

await DatabaseInitializer.InitializeAsync(app.Services);
app.Run();

public partial class Program { }
