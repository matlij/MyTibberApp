using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.LocalNotification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tibber.Sdk;

namespace MyTibberApp
{
    public partial class MainPageViewModel : ObservableObject, IObserver<RealTimeMeasurement>
    {
        private readonly EffectService _effectService;

        public MainPageViewModel()
        {
            _effectService = new EffectService(this);   
        }

        [ObservableProperty]
        private decimal accumulatedConsumptionLastHour;

        [ObservableProperty]
        private decimal? lastMeterConsumption;

        [ObservableProperty]
        private string? currentTime;

        [RelayCommand]
        async Task StartListener()
        {
            await _effectService.StartListening();
        }

        [RelayCommand]
        async Task StopListener()
        {
            await _effectService.StopListening();
        }

        public void OnCompleted() => Debug.WriteLine("Real time measurement stream has been terminated.");

        public void OnError(Exception error) => Debug.WriteLine($"An error occured: {error}");

        public void OnNext(RealTimeMeasurement value)
        {
            Debug.WriteLine($"{value.Timestamp} - power: {value.Power:N0} W (average: {value.AveragePower:N0} W); consumption since last midnight: {value.AccumulatedConsumption:N3} kWh; cost since last midnight: {value.AccumulatedCost:N2} {value.Currency}");

            AccumulatedConsumptionLastHour = value.AccumulatedConsumptionLastHour;
            LastMeterConsumption = value.LastMeterConsumption;
            CurrentTime = value.Timestamp.TimeOfDay.ToString();

            const decimal consumptionLimit = 0.5m;
            if (value.AccumulatedConsumptionLastHour > consumptionLimit)
            {
                var request = new NotificationRequest
                {
                    NotificationId = 1337,
                    Title = "Hög elförbrukning",
                    Subtitle = value.AccumulatedConsumptionLastHour.ToString(),
                    Description = $"Din förbrukning är uppe i {value.AccumulatedConsumptionLastHour} kWh senaste timmen ({DateTime.Now.TimeOfDay})",
                    BadgeNumber = 1,
                };

                LocalNotificationCenter.Current.Show(request);
            }
        }
    }
}
