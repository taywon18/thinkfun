using MongoDB.Bson.Serialization;
using ThinkFun.Server;
using Wood;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

LogManager.Instance.DefaultConfiguration();
LogManager.Instance.Destinations.AddDestination(new Wood.Destination.FileDestination());
LogManager.Information($"Wood log system started.");


BsonClassMap.RegisterClassMap<ThinkFun.Model.User>(cm =>
{
    cm.AutoMap();
    cm.SetIgnoreExtraElements(true);
    cm.SetIdMember(cm.GetMemberMap(p => p.Identifier));
});


BsonClassMap.RegisterClassMap<ThinkFun.Model.Queue>(cm =>
{
    cm.AutoMap();
    cm.SetIgnoreExtraElements(true);
});

BsonClassMap.RegisterClassMap<ThinkFun.Model.LiveData>(cm =>
{
    cm.AutoMap();
    cm.SetDiscriminator("_t");
    cm.AddKnownType(typeof(ThinkFun.Model.Queue));
});

BsonClassMap.RegisterClassMap<ThinkFun.Model.StatusChangedEvent>(cm =>
{
    cm.AutoMap();
    cm.SetIgnoreExtraElements(true);
});

BsonClassMap.RegisterClassMap<ThinkFun.Model.Event>(cm =>
{
    cm.AutoMap();
    cm.SetDiscriminator("_t");
    cm.AddKnownType(typeof(ThinkFun.Model.StatusChangedEvent));
});


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(60);
    });


builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(60);

    options.AccessDeniedPath = "/User/AccessDenied";
    options.SlidingExpiration = true;
});





 

DataStore.Instance.Configure(builder.Configuration.GetSection("Database")).Wait();
DataManager.Instance.Configure(builder.Configuration.GetSection("Manager")).Wait();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();
app.UseCookiePolicy();


DataManager.Instance.Start().Wait();

app.Run();