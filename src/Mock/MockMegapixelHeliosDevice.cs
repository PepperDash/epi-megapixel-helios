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
    /// Two mocks with the same <c>mockPairKey</c> share their redundancy state, like a real pair (see
    /// <see cref="MockHeliosRedundancyPair"/>).
    /// </summary>
    public class MockMegapixelHeliosDevice : TwoWayDisplayBase, IOnline, ICommunicationMonitor, IMegapixelHelios
    {
        private readonly List<MegapixelHeliosInputConfig> _inputs;
        private readonly HeliosBrightnessLevels _brightnessLevels = new HeliosBrightnessLevels();
        private readonly MockHeliosRedundancyPair _pair;
        private string _role;
        private string _stateOverride;

        private bool _powerIsOn;
        private bool _warming;
        private bool _cooling;
        private string _currentInput;
        private bool _testPatternIsOn;
        private int _brightness;

        public MockMegapixelHeliosDevice(string key, string name, MegapixelHeliosPropertiesConfig props, string redundancyRole,
            string pairKey = null)
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
            _role = NormalizeRole(redundancyRole) ?? "main";
            if (!string.IsNullOrEmpty(redundancyRole) && NormalizeRole(redundancyRole) == null)
                this.LogWarning("MOCK mockRedundancyRole '{Role}' is not main, backup or offline; using main", redundancyRole);

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

            int pairMembers;
            _pair = MockHeliosRedundancyPair.Join(pairKey, this, out pairMembers);
            if (pairMembers > 2)
                this.LogWarning("MOCK pair '{PairKey}' now has {Count} units; a Helios pair has two", pairKey, pairMembers);

            IsOnline = new BoolFeedback("isOnline", () => _monitor.Status == MonitorStatus.IsOk);
            CurrentInputNameFeedback = new StringFeedback("currentInputName", () => _currentInput);
            TestPatternIsOnFeedback = new BoolFeedback("testPatternIsOn", () => _testPatternIsOn);
            BrightnessFeedback = new IntFeedback("brightness", () => _brightness);
            RedundancyRoleFeedback = new StringFeedback("redundancyRole", () => _role);
            RedundancyStateFeedback = new StringFeedback("redundancyState", () => _pair.StateFor(this));
            RedundancyDesiredFeedback = new StringFeedback("redundancyDesired", () => _pair.Desired);
            RedundancyModeFeedback = new StringFeedback("redundancyMode", () => _pair.Mode);

            // The partner's state can depend on this unit: refresh the pair now it has joined.
            _pair.NotifyMembers();

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

            // A unit in error is out of the pair, so its partner fails over.
            _pair.NotifyMembers();
        }

        /// <summary>
        /// Bench control, from the console:
        /// <c>devjson {"deviceKey":"vwp-01","methodName":"SetMockRedundancyRole","params":["offline"]}</c>.
        /// Sets this unit's role: main, backup or offline. The pair's states follow.
        /// </summary>
        public void SetMockRedundancyRole(string role)
        {
            var normalized = NormalizeRole(role);
            if (normalized == null)
            {
                this.LogWarning("MOCK SetMockRedundancyRole: '{Role}' is not main, backup or offline", role);
                return;
            }

            this.LogInformation("MOCK redundancy role {Role}", normalized);
            _role = normalized;
            _pair.NotifyMembers();
        }

        /// <summary>
        /// Bench control, from the console:
        /// <c>devjson {"deviceKey":"vwp-01","methodName":"SetMockRedundancyState","params":["mixed"]}</c>.
        /// <list type="bullet">
        /// <item><c>mixed</c>: the whole pair reports mixed (neither unit active), as the emulator's <c>mixed on</c>.</item>
        /// <item><c>active</c> / <c>standby</c>: this unit alone reports that state, whatever the pair says.</item>
        /// <item><c>auto</c> (or <c>normal</c>, <c>off</c>): clears both, so the states are computed again.</item>
        /// </list>
        /// </summary>
        public void SetMockRedundancyState(string state)
        {
            var value = (state ?? string.Empty).Trim().ToLowerInvariant();
            switch (value)
            {
                case "mixed":
                    this.LogInformation("MOCK redundancy pair {PairKey} mixed", _pair.Key);
                    _pair.SetMixed(true);
                    break;
                case "active":
                case "standby":
                    this.LogInformation("MOCK redundancy state forced {State}", value);
                    _stateOverride = value;
                    _pair.NotifyMembers();
                    break;
                case "auto":
                case "normal":
                case "off":
                case "":
                    this.LogInformation("MOCK redundancy state automatic (pair {PairKey})", _pair.Key);
                    _stateOverride = null;
                    _pair.SetMixed(false);
                    break;
                default:
                    this.LogWarning("MOCK SetMockRedundancyState: '{State}' is not mixed, active, standby or auto", state);
                    break;
            }
        }

        /// <summary>
        /// Bench control, from the console:
        /// <c>devjson {"deviceKey":"vwp-01","methodName":"GetMockRedundancyStatus","params":[]}</c>.
        /// Logs the pair, e.g. <c>pair-1: vwp-01 (main, standby) + vwp-02 (backup, active)  desired=backup ...</c>.
        /// </summary>
        public void GetMockRedundancyStatus()
        {
            this.LogInformation("MOCK redundancy {PairStatus}", _pair.Describe());
        }

        internal string MockRole => _role;
        internal string MockStateOverride => _stateOverride;
        internal bool MockIsLive => _monitor.Status != MonitorStatus.InError;

        internal void FireRedundancyFeedbacks()
        {
            RedundancyRoleFeedback?.FireUpdate();
            RedundancyStateFeedback?.FireUpdate();
            RedundancyDesiredFeedback?.FireUpdate();
            RedundancyModeFeedback?.FireUpdate();
        }

        private static string NormalizeRole(string role)
        {
            switch ((role ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "main": case "primary": return "main";
                case "backup": return "backup";
                case "offline": return "offline";
                default: return null;
            }
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

            // Sets the pair's desired controller, so either unit moves both.
            this.LogInformation("MOCK active controller {Controller} (pair {PairKey})", desired, _pair.Key);
            _pair.SetDesired(desired);
        }
    }
}
