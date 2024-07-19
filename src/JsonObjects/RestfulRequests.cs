
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MegapixelHelios.JsonObjects
{
	public static class MegapixelHeliosRestRequests
	{
		public class SetBlackoutState
		{
			[JsonProperty("dev")]
			public Device Device { get; set; }

			public SetBlackoutState(bool state)
			{
				Device.Display.Blackout = state;
			}
		}

		public class RecallPresetByName
		{
			[JsonProperty("preset")]
			public JObject Params { get; set; }

			public RecallPresetByName(string name)
			{
				Params = new JObject
				{
					{ "presetName", name}
				};
			}
		}

		public class Display
		{
			[JsonProperty("blackout", NullValueHandling = NullValueHandling.Ignore)]
			public bool Blackout { get; set; }
		}

		public class Device
		{
			[JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)]
			public Display Display { get; set; }
		}

		public class Preset
		{
			[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
			public int Id { get; set; }

			[JsonProperty("presetName", NullValueHandling = NullValueHandling.Ignore)]
			public string PresetName { get; set; }

			[JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
			public DateTime CreatedAt { get; set; }

			[JsonProperty("updatedAt", NullValueHandling = NullValueHandling.Ignore)]
			public DateTime UpdatedAt { get; set; }
		}
	}
}