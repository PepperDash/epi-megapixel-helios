using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

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
        /// Serializes the poll time value
        /// </summary>
        /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// PollTimeMs property gets/sets the value as a long
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"polltimeMs": 30000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("pollTimeMs", NullValueHandling = NullValueHandling.Ignore)]
        public long PollTimeMs { get; set; }

        /// <summary>
        /// Serializes the warning timeout value
        /// </summary>
        /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// WarningTimeoutMs property gets/sets the value as a long
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"warningTimeoutMs": 180000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("warningTimeoutMs", NullValueHandling = NullValueHandling.Ignore)]
        public long WarningTimeoutMs { get; set; }

        /// <summary>
        /// Serializes the error timeout value
        /// </summary>
        /// /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// ErrorTimeoutMs property gets/sets the value as a long
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"errorTimeoutMs": 300000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("errorTimeoutMs", NullValueHandling = NullValueHandling.Ignore)]
        public long ErrorTimeoutMs { get; set; }

        /// <summary>
        /// Constuctor
        /// </summary>
        public MegapixelHeliosPropertiesConfig()
        {
            Presets = new List<MegaPixelHeliosPresetConfig>();
        }

        [JsonProperty("brightness")]
        public BrightnessConfigObject Brightness { get; set; }

        [JsonProperty("presets")]
        public List<MegaPixelHeliosPresetConfig> Presets { get; set; }
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

    public class BrightnessConfigObject
    {
        [JsonProperty("high")]
        public ushort High { get; set; }
        [JsonProperty("medium")]
        public ushort Medium { get; set; }
        [JsonProperty("low")]
        public ushort Low { get; set; }

    }
}