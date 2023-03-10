using ThinkFun.Server;
using Wood;

LogManager.Instance.DefaultConfiguration();
LogManager.Instance.Destinations.AddDestination(new Wood.Destination.FileDestination());
LogManager.Information($"Wood log system started.");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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