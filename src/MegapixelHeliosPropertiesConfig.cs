using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace MegapixelHelios
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	[ConfigSnippet("\"properties\":{}")]
	public class MegapixelHeliosPropertiesConfig
	{
		/// <summary>
		/// JSON control object
		/// </summary>
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Constuctor
		/// </summary>
		public MegapixelHeliosPropertiesConfig()
		{
		}
	}

	/// <summary>
	/// Preset configuration object
	/// </summary>
	public class MegaPixelHeliosPresetConfig
	{
		[JsonProperty("label")]
		public string Label { get; set; }

		[JsonProperty("presetName", NullValueHandling = NullValueHandling.Ignore)]
		public string PresetName { get; set; }

		[JsonProperty("presetId", NullValueHandling = NullValueHandling.Ignore)]
		public uint PreseId { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}