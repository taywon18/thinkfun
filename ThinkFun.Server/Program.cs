using MongoDB.Bson.Serialization;
using ThinkFun.Server;
using Wood;

LogManager.Instance.DefaultConfiguration();
LogManager.Instance.Destinations.AddDestination(new Wood.Destination.FileDestination());
LogManager.Information($"Wood log system started.");

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
DataStore.Instance.Configure(builder.Configuration.GetSection("Database")).Wait();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

DataManager.Instance.Start().Wait();

app.Run();