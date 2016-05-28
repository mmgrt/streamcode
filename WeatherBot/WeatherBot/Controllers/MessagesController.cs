using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using WeatherBot.Core.Models;
using Microsoft.Bot.Builder.Luis;
using WeatherBot.Core;
using System.Globalization;

namespace WeatherBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        string lang = "en";

        #region Human Language Base

        string forecastFormat = "Hello, For {0}, it'll be {1} in {2}.. with low temp at {3} and high at {4}..";
        string weatherFormat = "Hello, It's {0} in {1}.. with low temp at {2} and high at {3}..";
        string yesFormat = "Hi..actually yes, it's {0} in {1}.";
        string noFormat = "Hi.. actually No, it's {0} in {1}.";
        string helloFormat = "Hello!! You can ask me in {0} now :-)";

        #endregion




        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {

                string ErrorState = "200OK";

                if (message.Text == "/arabic")
                {

                    lang = "ar";
                    System.Web.Configuration.WebConfigurationManager.AppSettings["lang"] = lang;
                    string hello = await string.Format(helloFormat, "Arabic").Translate("en", "ar");
                    return message.CreateReplyMessage(hello);

                }
                else if (message.Text == "/english")
                {
                    lang = "en";
                    System.Web.Configuration.WebConfigurationManager.AppSettings["lang"] = lang;
                    string hello = string.Format(helloFormat, "English");
                    return message.CreateReplyMessage(hello);

                }

                //Received a message
                lang = System.Web.Configuration.WebConfigurationManager.AppSettings["lang"];

                string standardEnglishMessage = message.Text;
                if (lang == "ar")
                {
                    standardEnglishMessage = await message.Text.Translate("ar", "en");
                }

                OWeatherMap weatherService = new OWeatherMap();
                string city, time, condition;

                Core.LuisService luisService = new Core.LuisService();
                LUISObject luis = await luisService.QueryAsync(standardEnglishMessage);

                if (luis == null) { return message.CreateReplyMessage(BuildErrorMessage("404NOT_FOUND_LUISSERVICE")); }
                if (luis.intents.Count() == 0) { return message.CreateReplyMessage(BuildErrorMessage("404NOT_FOUND_LUISSERVICEINTENTS")); }

                switch (luis.intents[0]?.intent)
                {

                    #region Weather CASE:
                    case "Weather":
                        {
                            if (luis.entities.Count() == 0) { return message.CreateReplyMessage(BuildErrorMessage("404NOT_FOUND_LUISSERVICEENTITIES")); }

                            city = luis.entities.Where(ent => ent.type == "Location").FirstOrDefault()?.entity;
                            time = luis.entities.Where(ent => ent.type == "builtin.datetime.date").FirstOrDefault()?.resolution.date;

                            //Todo: Build error messages..
                            if (city == null)
                            {
                                return message.CreateReplyMessage(":TODO");
                                break; //"save state to serve again" return message.CreateReplyMessage("Please specify the location.."); }
                            }

                            if (time == null)
                            {
                                time = DateTime.Now.ToShortDateString(); //Default time is now..
                            }

                            DateTime requestedDt = time.ConvertToDateTime();
                            string replyBase;


                            if ((requestedDt - DateTime.Now).Days > 0)
                            {
                                //Forecast Requested
                                var weatherForecast = await weatherService.GetForecastData(city, requestedDt, lang);

                                List lastDayWeather = weatherForecast.list.Last();

                                string description = lastDayWeather.weather.FirstOrDefault()?.description;
                                DateTime date = lastDayWeather.dt.ConvertToDateTime();
                                string lowAt = Math.Round(lastDayWeather.temp.min) + "°";
                                string highAt = Math.Round(lastDayWeather.temp.max) + "°";
                                string cityName = "";

                                if (lang == "ar")
                                {
                                    //Country is not in a good format to translate, i.e. SA, US, UAE.. etc.
                                    cityName = await weatherForecast.city.name.Translate("en", "ar");
                                }
                                else if (lang == "en")
                                {
                                    cityName = weatherForecast.city.name + ", " + weatherForecast.city.country;
                                }

                                replyBase = forecastFormat;

                                if (lang == "ar")
                                    replyBase = await forecastFormat.Translate("en", "ar");

                                replyBase = string.Format(replyBase, date.ToString("dddd, MMMM, yyyy", new CultureInfo($"{lang},SA")), description, cityName, lowAt, highAt);

                            }
                            else
                            {
                                var weather = await weatherService.GetWeatherData(city, lang);

                                string description = weather.weather.FirstOrDefault()?.description;
                                string lowAt = weather.main.temp_min + "";
                                string highAt = weather.main.temp_min + "";
                                string cityName = "";

                                if (lang == "ar")
                                {
                                    //Country is not in a good format to translate, i.e. SA, US, UAE.. etc.
                                    cityName = await weather.name.Translate("en", "ar");
                                }
                                else if (lang == "en")
                                {
                                    cityName = weather.name + ", " + weather.sys.country;
                                }

                                //Build a reply message
                                replyBase = weatherFormat;

                                if (lang == "ar")
                                    replyBase = await weatherFormat.Translate("en", "ar");

                                replyBase = string.Format(replyBase, description, cityName, lowAt, highAt);
                                
                            }
                            return message.CreateReplyMessage(replyBase);

                        }
                    #endregion

                    #region Condition CASE:
                    case "Condition":
                        {
                            city = luis.entities.Where(ent => ent.type == "Location").FirstOrDefault()?.entity;
                            condition = luis.entities.Where(ent => ent.type == "Condition").FirstOrDefault()?.entity;

                            if (city == null)
                            {
                                return message.CreateReplyMessage(":TODO");

                                // if (city == null || condition == null) { return message.CreateReplyMessage("404NOT_FOUND_LUISSERVICEENTITIES"); }
                                break; //"save state to serve again" return message.CreateReplyMessage("Please specify the location.."); }
                            }

                            var weatherForecast = await weatherService.GetWeatherData(city, lang);
                            string description = weatherForecast.weather.FirstOrDefault()?.description;
                            string status = weatherForecast.weather.FirstOrDefault()?.main;

                            string cityName;
                            if (lang == "ar")
                            {
                                cityName = city; //Use the user input city.. OWM will return wired name city
                                status = await status.Translate("en", "ar");

                                #region Language Builder
                                description = description.Replace("لطيف", "صافي|شمس|غائم جزئيا").Replace("جيد", "صافي|شمس|غائم جزئيا").Replace("bad", "مطر|ثلج|غائم|ماطر").Replace("cold", "ثلج|برد|بارد|مطر").Replace("ليلة", "").Replace("يوم", "").Replace("صباح", "");
                                #endregion
                            }
                            else if (lang == "en")
                            {
                                cityName = weatherForecast.name + ", " + weatherForecast.sys.country;

                                #region Language Builder
                                description = description.Replace("nice", "clear|sun|bright|fine|partially cloudy").Replace("good", "clear|sun|bright|fine").Replace("bad", "rain|snow|cloud").Replace("cold", "snow|hail|sleet|blizzard").Replace("day", "").Replace("night", "").Replace("morning", "").Replace("afternoon", "");
                                #endregion
                            }


                            if (condition.ToLower().StartsWith(status.ToLower()) || description.Contains(condition))
                            {
                                if (lang == "ar") yesFormat = await yesFormat.Translate("en", "ar");

                                yesFormat = string.Format(yesFormat, description, city);
                                return message.CreateReplyMessage(yesFormat);

                            }
                            else //Condition is false
                            {
                                if (lang == "ar") noFormat = await noFormat.Translate("en", "ar");

                                noFormat = string.Format(noFormat, description, city);
                                return message.CreateReplyMessage(noFormat);
                            }
                        }
                    #endregion

                    #region None CASE:
                    case "None":
                        {

                            return message.CreateReplyMessage(BuildErrorMessage("400BAD_REQUEST"));
                            break;
                        }
                        #endregion
                }


                return message.CreateReplyMessage(BuildErrorMessage("400BAD_REQUEST"));

            }
            else
            {
                return HandleSystemMessage(message);
            }


        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }

        private string BuildErrorMessage(string errorcode)
        {
            return DateTime.Now.ToShortDateString() + " :: " + errorcode;
        }
    }

    public static class Helpers
    {

        public static DateTime ConvertToDateTime(this string dt)
        {
            if (dt.Contains("-")) { dt = dt.Replace("-", "/"); }

            if (string.IsNullOrEmpty(dt)) { return DateTime.Now; }
            DateTime _dt;

            try
            {
                _dt = System.Convert.ToDateTime(dt);
            }
            catch (FormatException)
            {
                _dt = DateTime.Now;
            }

            return _dt;
        }

     
        public static DateTime ConvertToDateTime(this double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }

}