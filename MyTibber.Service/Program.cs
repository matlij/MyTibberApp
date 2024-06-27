using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Tibber.Sdk;

namespace MyTibber.Service;

internal class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<ConsumptionHostedService>();
        builder.Services.AddScoped<IObserver<RealTimeMeasurement>, ConsumptionObserver>();

        using IHost host = builder.Build();

        await host.RunAsync();
    }
}
