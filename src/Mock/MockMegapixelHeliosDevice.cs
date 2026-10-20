using System;
using System.Collections.Generic;
using System.Linq;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common;
using PepperDash.Essentials.Devices.Common.Displays;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// A Helios processor with no processor behind it, for bench testing. It has the same routing input ports,
    /// Mobile Control messenger and interfaces as <see cref="MegapixelHeliosDevice"/> and keeps its state in
    /// memory, so a config can swap <c>mockMegapixelHelios</c> for <c>megapixelHelios</c> with the same key and
    /// tie lines. Every command is logged at Information level, which is how a bench test sees it arrive.
    /// </summary>
    public class MockMegapixelHeliosDevice : TwoWayDisplayBase, IOnline, ICommunicationMonitor, IMegapixelHelios
    {
        private readonly List<MegapixelHeliosInputConfig> _inputs;
        private readonly HeliosBrightnessLevels _brightnessLevels = new HeliosBrightnessLevels();
        private readonly string _role;

        private bool _powerIsOn;
        private bool _warming;
        private bool _cooling;
        private string _currentInput;
        private bool _testPatternIsOn;
        private int _brightness;
        private string _state;
        private string _desired = "main";

        public MockMegapixelHeliosDevice(string key, string name, MegapixelHeliosPropertiesConfig props, string redundancyRole)
            : base(key, name)
        {
            props = props ?? new MegapixelHeliosPropertiesConfig();

            if (props.Brightness != null)
            {
                _brightnessLevels.High = props.Brightness.High;
                _brightnessLevels.Medium = props.Brightness.Medium;
                _brightnessLevels.Low = props.Brightness.Low;
            }

            _brightness = _brightnessLevels.High;
            _role = string.IsNullOrEmpty(redundancyRole) ? "main" : redundancyRole.ToLowerInvariant();
            _state = _role == "main" ? "active" : "standby";

            _inputs = props.InputsOrDefault();
            _currentInput = _inputs.FirstOrDefault()?.IngestName ?? string.Empty;

            foreach (var input in _inputs.Where(i => !string.IsNullOrEmpty(i.Key)))
            {
                var ingestName = input.IngestName;
                InputPorts.Add(new RoutingInputPort(input.Key, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi,
                    new Action(() => SetInput(ingestName)), this));
            }

            _monitor = new MockCommunicationMonitor(this);
            CommunicationMonitor = _monitor;
            IsOnline = new BoolFeedback("isOnline", () => _monitor.Status == MonitorStatus.IsOk);
            CurrentInputNameFeedback = new StringFeedback("currentInputName", () => _currentInput);
            TestPatternIsOnFeedback = new BoolFeedback("testPatternIsOn", () => _testPatternIsOn);
            BrightnessFeedback = new IntFeedback("brightness", () => _brightness);
            RedundancyRoleFeedback = new StringFeedback("redundancyRole", () => _role);
            RedundancyStateFeedback = new StringFeedback("redundancyState", () => _state);
            RedundancyDesiredFeedback = new StringFeedback("redundancyDesired", () => _desired);
            RedundancyModeFeedback = new StringFeedback("redundancyMode", () => "failover");

            WarmupTime = 1500;
            CooldownTime = 1500;
        }

        private readonly MockCommunicationMonitor _monitor;

        public StatusMonitorBase CommunicationMonitor { get; }
        public BoolFeedback IsOnline { get; }

        /// <summary>
        /// Bench control, from the console:
        /// <c>devjson {"deviceKey":"vwp-01","methodName":"SetIsOnline","params":[false]}</c>.
        /// Offline reports the communication monitor in error; online reports it OK.
        /// </summary>
        public void SetIsOnline(bool online) => SetCommStatus(online ? "ok" : "error");

        /// <summary>
        /// Bench control, from the console:
        /// <c>devjson {"deviceKey":"vwp-01","methodName":"SetCommStatus","params":["warning"]}</c>.
        /// Takes ok / warning / error / unknown (or the MonitorStatus names). Only "ok" is online.
        /// </summary>
        public void SetCommStatus(string status)
        {
            MonitorStatus parsed;
            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "ok": case "isok": case "online": parsed = MonitorStatus.IsOk; break;
                case "warning": case "inwarning": parsed = MonitorStatus.InWarning; break;
                case "error": case "inerror": case "offline": parsed = MonitorStatus.InError; break;
                case "unknown": case "statusunknown": parsed = MonitorStatus.StatusUnknown; break;
                default:
                    this.LogWarning("SetCommStatus: '{Status}' is not ok, warning, error or unknown", status);
                    return;
            }

            this.LogInformation("MOCK comm status {Status}", parsed);
            _monitor.SetStatus(parsed);
            IsOnline.FireUpdate();
        }
        public IReadOnlyList<MegapixelHeliosInputConfig> Inputs => _inputs;
        public StringFeedback CurrentInputNameFeedback { get; }
        public BoolFeedback TestPatternIsOnFeedback { get; }
        public IntFeedback BrightnessFeedback { get; }
        public HeliosBrightnessLevels BrightnessLevels => _brightnessLevels;
        public StringFeedback RedundancyRoleFeedback { get; }
        public StringFeedback RedundancyStateFeedback { get; }
        public StringFeedback RedundancyDesiredFeedback { get; }
        public StringFeedback RedundancyModeFeedback { get; }

        protected override Func<string> CurrentInputFeedbackFunc => () => _currentInput;
        protected override Func<bool> PowerIsOnFeedbackFunc => () => _powerIsOn;
        protected override Func<bool> IsCoolingDownFeedbackFunc => () => _cooling;
        protected override Func<bool> IsWarmingUpFeedbackFunc => () => _warming;

        protected override void CreateMobileControlMessengers()
        {
            base.CreateMobileControlMessengers();

            var mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();
            if (mc == null) return;

            mc.AddDeviceMessenger(new MegapixelHeliosMessenger($"{Key}-helios", $"/device/{Key}", this));
        }

        public override void ExecuteSwitch(object selector)
        {
            if (selector is Action action) action();
            else this.LogWarning("ExecuteSwitch cannot handle type {SelectorType}", selector?.GetType());
        }

        public override void PowerOn()
        {
            this.LogInformation("MOCK power on (blackout off)");
            if (_powerIsOn) return;

            _warming = true;
            IsWarmingUpFeedback.FireUpdate();
            _powerIsOn = true;
            PowerIsOnFeedback.FireUpdate();

            WarmupTimer?.Stop();
            WarmupTimer = new System.Timers.Timer(WarmupTime) { AutoReset = false };
            WarmupTimer.Elapsed += (s, e) => { _warming = false; IsWarmingUpFeedback.FireUpdate(); };
            WarmupTimer.Start();
        }

        public override void PowerOff()
        {
            this.LogInformation("MOCK power off (blackout on)");
            if (!_powerIsOn) return;

            _cooling = true;
            IsCoolingDownFeedback.FireUpdate();
            _powerIsOn = false;
            PowerIsOnFeedback.FireUpdate();

            CooldownTimer?.Stop();
            CooldownTimer = new System.Timers.Timer(CooldownTime) { AutoReset = false };
            CooldownTimer.Elapsed += (s, e) => { _cooling = false; IsCoolingDownFeedback.FireUpdate(); };
            CooldownTimer.Start();
        }

        public override void PowerToggle()
        {
            if (_powerIsOn) PowerOff(); else PowerOn();
        }

        public void SelectInput(string input)
        {
            var match = _inputs.FirstOrDefault(i =>
                string.Equals(i.Key, input, StringComparison.OrdinalIgnoreCase)
                || string.Equals(i.IngestName, input, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                this.LogWarning("MOCK SelectInput: no input '{Input}' configured", input);
                return;
            }

            SetInput(match.IngestName);
        }

        private void SetInput(string ingestName)
        {
            this.LogInformation("MOCK input {Input}", ingestName);
            _currentInput = ingestName;
            CurrentInputNameFeedback.FireUpdate();
            CurrentInputFeedback.FireUpdate();
        }

        public void TestPatternOn()
        {
            this.LogInformation("MOCK test pattern on");
            _testPatternIsOn = true;
            TestPatternIsOnFeedback.FireUpdate();
        }

        public void TestPatternOff()
        {
            this.LogInformation("MOCK test pattern off");
            _testPatternIsOn = false;
            TestPatternIsOnFeedback.FireUpdate();
        }

        public void SetBrightness(ushort brightness)
        {
            if (brightness <= 0 || brightness >= 100)
            {
                this.LogError("MOCK SetBrightness: value {Brightness} out of range", brightness);
                return;
            }

            this.LogInformation("MOCK brightness {Brightness}", brightness);
            _brightness = brightness;
            BrightnessFeedback.FireUpdate();
        }

        public void SetActiveController(string mainOrBackup)
        {
            var desired = (mainOrBackup ?? string.Empty).ToLowerInvariant();
            if (desired == "primary") desired = "main";
            if (desired != "main" && desired != "backup")
            {
                this.LogWarning("MOCK SetActiveController: '{Controller}' is not main or backup", mainOrBackup);
                return;
            }

            this.LogInformation("MOCK active controller {Controller}", desired);
            _desired = desired;
            _state = desired == _role ? "active" : "standby";
            RedundancyDesiredFeedback.FireUpdate();
            RedundancyStateFeedback.FireUpdate();
        }
    }
}
