using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UPBot.UPBot_Code;
using UPBot.UPBot_Code.DataClasses;

namespace UPBot
{
    /// <summary>
    /// Цeather command, allows users to get information about the weather in their city
    /// Made with help of weatherapi.com
    /// Information can be false from time to time, but it works. 
    /// Author: J0nathan550
    /// </summary>
    public class Weather : ApplicationCommandModule
    {
        [SlashCommand("weather", "Get weather information from any city")]
        public async Task WeatherCommand(InteractionContext ctx, [Option("city", "Information in city")] string city = null)
        {
            try
            {
                if (city == null)
                {
                    DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Error!",
                        Description = "Looks like you typed wrong city, or you typed nothing.",
                        Color = DiscordColor.Red
                    };
                    await ctx.CreateResponseAsync(discordEmbed.Build());
                    return;
                }
                Utils.Log($"Weather executed by {ctx.User} command: Trying to get information from city: {city}", null);
                
                HttpClient response = new HttpClient();
                string json = response.GetStringAsync($"https://api.weatherapi.com/v1/forecast.json?key={Utils.WEATHER_API_KEY}&q={city}&days=3&aqi=yes&alerts=yes").Result;
                WeatherData data = JsonConvert.DeserializeObject<WeatherData>(json);

                if (data == null)
                {
                    DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Error!",
                        Description = "There was a problem in getting weather information, try again.",
                        Color = DiscordColor.Red
                    };
                    await ctx.CreateResponseAsync(discordEmbed.Build());
                    return;
                }

