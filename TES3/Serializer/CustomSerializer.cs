using Luaon.Linq;
using Newtonsoft.Json.Linq;
using Quest_Data_Builder.Extentions;
using YamlDotNet.Serialization;

namespace Quest_Data_Builder.TES3.Serializer
{
    internal class CustomSerializer
    {
        private readonly SerializerType _type;

        public CustomSerializer(SerializerType type)
        {
            _type = type;
        }

        public dynamic NewTable()
        {
            switch (_type)
            {
                case SerializerType.Lua: return new LTable();
                default: return new JObject();
            }
        }

        public dynamic NewArray()
        {
            switch (_type)
            {
                case SerializerType.Lua: return new LTable();
                default: return new JArray();
            }
        }

        private static object? ConvertJTokenToObject(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Object => token.Children<JProperty>()
                    .ToDictionary(prop => prop.Name, prop => ConvertJTokenToObject(prop.Value)),

                JTokenType.Array => token.Select(ConvertJTokenToObject).ToList(),

                JTokenType.Integer => token.ToObject<int>(),
                JTokenType.Float => token.ToObject<double>(),
                JTokenType.Boolean => token.ToObject<bool>(),
                JTokenType.Null => null,
                _ => token.ToString()
            };
        }

        public string GetResult(dynamic obj)
        {
            switch (_type)
            {
                case SerializerType.Lua: return "return " + obj.ToString();

                case SerializerType.Yaml:
                    var jObj = ConvertJTokenToObject(obj);
                    var serializer = new SerializerBuilder().WithEventEmitter(a => new YamlChainedEventEmitter(a)).Build();
                    return serializer.Serialize(jObj);

                default: return obj.ToString();
            }
        }
    }
}
