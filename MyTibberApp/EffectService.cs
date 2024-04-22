using Plugin.LocalNotification;
using System.Diagnostics;
using System.Net.Http.Headers;
using Tibber.Sdk;

namespace MyTibberApp
{
    public class EffectService
    {
        private readonly string _accessToken = "hHYECYJUfCcxUbfFasjYmi4t59TDLFPPkE2Ox9yL214";
        private readonly TibberApiClient _client;
        private readonly IObserver<RealTimeMeasurement> _observer;

        public EffectService(IObserver<RealTimeMeasurement> observer)
        {
            var userAgent = new ProductInfoHeaderValue("My-home-automation-system", "1.2");
            _client = new TibberApiClient(_accessToken, userAgent);
            _observer = observer;
        }

        public async Task StartListening()
        {
            var homeId = await GetHomeId(_client);

            var listener = await _client.StartRealTimeMeasurementListener(homeId);
            listener.Subscribe(_observer);
        }

        public async Task StopListening()
        {
            var homeId = await GetHomeId(_client);

            await _client.StopRealTimeMeasurementListener(homeId);
        }

        private static async Task<Guid> GetHomeId(TibberApiClient tibberApiClient)
        {
            var basicData = await tibberApiClient.GetBasicData();
            var home = basicData.Data.Viewer.Homes.First();
            if (home is null || home.Id is null)
            {
                throw new FormatException("Home or home ID returned from Tibber is null");
            }

            return home.Id.Value;
        }
    }
}
