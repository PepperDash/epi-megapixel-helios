
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MegapixelHelios.JsonObjects
{
	public class RootDevObject
	{
		[JsonProperty("dev")]
		public DevObject Dev { get; set; }		
	}

	public class DevObject
	{
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)]
		public DisplayObject Display { get; set; }
        [JsonProperty("ingest", NullValueHandling = NullValueHandling.Ignore)]
        public IngestObject Ingest { get; set; }
	}

	public class DisplayObject
	{
		[JsonProperty("blackout", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Blackout { get; set; }

        [JsonProperty("brightness", NullValueHandling = NullValueHandling.Ignore)]
        public int? Brightness { get; set; }

        [JsonProperty("redundancy", NullValueHandling = NullValueHandling.Ignore)]
        public Redundancy Redundancy { get; set; }
    }

    public class IngestObject
    {
        [JsonProperty("testPattern", NullValueHandling = NullValueHandling.Ignore)]
        public TestPatternObject TestPattern { get; set; }
    }


    public class TestPatternObject
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Redundancy
    {

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public eRedundancyState State { get; set; }

        [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public eRedundancyRole Role { get; set; }
    }

    public enum eRedundancyState
    {
        active,
        mixed,
        standby,
        main,
        backup
    }

    public enum eRedundancyRole
    {
        main,
        backup,
        offline
    }

	public class PresetNameObject
	{
		[JsonProperty("presetName")]
		public string PresetName { get; set; }
	}

	public static class MegapixelHeliosJsonRpcRequests
	{
		public abstract class BaseRequestPropeties
		{
			[JsonProperty("id")]
			public string Id { get; protected set; }

			[JsonProperty("jsonrpc")]
			public string JsonRpc { get; protected set; }

			[JsonProperty("method")]
			public string Method { get; protected set; }

			protected BaseRequestPropeties()
			{
				JsonRpc = "2.0";
				Id = Guid.NewGuid().ToString();
				Method = string.Empty;
			}
		}
	}
}