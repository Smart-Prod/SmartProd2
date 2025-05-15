using SmartProd.Models;
using Microsoft.EntityFrameworkCore;
using SmartProd.Sevices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ReceitaWsService>();

ContextMongodb.ConnectionString = builder.Configuration.GetSection("MongoConnection:ConnectionString").Value;
ContextMongodb.Database = builder.Configuration.GetSection("MongoConnection:Database").Value;
ContextMongodb.IsSSL = Convert.ToBoolean(builder.Configuration.GetSection("MongoConnection:IsSSL").Value);

// Add services to the container.
builder.Services.AddIdentity<ApplicationEmpresa, ApplicationRole>()
    .AddMongoDbStores<ApplicationEmpresa, ApplicationRole, Guid>(
    ContextMongodb.ConnectionString, ContextMongodb.Database);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
