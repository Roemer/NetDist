using Newtonsoft.Json;

namespace Wpf.Shared
{
    /// <summary>
    /// Json.Net converter for PortableConfiguration
    /// Last updated: 21.01.2015
    /// </summary>
    public class JsonNetSerializer : PortableConfiguration.ISerializer
    {
        public T Deserialize<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public string Serialize(object settings)
        {
            var parameters = new JsonSerializerSettings { Formatting = Formatting.Indented };
            return JsonConvert.SerializeObject(settings, parameters);
        }
    }
}
