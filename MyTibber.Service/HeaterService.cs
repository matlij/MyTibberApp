using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTibber.Service;

public class HeaterService
{
    public async void SetHeat()
    {
        using var httpClient = new HttpClient();

        httpClient.BaseAddress = new Uri("https://internalapi.myuplink.com/oauth/token");

        var postData = new Dictionary<string, string>
            {
                { "client_id", "My-Uplink-Web" },
                { "username", "Me@mattiasmorell.se" },
                { "password", "" },
                { "grant_type", "password" }
            };

        var content = new FormUrlEncodedContent(postData);

        var authResponse = await httpClient.PostAsync(string.Empty, content);
    }
}