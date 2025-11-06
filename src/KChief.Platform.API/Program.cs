using KChief.Platform.API.Hubs;
using KChief.Platform.API.Services;
using KChief.Platform.Core.Interfaces;
using KChief.AlarmSystem.Services;
using KChief.Protocols.OPC.Services;
using KChief.VesselControl.Services;

namespace KChief.Platform.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "K-Chief Marine Automation Platform API",
                Version = "v1",
                Description = "RESTful API for marine vessel control and monitoring"
            });
        });

        // Add SignalR
        builder.Services.AddSignalR();

        // Register application services
        builder.Services.AddScoped<IVesselControlService, VesselControlService>();
        builder.Services.AddSingleton<IAlarmService, AlarmService>();
        builder.Services.AddSingleton<IOPCUaClient, OPCUaClientService>();
        builder.Services.AddSingleton<RealtimeUpdateService>();

        // Add CORS for development
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "K-Chief API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthorization();
        app.MapControllers();

        // Map SignalR hub
        app.MapHub<VesselHub>("/hubs/vessel");

        app.Run();
    }
}
