using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WeatherBot.Core.Models;

namespace WeatherBot.Core
{
    public class LuisService
    {
        public async Task<LUISObject> QueryAsync(string query)
        {
            try
            {
                using (HttpClient LUISHttpClient = new HttpClient())
                {
                    string response = await LUISHttpClient.GetStringAsync(new Uri($"https://api.projectoxford.ai/luis/v1/application?id={ResourcesManager.LUISAppID}&subscription-key={ResourcesManager.LUISSubscriptionKey}&q={query}"));

                    LUISObject luisResponse = JsonConvert.DeserializeObject<LUISObject>(response);
                    if (luisResponse != null) return luisResponse;
                }

                return null;
            }
            catch(Exception)
            {
                return null;
            }
           
        }
    }
}