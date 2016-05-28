using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WeatherBot.Core.Models;

namespace WeatherBot.Core
{
    public static class MSTranslator
    {
        public static async Task<string> Translate(this string query, string from, string to)
        {

            try
            {
                using (HttpClient MSTRanslatorHttpClient = new HttpClient())
                {
                    MSTRanslatorHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                    MSTRanslatorHttpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", ResourcesManager.MSTranslatorBasicAuth);

                    string uri = Uri.EscapeUriString($"https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text='{query}'&To='{to}'&From='{from}'");
                    string js = await MSTRanslatorHttpClient.GetStringAsync(uri);
                    if (string.IsNullOrEmpty(js)) { return string.Empty; }

                    MSTranslatorObject trnas = JsonConvert.DeserializeObject<MSTranslatorObject>(js);
                    return trnas.d.results[0].Text;

                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }


}