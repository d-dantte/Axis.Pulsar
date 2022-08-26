using Axis.Pulsar.Importer.Common.Json.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Axis.Pulsar.Importer.Common.Json.Utils
{
    public class RuleJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type type) => type.FullName == typeof(IRule).FullName;


        /// <summary>
        /// Convert to jobject. switch on the "type" property, returning the appropriate object.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jobj = JObject.Load(reader);
            var ruleType = jobj
                .Value<string>(nameof(IRule.Type))
                .Map(Enum.Parse<RuleType>);

            return ruleType switch
            {
                RuleType.Literal => jobj.ToObject<Literal>(),

                RuleType.Pattern => jobj.ToObject<Pattern>(),

                RuleType.Ref => jobj.ToObject<Ref>(),

                RuleType.Expression => jobj.ToObject<Expression>(serializer),

                RuleType.Grouping => jobj.ToObject<Grouping>(serializer),

                _ => throw new Exception($"Invalid rule type: {ruleType}")
            };
        }

        /// <summary>
        /// Simple object-json translation.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jobj = JObject.FromObject(value);
            jobj.WriteTo(writer, serializer.Converters.ToArray()); //what's the impact of omitting the converters?
        }
    }
}
