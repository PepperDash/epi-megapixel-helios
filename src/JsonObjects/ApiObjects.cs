
using System;
using Newtonsoft.Json;

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
        [JsonProperty("ingest")]
        public IngestObject Ingest { get; set; }
	}

	public class DisplayObject
	{
		[JsonProperty("blackout")]
		public bool Blackout { get; set; }
        [JsonProperty("brightness")]
        public int Brightness { get; set; }
	}

    public class IngestObject
    {
        [JsonProperty("testPattern")]
        public TestPatternObject TestPattern { get; set; }
    }

    public class TestPatternObject
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
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