using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Devlooped
{
    public class Renderer
    {
        readonly JsonSerializerSettings settings;

        public Renderer(HashSet<string> ignored)
        {
            settings = new JsonSerializerSettings
            {
                ContractResolver = new IgnoredPropertiesResolver(ignored),
                Converters =
                {
                    new IsoDateTimeConverter
                    {
                        DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
                        DateTimeStyles = DateTimeStyles.AdjustToUniversal
                    },
                },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

        public static Renderer Parse(params string[] args)
        {
            var props = typeof(EventGridEvent).GetProperties()
                .ToDictionary(prop => prop.Name, StringComparer.OrdinalIgnoreCase);

            var excluded = new HashSet<string>();

            foreach (var exclude in args
                .Where(s => s.StartsWith('-'))
                .Select(s => s.Substring(1)))
            {
                if (!props.TryGetValue(exclude, out var prop))
                    throw new ArgumentException($"Property '{exclude}' does not exist in {nameof(EventGridEvent)}. Cannot apply '-{exclude}'.", nameof(args));

                excluded.Add(prop.Name);
            }

            return new Renderer(excluded);
        }

        public string Render(EventGridEvent e)
        {
            // First attempt to convert a string Data property into a proper Json object
            // for improved rendering.
            if (e.Data is string data)
            {
                try
                {
                    e.Data = JsonConvert.DeserializeObject(data, settings);
                }
                catch { }
            }

            return JsonConvert.SerializeObject(e, settings);
        }

        class IgnoredPropertiesResolver : DefaultContractResolver
        {
            readonly HashSet<string> ignored;

            public IgnoredPropertiesResolver(HashSet<string> ignored) => this.ignored = ignored;

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);

                if (property.DeclaringType == typeof(EventGridEvent) && 
                    ignored.Contains(member.Name))
                {
                    property.ShouldSerialize = _ => false;
                    property.Ignored = true;
                }

                return property;
            }
        }
    }
}
