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

                RuleType.Pattern => ReadPattern(jobj),

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

        private Pattern ReadPattern(JObject ruleObject)
        {
            var matchTypeJobj = ruleObject[nameof(Pattern.MatchType)] as JObject;
            IMatchType matchType;
            if (matchTypeJobj == null)
                matchType = new IMatchType.OpenMatchType();

            else
            {
                var maxMisMatchProp = nameof(IMatchType.OpenMatchType.MaxMismatch);
                var allowsEmptyProp = nameof(IMatchType.OpenMatchType.AllowsEmpty);
                var maxMatchProp = nameof(IMatchType.ClosedMatchType.MaxMatch);
                var minMatchProp = nameof(IMatchType.ClosedMatchType.MinMatch);
                matchType =  matchTypeJobj.ContainsKey(maxMatchProp)
                    ? new IMatchType.ClosedMatchType
                    {
                        MaxMatch = matchTypeJobj[maxMatchProp].Value<int>(),
                        MinMatch = matchTypeJobj.TryGetValue(minMatchProp, out var value)
                            ? value.Value<int>()
                            : 1
                    }
                    : new IMatchType.OpenMatchType
                    {
                        MaxMismatch = matchTypeJobj.TryGetValue(maxMisMatchProp, out value)
                            ? value.Value<int>()
                            : 1,
                        AllowsEmpty = matchTypeJobj.TryGetValue(allowsEmptyProp, out value)
                            ? value.Value<bool>()
                            : false,
                    };
            }

            var regexProp = nameof(Pattern.Regex);
            var caseSensitiveProp = nameof(Pattern.IsCaseSensitive);
            return new Pattern
            {
                MatchType = matchType,
                Regex = ruleObject.ContainsKey(regexProp)
                    ? ruleObject[regexProp].Value<string>()
                    : null,
                IsCaseSensitive = ruleObject.ContainsKey(caseSensitiveProp)
                    ? ruleObject[caseSensitiveProp].Value<bool>()
                    : true
            };
        }
    }
}
