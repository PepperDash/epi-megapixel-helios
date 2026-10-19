using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.AppServer.Messengers;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Mobile Control messenger for a Helios processor, at <c>/device/{key}</c>: what the core display messengers do
    /// not cover. Power also has the core power messenger; it is repeated here so one status message describes the
    /// processor.
    /// </summary>
    public class MegapixelHeliosMessenger : MessengerBase
    {
        private readonly IMegapixelHelios helios;

        public MegapixelHeliosMessenger(string key, string messagePath, IMegapixelHelios device)
            : base(key, messagePath, device)
        {
            helios = device;
        }

        protected override void RegisterActions()
        {
            AddAction("/fullStatus", (id, content) => PostStatusMessage(BuildState(), id));
            AddAction("/heliosStatus", (id, content) => PostStatusMessage(BuildState(), id));

            AddAction("/powerOn", (id, content) => helios.PowerOn());
            AddAction("/powerOff", (id, content) => helios.PowerOff());
            AddAction("/selectInput", (id, content) => helios.SelectInput(StringFrom(content, "input")));
            AddAction("/testPattern", (id, content) =>
            {
                if (BoolFrom(content, "on")) helios.TestPatternOn(); else helios.TestPatternOff();
            });
            AddAction("/brightness", (id, content) =>
            {
                // "high" / "medium" / "low", or a percent.
                var level = StringFrom(content, "level");
                var preset = helios.BrightnessLevels.ValueFor(level);
                if (preset.HasValue)
                    helios.SetBrightness(preset.Value);
                else if (ushort.TryParse(level, out var value))
                    helios.SetBrightness(value);
            });
            AddAction("/setActiveController", (id, content) => helios.SetActiveController(StringFrom(content, "controller")));

            helios.IsOnline.OutputChange += (_, __) => PostState();
            helios.PowerIsOnFeedback.OutputChange += (_, __) => PostState();
            helios.CurrentInputNameFeedback.OutputChange += (_, __) => PostState();
            helios.TestPatternIsOnFeedback.OutputChange += (_, __) => PostState();
            helios.BrightnessFeedback.OutputChange += (_, __) => PostState();
            helios.RedundancyRoleFeedback.OutputChange += (_, __) => PostState();
            helios.RedundancyStateFeedback.OutputChange += (_, __) => PostState();
            helios.RedundancyDesiredFeedback.OutputChange += (_, __) => PostState();
            helios.RedundancyModeFeedback.OutputChange += (_, __) => PostState();
        }

        private void PostState() => PostStatusMessage(BuildState());

        private static string StringFrom(JToken content, string property)
        {
            if (content == null) return null;
            if (content.Type == JTokenType.Object) return content[property]?.ToString();
            return content.ToString();
        }

        private static bool BoolFrom(JToken content, string property)
        {
            if (content == null) return false;
            var token = content.Type == JTokenType.Object ? content[property] : content;
            return token != null && token.Type == JTokenType.Boolean ? token.ToObject<bool>() : bool.TryParse(token?.ToString(), out var b) && b;
        }

        private MegapixelHeliosStateMessage BuildState()
        {
            var ingest = helios.CurrentInputNameFeedback.StringValue;
            var input = helios.Inputs.FirstOrDefault(i => string.Equals(i.IngestName, ingest, System.StringComparison.OrdinalIgnoreCase));

            return new MegapixelHeliosStateMessage
            {
                IsOnline = helios.IsOnline.BoolValue,
                PowerIsOn = helios.PowerIsOnFeedback.BoolValue,
                CurrentInput = input?.Key ?? ingest,
                Inputs = helios.Inputs.Select(i => new HeliosInputState { Key = i.Key, Name = i.Name }).ToList(),
                TestPatternIsOn = helios.TestPatternIsOnFeedback.BoolValue,
                Brightness = helios.BrightnessFeedback.IntValue,
                BrightnessLevel = helios.BrightnessLevels.NameFor(helios.BrightnessFeedback.IntValue),
                BrightnessLevels = new Dictionary<string, ushort>
                {
                    { "high", helios.BrightnessLevels.High },
                    { "medium", helios.BrightnessLevels.Medium },
                    { "low", helios.BrightnessLevels.Low },
                },
                Redundancy = new HeliosRedundancyState
                {
                    Role = helios.RedundancyRoleFeedback.StringValue,
                    State = helios.RedundancyStateFeedback.StringValue,
                    Desired = helios.RedundancyDesiredFeedback.StringValue,
                    Mode = helios.RedundancyModeFeedback.StringValue,
                }
            };
        }
    }

    public class MegapixelHeliosStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("isOnline")] public bool IsOnline { get; set; }
        [JsonProperty("powerIsOn")] public bool PowerIsOn { get; set; }

        /// <summary>The selected input's key (<c>hdmiIn1</c>); the raw ingest name if no input matches.</summary>
        [JsonProperty("currentInput")] public string CurrentInput { get; set; }
        [JsonProperty("inputs")] public List<HeliosInputState> Inputs { get; set; }
        [JsonProperty("testPatternIsOn")] public bool TestPatternIsOn { get; set; }
        [JsonProperty("brightness")] public int Brightness { get; set; }

        /// <summary>"high", "medium" or "low" when the brightness matches a preset; otherwise absent.</summary>
        [JsonProperty("brightnessLevel", NullValueHandling = NullValueHandling.Ignore)] public string BrightnessLevel { get; set; }
        [JsonProperty("brightnessLevels")] public Dictionary<string, ushort> BrightnessLevels { get; set; }
        [JsonProperty("redundancy")] public HeliosRedundancyState Redundancy { get; set; }
    }

    public class HeliosInputState
    {
        [JsonProperty("key")] public string Key { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }

    public class HeliosRedundancyState
    {
        [JsonProperty("role")] public string Role { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("desired")] public string Desired { get; set; }
        [JsonProperty("mode")] public string Mode { get; set; }
    }
}
