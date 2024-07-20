using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyTibber.Service.Services;
using Tibber.Sdk;

namespace MyTibber.Service;

internal class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<ConsumptionHost>();
        builder.Services.AddScoped<IObserver<RealTimeMeasurement>, ConsumptionObserver>();
        builder.Services.AddScoped<HeaterService>();
        builder.Services.AddScoped<HeaterReposiory>();
        builder.Services.AddHttpClient();

        using IHost host = builder.Build();

        await host.RunAsync();
    }
}
