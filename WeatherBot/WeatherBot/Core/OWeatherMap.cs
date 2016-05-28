using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using WeatherBot.Core.Models;

namespace WeatherBot.Core
{
    class OWeatherMap
    {

        public async Task<WeatherObject> GetWeatherData(string query, string lang = "en")
        {
            try
            {
                using (HttpClient OWMHttpClient = new HttpClient())
                {

                    string response = await OWMHttpClient.GetStringAsync(new Uri($"http://api.openweathermap.org/data/2.5/weather?q={query}&appid={ResourcesManager.OWMAppID}&units=metric&lang={lang}"));
                    WeatherObject owmResponde = JsonConvert.DeserializeObject<WeatherObject>(response);
                    if (owmResponde != null) return owmResponde;
                    return null;

                }
            }
            catch (Exception)
            {
                return null;

            }
        }


        public async Task<WeatherForecastObject> GetForecastData(string query, DateTime dt, string lang = "en")
        {
            try
            {
                int days = (dt - DateTime.Now).Days;

                using (HttpClient OWMHttpClient = new HttpClient())
                {

                    string response = await OWMHttpClient.GetStringAsync(new Uri($"http://api.openweathermap.org/data/2.5/forecast/daily?q={query}&appid={ResourcesManager.OWMAppID}&units=metric&lang=en&cnt={days}&lang={lang}"));

                    WeatherForecastObject owmResponde = JsonConvert.DeserializeObject<WeatherForecastObject>(response);
                    if (owmResponde != null) return owmResponde;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }



        }

    }
}
