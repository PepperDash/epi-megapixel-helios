using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
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
        /// Constuctor
        /// </summary>
        public MegapixelHeliosPropertiesConfig()
        {
            Presets = new List<MegapixelHeliosPresetConfig>();
        }

        [JsonProperty("brightness")]
        public BrightnessConfigObject Brightness { get; set; }

        [JsonProperty("presets")]
        public List<MegapixelHeliosPresetConfig> Presets { get; set; }

        /// <summary>
        /// The processor's inputs: each becomes a routing input port (<c>key</c>, the port a tie line names) and an
        /// input the Mobile Control messenger offers (<c>name</c>); selecting it sets <c>dev.ingest.input</c> to
        /// <c>ingestName</c>. Leave out for the processor's own four: HDMI 1, HDMI 2, SDI 1, SDI 2.
        /// </summary>
        [JsonProperty("inputs", NullValueHandling = NullValueHandling.Ignore)]
        public List<MegapixelHeliosInputConfig> Inputs { get; set; }

        /// <summary>
        /// How long after a power on (blackout off) other commands are held back, because the processor ignores
        /// commands sent too soon after powering on. Commands sent in that window are delayed, not dropped. 0 (the
        /// default) sends them straight away.
        /// </summary>
        [JsonProperty("commandDelayAfterPowerOnMs", NullValueHandling = NullValueHandling.Ignore)]
        public int CommandDelayAfterPowerOnMs { get; set; }

        /// <summary>Communication monitor: no response for this long reports a warning. Default 180000 (3 minutes).</summary>
        [JsonProperty("warningTimeoutMs", NullValueHandling = NullValueHandling.Ignore)]
        public long WarningTimeoutMs { get; set; } = 180000;

        /// <summary>Communication monitor: no response for this long reports an error. Default 300000 (5 minutes).</summary>
        [JsonProperty("errorTimeoutMs", NullValueHandling = NullValueHandling.Ignore)]
        public long ErrorTimeoutMs { get; set; } = 300000;

        /// <summary>The configured inputs, or the processor's own four when none are configured.</summary>
        public List<MegapixelHeliosInputConfig> InputsOrDefault() =>
            Inputs != null && Inputs.Count > 0
                ? Inputs
                : new List<MegapixelHeliosInputConfig>
                {
                    new MegapixelHeliosInputConfig { Key = "hdmiIn1", Name = "HDMI 1", IngestName = "hdmi1" },
                    new MegapixelHeliosInputConfig { Key = "hdmiIn2", Name = "HDMI 2", IngestName = "hdmi2" },
                    new MegapixelHeliosInputConfig { Key = "sdiIn1", Name = "SDI 1", IngestName = "sdi1" },
                    new MegapixelHeliosInputConfig { Key = "sdiIn2", Name = "SDI 2", IngestName = "sdi2" },
                };
    }

    /// <summary>
    /// One processor input.
    /// </summary>
    public class MegapixelHeliosInputConfig
    {
        /// <summary>The routing input port key, used by tie lines (e.g. <c>hdmiIn1</c>).</summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>The name a panel shows (e.g. "HDMI 1").</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>The value the processor uses for <c>dev.ingest.input</c> (e.g. <c>hdmi1</c>).</summary>
        [JsonProperty("ingestName")]
        public string IngestName { get; set; }

        /// <summary>The port's connector type, for routing. Defaults to HDMI.</summary>
        [JsonProperty("connectionType", NullValueHandling = NullValueHandling.Ignore)]
        public string ConnectionType { get; set; }
    }

    /// <summary>
    /// Preset configuration object
    /// </summary>
    public class MegapixelHeliosPresetConfig
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
