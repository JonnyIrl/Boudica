using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Boudica.Classes
{
    public partial class BungieActivityResponse
    {
        [JsonProperty("Response")]
        public Response Response { get; set; }

        [JsonProperty("ErrorCode")]
        public long ErrorCode { get; set; }

        [JsonProperty("ThrottleSeconds")]
        public long ThrottleSeconds { get; set; }

        [JsonProperty("ErrorStatus")]
        public string ErrorStatus { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("MessageData")]
        public MessageData MessageData { get; set; }
    }

    public partial class MessageData
    {
    }

    public partial class Response
    {
        [JsonProperty("activities")]
        public List<BungieActivity> BungieActivities { get; set; }
    }

    public partial class BungieActivity
    {
        [JsonProperty("period")]
        public DateTimeOffset Period { get; set; }

        [JsonProperty("activityDetails")]
        public BungieActivityDetails BungieActivityDetails { get; set; }

        [JsonProperty("values")]
        public Dictionary<string, Value> Values { get; set; }
    }

    public partial class BungieActivityDetails
    {
        [JsonProperty("referenceId")]
        public long ReferenceId { get; set; }

        [JsonProperty("directorActivityHash")]
        public long DirectorActivityHash { get; set; }

        [JsonProperty("instanceId")]
        public string InstanceId { get; set; }

        [JsonProperty("mode")]
        public long Mode { get; set; }

        [JsonProperty("modes")]
        public List<long> Modes { get; set; }

        [JsonProperty("isPrivate")]
        public bool IsPrivate { get; set; }

        [JsonProperty("membershipType")]
        public long MembershipType { get; set; }
    }

    public partial class Value
    {
        [JsonProperty("statId")]
        public string StatId { get; set; }

        [JsonProperty("basic")]
        public Basic Basic { get; set; }
    }

    public partial class Basic
    {
        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("displayValue")]
        public string DisplayValue { get; set; }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}

