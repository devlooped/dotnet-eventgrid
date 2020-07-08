using System;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;

namespace Devlooped
{
    public class PathEventGridEvent : EventGridEvent
    {
        const string DomainsPrefix = "Microsoft.EventGrid/domains/";

        public PathEventGridEvent(string id, string subject, object data, string eventType, DateTime eventTime, string dataVersion, string? topic = null, string? metadataVersion = null)
            : base(id, subject, data, eventType, eventTime, dataVersion, topic, metadataVersion)
        {
        }

        /// <summary>
        /// Gets the Topic/Subject/EventType path of the event.
        /// </summary>
        [JsonProperty("path")]
        public string Path
        {
            get
            {
                var topic = Topic;
                var domains = Topic.IndexOf(DomainsPrefix);
                if (domains != -1)
                {
                    topic = Topic.Substring(domains + DomainsPrefix.Length).Replace("/topics/", "/");
                }

                return topic + "/" + Subject + "/" + EventType;
            }
        }
    }
}
