using MyTibber.Common.Interfaces;
using MyTibber.Common.Options;
using MyTibber.Common.Repositories;
using MyTibber.Common.Services;
using MyTibber.WebApi.HostedServices;
using System.Net.Http.Headers;
using Tibber.Sdk;

namespace MyTibber.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddUserSecrets<Program>();

            RegisterDependencies(builder);

            builder.Services.AddAuthorization();
            builder.Services.AddHostedService<EnergyPriceRegulatorService>();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapGet("/heatadjustment", async (HttpContext httpContext, IEnergyRepository energyRepository) =>
            {
                var prices = await energyRepository.GetTodaysEnergyPrices();

                return HeatRegulator.CalculateHeatAdjustments(prices);
            });

            app.Run();
        }

        private static void RegisterDependencies(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddScoped<IEnergyRepository, EnergyRepository>();
            builder.Services.AddScoped<HeaterReposiory>();
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

        }
    }
}
