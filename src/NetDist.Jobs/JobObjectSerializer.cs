using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;

namespace NetDist.Jobs
{
    public static class JobObjectSerializer
    {
        /// <summary>
        /// Serializes the given object
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <param name="useObjectTypeName">Set to true in case when serializing interfaces or abstract classes</param>
        /// <param name="indented">Ident the output... or not</param>
        /// <returns>Serialized string</returns>
        public static string Serialize(object obj, bool useObjectTypeName = false, bool indented = false)
        {
            // Special settings so that interfaces and abstract objects are serialized correctly
            var serializerSettings = new JsonSerializerSettings();
            if (useObjectTypeName)
            {
                serializerSettings.TypeNameHandling = TypeNameHandling.Objects;
                serializerSettings.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
            }
            var objectString = JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, serializerSettings);
            return objectString;
        }

        /// <summary>
        /// Deserializes a string to a given object
        /// </summary>
        /// <typeparam name="TOut">Type of the object to deserialize</typeparam>
        /// <param name="objectString">Serialized string of the object</param>
        /// <param name="useObjectTypeName">Set to true in case when the serializer used is as well</param>
        /// <returns>Deserialized object</returns>
        public static TOut Deserialize<TOut>(string objectString, bool useObjectTypeName = false)
        {
            var serializerSettings = new JsonSerializerSettings();
            if (useObjectTypeName)
            {
                serializerSettings.TypeNameHandling = TypeNameHandling.Objects;
            }
            var obj = JsonConvert.DeserializeObject<TOut>(objectString, serializerSettings);
            return obj;
        }
    }
}
