public class WeatherAPIKey : Entity
{
    [Key] public string WeatherApiKey;

    public WeatherAPIKey() { }

    public WeatherAPIKey(string key)
    {
        WeatherApiKey = key;
    }

}