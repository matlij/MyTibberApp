using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.LocalNotification;
using System.Diagnostics;
using Tibber.Sdk;

namespace MyTibberApp
{
    public partial class MainPageViewModel : ObservableObject, IObserver<RealTimeMeasurement>
    {
        private readonly EffectService _effectService;

        public MainPageViewModel()
        {
            _effectService = new EffectService(this);
            ListenerStatus = "Disconnected";
        }

        [ObservableProperty]
        private string listenerStatus;

        [ObservableProperty]
        private decimal accumulatedConsumptionLastHour;

        [ObservableProperty]
        private decimal power;

        [RelayCommand]
        async Task StartListener()
        {
            ListenerStatus = "Connecting...";
            await _effectService.StartListening();
        }

        [RelayCommand]
        async Task StopListener()
        {
            await _effectService.StopListening();
        }

        public void OnCompleted() => ListenerStatus = "Disconnected";

        public void OnError(Exception error) => ListenerStatus = $"Error: {error.Message}";

        public void OnNext(RealTimeMeasurement value)
        {
            Debug.WriteLine($"{value.Timestamp} - power: {value.Power:N0} W (average: {value.AveragePower:N0} W); consumption since last midnight: {value.AccumulatedConsumption:N3} kWh; cost since last midnight: {value.AccumulatedCost:N2} {value.Currency}");

            ListenerStatus = $"Connected ({value.Timestamp.TimeOfDay})";
            AccumulatedConsumptionLastHour = value.AccumulatedConsumptionLastHour;
            Power = value.Power;

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
