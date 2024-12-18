﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTibber.Common.Interfaces;
using MyTibber.Common.Options;
using MyTibber.Common.Repositories;
using MyTibber.Service.HostedServices;
using System.Net.Http.Headers;
using Tibber.Sdk;

namespace MyTibber.Service;

internal class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Configuration.AddJsonFile("appsettings.local.json");

        builder.Services.AddHostedService<ConsumptionHost>();
        builder.Services.AddScoped<IObserver<RealTimeMeasurement>, ConsumptionObserver>();
        builder.Services.AddScoped<IEnergyRepository, EnergyRepository>();
        builder.Services.AddScoped<HeatpumpReposiory>();
        builder.Services.AddScoped(s =>
        {
            var accessToken = builder.Configuration["TibberApiClient:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new KeyNotFoundException("TibberApiClient:AccessToken is missing in appsettings.json");
            }
            var userAgent = new ProductInfoHeaderValue("My-home-automation-system", "1.2");
            return new TibberApiClient(accessToken, userAgent);
        });

        builder.Services.Configure<UpLinkCredentialsOptions>(
            builder.Configuration.GetSection(UpLinkCredentialsOptions.UpLinkCredentials));

        builder.Services.AddHttpClient();

        using IHost host = builder.Build();

        await host.RunAsync();
    }
}
