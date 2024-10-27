using MyTibber.Common.Interfaces;
using MyTibber.Common.Options;
using MyTibber.Common.Repositories;
using MyTibber.WebApi.HostedServices;
using MyTibber.WebUi.Components;
using System.Net.Http.Headers;
using Tibber.Sdk;

namespace MyTibber.WebUi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddApplicationInsightsTelemetry();
            }

            builder.Services.AddHostedService<EnergyPriceRegulatorService>();

            RegisterDependencies(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

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
