using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Devlooped
{
    public class Renderer
    {
        /// <summary>
        /// Properties excluded by default, unless explicitly opted-in via +all
        /// </summary>
        public static HashSet<string> DefaultExcluded { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(EventGridEvent.EventType),
            nameof(EventGridEvent.Subject),
            nameof(EventGridEvent.Topic),
            nameof(EventGridEvent.DataVersion),
            nameof(EventGridEvent.MetadataVersion),
        };

        readonly JsonSerializerSettings settings;
        readonly HashSet<string> excluded;
        readonly HashSet<string> included;

        public Renderer(HashSet<string> excluded, HashSet<string> included)
        {
            this.excluded = excluded;
            this.included = included;
            settings = new JsonSerializerSettings
            {
                ContractResolver = new IgnoredPropertiesResolver(excluded),
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

        public static Renderer Parse(params string[] args) => Parse((IEnumerable<string>)args);

        public static Renderer Parse(IEnumerable<string> args)
        {
            var props = typeof(PathEventGridEvent).GetProperties()
                .ToDictionary(prop => prop.Name, StringComparer.OrdinalIgnoreCase);

            HashSet<string> excluded;
            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!args.Any(s => "+all".Equals(s, StringComparison.OrdinalIgnoreCase)))
                excluded = DefaultExcluded;
            else
                excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var include in args
                .Where(x => x.StartsWith('+'))
                .Select(s => s.TrimStart('+').Split(new[] { ':', '=' })))
            {
                excluded.Remove(include[0].Trim());
                if (props.TryGetValue(include[0].Trim(), out var prop))
                    included.Add(prop.Name);
                else
                    included.Add(include[0].Trim());
            }

            foreach (var exclude in args
                .Where(s => s.StartsWith('-'))
                .Select(s => s.TrimStart('-'))
                .Where(s => s.Length > 0))
            {
                if (props.TryGetValue(exclude.Trim(), out var prop))
                    excluded.Add(prop.Name);
                else
                    excluded.Add(exclude);
            }

            return new Renderer(excluded, included);
        }

        public override string ToString() => @$"Rendering with:
{'\t'}Include: {string.Join(", ", included)}
{'\t'}Exclude: {string.Join(", ", excluded)}";

        public string Render(PathEventGridEvent e)
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

            // Define new anonymous object to control ordering of properties to 
            // make them easier to visualize in the default configuration. Data 
            // should typically go last, for example.
            return JsonConvert.SerializeObject(new
            {
                id = e.Id,
                eventTime = e.EventTime,
                eventType = e.EventType,
                path = e.Path,
                subject = e.Subject,
                topic = e.Topic,
                dataVersion = e.DataVersion,
                metadataVersion = e.MetadataVersion,
                data = e.Data
            }, settings);
        }

        class IgnoredPropertiesResolver : DefaultContractResolver
        {
            readonly HashSet<string> ignored;

            public IgnoredPropertiesResolver(HashSet<string> ignored) => this.ignored = ignored;

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);

                if (ignored.Contains(property.PropertyName))
                {
                    property.ShouldSerialize = _ => false;
                    property.Ignored = true;
                }

                return property;
            }
        }
    }
}