                DiscordColor orangeColor = new DiscordColor("#fc7f03");

                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder()
                {
                    Title = $"Weather information - {city}",
                    Timestamp = DateTime.Parse(data.current.LastUpdated),
                    Color = orangeColor,
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Last weather update: ",
                        IconUrl = "https://media.discordapp.net/attachments/1137667651326447726/1137668268426002452/cloudy.png"
                    },
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = $"https:{data.current.Condition.Icon}"
                    },
                };
                discordEmbedBuilder.AddField("Location", 
                    $"City: {data.location.Name} :cityscape:\n" +
                    $"Region: {data.location.Region} :map:\n" +
                    $"Country: {data.location.Country} :globe_with_meridians:\n" +
                    $"Latitude: {data.location.Lat}ϕ :map:\n" +
                    $"Longitude: {data.location.Lon}λ :map:\n" +
                    $"Timezone ID: {data.location.TzId} :timer:\n" +
                    $"Localtime: {data.location.Localtime} :clock1:", false);
                discordEmbedBuilder.AddField("Current Weather", 
                    $"Temperature °C: {data.current.TempC}°C :thermometer:\n" +
                    $"Temperature °F: {data.current.TempF}°F :thermometer:\n" +
                    $"Condition: {data.current.Condition.Text} :sunny:\n" +
                    $"Wind MPH: {data.current.WindMph} :leaves:\n" +
                    $"Wind KPH: {data.current.WindKph} :leaves:\n" +
                    $"Wind Degree: {data.current.WindDegree}° :triangular_ruler:\n" +
                    $"Wind Direction: {data.current.WindDir} :straight_ruler:\n" +
                    $"Pressure MB: {data.current.PressureMb} :compression:\n" +
                    $"Pressure IN: {data.current.PressureIn} :compression:\n" +
                    $"Precip MM: {data.current.PrecipMm} :droplet:\n" +
                    $"Precip IN: {data.current.PressureIn} :droplet:\n" +
                    $"Humidity: {data.current.Humidity} :cloud_rain:\n" +
                    $"Cloudiness: {data.current.Cloud} :cloud:\n" +
                    $"Feels like °C: {data.current.FeelslikeC}°C :thermometer:\n" +
                    $"Feels like °F: {data.current.FeelslikeF}°F :thermometer:\n" +
                    $"Visibility KM: {data.current.VisKm} :railway_track:\n" +
                    $"Visibility Miles: {data.current.VisMiles} :railway_track:\n" +
                    $"Ultraviolet index: {data.current.Uv} :beach_umbrella:\n" +
                    $"Gust MPH: {data.current.GustMph} :leaves:\n" +
                    $"Gust KPH: {data.current.GustKph} :leaves:");
                discordEmbedBuilder.AddField("Forecast", "==========================================================", false);
                List<string> convertedForecastStrings = new();
                for (int i = 0; i < data.forecast.Forecastday.Count; i++)
                {
                    convertedForecastStrings.Add(
                        $"Condition: {data.forecast.Forecastday[i].Day.Condition.Text} :sunny:\n" +
                        $"Max. Temperature °C: {data.forecast.Forecastday[i].Day.MaxtempC}°C :thermometer:\n" +
                        $"Min. Temperature °C: {data.forecast.Forecastday[i].Day.MintempC}°C :thermometer:\n" +
                        $"Avg. Temperature °C: {data.forecast.Forecastday[i].Day.AvgtempC}°C :thermometer:\n" +
                        $"Max. Temperature °F: {data.forecast.Forecastday[i].Day.MaxtempF}°F :thermometer:\n" +
                        $"Min. Temperature °F: {data.forecast.Forecastday[i].Day.MintempF}°F :thermometer:\n" +
                        $"Avg. Temperature °F: {data.forecast.Forecastday[i].Day.AvgtempF}°F :thermometer:\n" +
                        $"Max. Wind MPH: {data.forecast.Forecastday[i].Day.MaxwindMph} :leaves:\n" +
                        $"Max. Wind KPH: {data.forecast.Forecastday[i].Day.MaxwindKph} :leaves:\n" +
                        $"Total precip MM: {data.forecast.Forecastday[i].Day.TotalprecipMm} :droplet:\n" +
                        $"Total precip IN: {data.forecast.Forecastday[i].Day.TotalprecipIn} :droplet:\n" +
                        $"Total snow CM: {data.forecast.Forecastday[i].Day.TotalsnowCm} :cloud_snow:\n" +
                        $"Avg. Visibility KM: {data.forecast.Forecastday[i].Day.AvgvisKm} :railway_track:\n" +
                        $"Avg. Visibility Miles: {data.forecast.Forecastday[i].Day.AvgvisMiles} :railway_track:\n" +
                        $"Avg. Humidity: {data.forecast.Forecastday[i].Day.Avghumidity} :cloud_rain:\n" +
                        $"Will it rain?: {(data.forecast.Forecastday[i].Day.DailyWillItRain == 1 ? "Yes" : "No")} :cloud_rain:\n" +
                        $"Chance of rain: {data.forecast.Forecastday[i].Day.DailyChanceOfRain}% :cloud_rain:\n" +
                        $"Will it snow?: {(data.forecast.Forecastday[i].Day.DailyWillItSnow == 1 ? "Yes" : "No")} :cloud_snow:\n" +
                        $"Chance of snow: {data.forecast.Forecastday[i].Day.DailyChanceOfSnow}% :cloud_snow:\n" +
                        $"Ultraviolet index: {data.forecast.Forecastday[i].Day.Uv} :beach_umbrella:");
                    discordEmbedBuilder.AddField($"Date: {data.forecast.Forecastday[i].Date}", convertedForecastStrings[i], true);
                }
                discordEmbedBuilder.AddField("Astronomic Info", 
                    $"Sunrise will be: {data.forecast.Forecastday[0].Astro.Sunrise}     :sunrise:\n" +
                    $"Sunset will be: {data.forecast.Forecastday[0].Astro.Sunset}       :city_sunset:\n" +
                    $"Moonrise will be: {data.forecast.Forecastday[0].Astro.Moonrise}   :full_moon:\n" +
                    $"Moonset will be: {data.forecast.Forecastday[0].Astro.Moonset}     :crescent_moon: \n" +
                    $"Moon phase: {data.forecast.Forecastday[0].Astro.MoonPhase}        :full_moon:\n" +
                    $"Moon illumination: {data.forecast.Forecastday[0].Astro.MoonIllumination}         :bulb:\n" +
                    $"Is moon up?: {(data.forecast.Forecastday[0].Astro.IsMoonUp == 1 ? "Yes" : "No")} :full_moon:\n" +
                    $"Is sun up?: {(data.forecast.Forecastday[0].Astro.IsSunUp == 1 ? "Yes" : "No")}   :sunny:" , false);
                await ctx.CreateResponseAsync(discordEmbedBuilder.Build());
            }
            catch(Exception ex)
            {
                Utils.Log($"Weather error command:\nMessage: {ex.Message}\nStacktrace:{ex.StackTrace}", null);
                DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Error!",
                    Description = $"There was a fatal error in executing weather command.\nMessage: {ex.Message}\nStacktrace: {ex.StackTrace}",
                    Color = DiscordColor.Red
                };
                await ctx.CreateResponseAsync(discordEmbed.Build());
            }
        }
    }
}