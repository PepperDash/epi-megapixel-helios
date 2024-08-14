
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
		[JsonProperty("display")]
		public DisplayObject Display { get; set; }
	}

	public class DisplayObject
	{
		[JsonProperty("blackout", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Blackout { get; set; }

        [JsonProperty("redundancy")]
        public Redundancy Redundancy { get; set; }
    }

    public class Redundancy
    {
        [JsonProperty("state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public eRedundancyState State { get; set; }
    }

    public enum eRedundancyState
    {
        main,
        backup,
        active,
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