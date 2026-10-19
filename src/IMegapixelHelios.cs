using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// What a Helios processor offers beyond a display: input selection by name, test pattern, brightness levels
    /// and redundancy. The Mobile Control messenger works through this, so the real processor and the mock are
    /// interchangeable.
    /// </summary>
    public interface IMegapixelHelios : IKeyName, IHasPowerControlWithFeedback
    {
        BoolFeedback IsOnline { get; }

        /// <summary>The processor's inputs, in the order a panel offers them.</summary>
        IReadOnlyList<MegapixelHeliosInputConfig> Inputs { get; }

        /// <summary>The current <c>dev.ingest.input</c> (an input's <c>ingestName</c>).</summary>
        StringFeedback CurrentInputNameFeedback { get; }

        /// <summary>Select an input by its key (<c>hdmiIn1</c>) or its ingest name (<c>hdmi1</c>).</summary>
        void SelectInput(string input);

        BoolFeedback TestPatternIsOnFeedback { get; }
        void TestPatternOn();
        void TestPatternOff();

        IntFeedback BrightnessFeedback { get; }

        /// <summary>This processor's High, Medium and Low brightness values.</summary>
        HeliosBrightnessLevels BrightnessLevels { get; }
        void SetBrightness(ushort brightness);

        /// <summary><c>main</c>, <c>backup</c> or <c>offline</c>: this processor's place in the redundant pair.</summary>
        StringFeedback RedundancyRoleFeedback { get; }

        /// <summary><c>active</c>, <c>standby</c> or <c>mixed</c>: whether this processor is driving the wall.</summary>
        StringFeedback RedundancyStateFeedback { get; }

        /// <summary>The processor the pair prefers (<c>main</c> or <c>backup</c>).</summary>
        StringFeedback RedundancyDesiredFeedback { get; }

        /// <summary>The pair's mode (<c>failover</c>, <c>manual</c>, ...).</summary>
        StringFeedback RedundancyModeFeedback { get; }

        /// <summary>Override the pair's automatic failover: make the main or the backup processor the active one.</summary>
        void SetActiveController(string mainOrBackup);
    }

    /// <summary>A processor's brightness presets, in percent.</summary>
    public class HeliosBrightnessLevels
    {
        public ushort High { get; set; } = 75;
        public ushort Medium { get; set; } = 50;
        public ushort Low { get; set; } = 20;

        /// <summary>"high", "medium" or "low" for a brightness that matches one, otherwise null.</summary>
        public string NameFor(int brightness)
        {
            if (brightness == High) return "high";
            if (brightness == Medium) return "medium";
            if (brightness == Low) return "low";
            return null;
        }

        /// <summary>The value for "high", "medium" or "low"; null for anything else.</summary>
        public ushort? ValueFor(string name)
        {
            switch ((name ?? string.Empty).ToLowerInvariant())
            {
                case "high": return High;
                case "medium": return Medium;
                case "low": return Low;
                default: return null;
            }
        }
    }
}
