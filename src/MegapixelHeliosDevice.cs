
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.Displays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
	/// Megapixel Helios LED processor, controlled over its REST API.
	/// </summary>
	public class MegapixelHeliosDevice : TwoWayDisplayBase, IBridgeAdvanced, IOnline, IHasPowerControlWithFeedback, IHasFeedback,
        ICommunicationMonitor, IMegapixelHelios
	{

        #region Fields, Properties, Feedbacks, Lists, CTimer
        private List<MegapixelHeliosPresetConfig> _presets;
        private readonly List<MegapixelHeliosInputConfig> _inputs;
        private readonly HeliosBrightnessLevels _brightnessLevels = new HeliosBrightnessLevels();
        private readonly int _commandDelayAfterPowerOnMs;
        private DateTime _commandsHeldUntil = DateTime.MinValue;
        private readonly HeliosResponseMonitor _monitor;

        /// <summary>Reports the processor OK while it answers, then warning and error once it stops.</summary>
        public StatusMonitorBase CommunicationMonitor => _monitor;

        /// <inheritdoc />
        public IReadOnlyList<MegapixelHeliosInputConfig> Inputs => _inputs;

        /// <inheritdoc />
        public HeliosBrightnessLevels BrightnessLevels => _brightnessLevels;

        private string _redundancyDesired = string.Empty;
        private string _redundancyMode = string.Empty;

        /// <inheritdoc />
        public StringFeedback RedundancyRoleFeedback { get; private set; }
        /// <inheritdoc />
        public StringFeedback RedundancyStateFeedback { get; private set; }
        /// <inheritdoc />
        public StringFeedback RedundancyDesiredFeedback { get; private set; }
        /// <inheritdoc />
        public StringFeedback RedundancyModeFeedback { get; private set; }

        private string RedundancyRoleName =>
            RedundancyRoleIsMain ? "main" : RedundancyRoleIsBackup ? "backup" : RedundancyRoleIsOffline ? "offline" : string.Empty;

        private string RedundancyStateName =>
            RedundancyStateIsActive ? "active" : RedundancyStateIsStandby ? "standby" : RedundancyStateIsMixed ? "mixed" : string.Empty;
        private static readonly string Separator = new string('-', 50);
        private readonly CTimer _pollTimer;

        // Request failures are logged up to this many in a row, then suppressed until the device responds again
        private const int RequestFailureLogLimit = 5;
        private int _consecutiveRequestFailures;

        private bool _DeviceIsOnline;
        public BoolFeedback IsOnline { get; private set; }
        public bool DeviceIsOnline
        {
            get { return _DeviceIsOnline; }
            set
            {
                if (_DeviceIsOnline == value) return;
                _DeviceIsOnline = value;
                IsOnline.FireUpdate();
            }
        }

        private int _responseCode;
        public IntFeedback ResponseCodeFeedback { get; private set; }
        public int ResponseCode
        {
            get { return _responseCode; }
            set
            {
                if (_responseCode == value) return;
                _responseCode = value;
                ResponseCodeFeedback.FireUpdate();
            }
        }

        private bool _powerIsOn;
		public bool PowerIsOn
		{
			get { return _powerIsOn; }
			set
			{
				if (_powerIsOn == value) return;
				_powerIsOn = value;
				PowerIsOnFeedback.FireUpdate();
			}
		}

        protected override Func<string> CurrentInputFeedbackFunc { get { return () => CurrentInputName; } }
        protected override Func<bool> PowerIsOnFeedbackFunc { get { return () => PowerIsOn; } }

        bool _IsWarmingUp;
        bool _IsCoolingDown;

        protected override Func<bool> IsCoolingDownFeedbackFunc { get { return () => _IsCoolingDown; } }
        protected override Func<bool> IsWarmingUpFeedbackFunc { get { return () => _IsWarmingUp; } }

        private bool _testPatternIsOn;
        public bool TestPatternIsOn
        {
            get { return _testPatternIsOn; }
            set
            {
                if (_testPatternIsOn == value) return;
                _testPatternIsOn = value;
                TestPatternIsOnFeedback.FireUpdate();
            }
        }

        public BoolFeedback Hdmi1InvalidFeedback { get; set; }

        private bool _hdmi1Invalid;
        public bool Hdmi1Invalid
        {
            get { return _hdmi1Invalid; }
            set
            {
                if (_hdmi1Invalid == value) return;
                _hdmi1Invalid = value;
                Hdmi1InvalidFeedback.FireUpdate();
            }
        }

        public BoolFeedback Hdmi2InvalidFeedback { get; set; }

        private bool _hdmi2Invalid;
        public bool Hdmi2Invalid
        {
            get { return _hdmi2Invalid; }
            set
            {
                if (_hdmi2Invalid == value) return;
                _hdmi2Invalid = value;
                Hdmi2InvalidFeedback.FireUpdate();
            }
        }

        public BoolFeedback Sdi1InvalidFeedback { get; set; }

        private bool _sdi1Invalid;
        public bool Sdi1Invalid
        {
            get { return _sdi1Invalid; }
            set
            {
                if (_sdi1Invalid == value) return;
                _sdi1Invalid = value;
                Sdi1InvalidFeedback.FireUpdate();
            }
        }

        public BoolFeedback Sdi2InvalidFeedback { get; set; }

        private bool _sdi2Invalid;
        public bool Sdi2Invalid
        {
            get { return _sdi2Invalid; }
            set
            {
                if (_sdi2Invalid == value) return;
                _sdi2Invalid = value;
                Sdi2InvalidFeedback.FireUpdate();
            }
        }

        public StringFeedback Hdmi1ResolutionFeedback { get; set; }

        private string _hdmi1Resolution;
        public string Hdmi1Resolution
        {
            get { return _hdmi1Resolution; }
            set
            {
                if (_hdmi1Resolution == value) return;
                _hdmi1Resolution = value;
                Hdmi1ResolutionFeedback.FireUpdate();
            }
        }

        public StringFeedback Hdmi2ResolutionFeedback { get; set; }

        private string _hdmi2Resolution;
        public string Hdmi2Resolution
        {
            get { return _hdmi2Resolution; }
            set
            {
                if (_hdmi2Resolution == value) return;
                _hdmi2Resolution = value;
                Hdmi2ResolutionFeedback.FireUpdate();
            }
        }

        public StringFeedback Sdi1ResolutionFeedback { get; set; }

        private string _sdi1Resolution;
        public string Sdi1Resolution
        {
            get { return _sdi1Resolution; }
            set
            {
                if (_sdi1Resolution == value) return;
                _sdi1Resolution = value;
                Sdi1ResolutionFeedback.FireUpdate();
            }
        }

        public StringFeedback Sdi2ResolutionFeedback { get; set; }

        private string _sdi2Resolution;
        public string Sdi2Resolution
        {
            get { return _sdi2Resolution; }
            set
            {
                if (_sdi2Resolution == value) return;
                _sdi2Resolution = value;
                Sdi2ResolutionFeedback.FireUpdate();
            }
        }

        public StringFeedback Hdmi1FormatFeedback { get; set; }

        private string _hdmi1Format;
        public string Hdmi1Format
        {
            get { return _hdmi1Format; }
            set
            {
                if (_hdmi1Format == value) return;
                _hdmi1Format = value;
                Hdmi1FormatFeedback.FireUpdate();
            }
        }

        public StringFeedback Hdmi2FormatFeedback { get; set; }

        private string _hdmi2Format;
        public string Hdmi2Format
        {
            get { return _hdmi2Format; }
            set
            {
                if (_hdmi2Format == value) return;
                _hdmi2Format = value;
                Hdmi2FormatFeedback.FireUpdate();
            }
        }

        public StringFeedback Sdi1FormatFeedback { get; set; }

        private string _sdi1Format;
        public string Sdi1Format
        {
            get { return _sdi1Format; }
            set
            {
                if (_sdi1Format == value) return;
                _sdi1Format = value;
                Sdi1FormatFeedback.FireUpdate();
            }
        }

        public StringFeedback Sdi2FormatFeedback { get; set; }

        private string _sdi2Format;
        public string Sdi2Format
        {
            get { return _sdi2Format; }
            set
            {
                if (_sdi2Format == value) return;
                _sdi2Format = value;
                Sdi2FormatFeedback.FireUpdate();
            }
        }
        public BoolFeedback TestPatternIsOnFeedback { get; set; }

        private int _brightness;
        public int Brightness
        {
            get { return _brightness; }
            set
            {
                if (_brightness == value) return;
                _brightness = value;
                BrightnessFeedback.FireUpdate();
            }
        }

        public IntFeedback BrightnessFeedback { get; set; }

		private int _currentPresetId;
		public int CurrentPresetId
		{
			get { return _currentPresetId; }
			set
			{
				if (_currentPresetId == value) return;
				_currentPresetId = value;
				CurrentPresetIdFeedback.FireUpdate();
			}
		}

		public IntFeedback CurrentPresetIdFeedback { get; set; }

		private string _currentPresetName;
		public string CurrentPresetName
		{
			get { return _currentPresetName; }
			set
			{
				if (_currentPresetName == value) return;
				_currentPresetName = value;
				CurrentPresetNameFeedback.FireUpdate();
			}
		}

		public StringFeedback CurrentInputNameFeedback { get; set; }

        private string _currentInputName;
        public string CurrentInputName
        {
            get { return _currentInputName; }
            set
            {
                if (_currentInputName == value) return;
                _currentInputName = value;
                this.LogInformation("Current Input change. Input: {CurrentInput}", _currentInputName);
                CurrentInputNameFeedback.FireUpdate();
            }
        }
        public StringFeedback CurrentPresetNameFeedback { get; set; }

        private bool _redundancyRoleIsMain;
        public bool RedundancyRoleIsMain
        {
            get { return _redundancyRoleIsMain; }
            set
            {
                if (value == true)
                {
                    RedundancyRoleIsBackup = false;
                    RedundancyRoleIsOffline = false;
                }

                if (_redundancyRoleIsMain == value) return;
                _redundancyRoleIsMain = value;
                RedundancyRoleIsMainFeedback.FireUpdate();
            }
        }
        public BoolFeedback RedundancyRoleIsMainFeedback { get; set; }

        private bool _redundancyRoleIsBackup;
        public bool RedundancyRoleIsBackup
        {
            get { return _redundancyRoleIsBackup; }
            set
            {
                if (value == true)
                {
                    RedundancyRoleIsMain = false;
                    RedundancyRoleIsOffline = false;
                }

                if (_redundancyRoleIsBackup == value) return;
                _redundancyRoleIsBackup = value;
                RedundancyRoleIsBackupFeedback.FireUpdate();
            }
        }
        public BoolFeedback RedundancyRoleIsBackupFeedback { get; set; }

        private bool _redundancyRoleIsOffline;
        public bool RedundancyRoleIsOffline
        {
            get { return _redundancyRoleIsOffline; }
            set
            {
                if (value == true)
                {
                    RedundancyRoleIsMain = false;
                    RedundancyRoleIsBackup = false;
                }

                if (_redundancyRoleIsOffline == value) return;
                _redundancyRoleIsOffline = value;
                RedundancyRoleIsOfflineFeedback.FireUpdate();
            }
        }
        public BoolFeedback RedundancyRoleIsOfflineFeedback { get; set; }

        private bool _redundancyStateIsActive;
        public bool RedundancyStateIsActive
        {
            get { return _redundancyStateIsActive; }
            set
            {
                if (value == true)
                {
                    RedundancyStateIsMixed = false;
                    RedundancyStateIsStandby = false;
                }

                if (_redundancyStateIsActive == value) return;
                _redundancyStateIsActive = value;
                RedundancyStateIsActiveFeedback.FireUpdate();
            }
        }
        public BoolFeedback RedundancyStateIsActiveFeedback { get; set; }

        private bool _redundancyStateIsMixed;
        public bool RedundancyStateIsMixed
        {
            get { return _redundancyStateIsMixed; }
            set
            {
                if (value == true)
                {
                    RedundancyStateIsActive = false;
                    RedundancyStateIsStandby = false;
                }

                if (_redundancyStateIsMixed == value) return;
                _redundancyStateIsMixed = value;
                RedundancyStateIsMixedFeedback.FireUpdate();
            }
        }
        public BoolFeedback RedundancyStateIsMixedFeedback { get; set; }

        private bool _redundancyStateIsStandby;
        public bool RedundancyStateIsStandby
        {
            get { return _redundancyStateIsStandby; }
            set
            {
                if (value == true)
                {
                    RedundancyStateIsActive = false;
                    RedundancyStateIsMixed = false;
                }

                if (_redundancyStateIsStandby == value) return;
                _redundancyStateIsStandby = value;
                RedundancyStateIsStandbyFeedback.FireUpdate();
            }
        }
        public BoolFeedback RedundancyStateIsStandbyFeedback { get; set; }

        #endregion

        #region Overrides of Essentials Core TwoWayDisplayBase

        public override void ExecuteSwitch(object selector)
        {
            if (selector is Action)
                (selector as Action).Invoke();
            else
                this.LogWarning("ExecuteSwitch cannot handle type {SelectorType}", selector.GetType());
        }

        #endregion

        private readonly HttpClient _client;

        #region Constructor

        /// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key">device key</param>
		/// <param name="name">device name</param>
		/// <param name="propertiesConfig">properties configuration object</param>
		public MegapixelHeliosDevice(string key, string name, MegapixelHeliosPropertiesConfig propertiesConfig)
			: base(key, name)
		{
			if (propertiesConfig == null || propertiesConfig.Control == null)
			{
				this.LogError("Configuration or control object is null, unable to construct new {Name} instance.  Check configuration.", name);
				return;
			}

            var method = propertiesConfig.Control.Method.ToString().ToLower();
            var address = propertiesConfig.Control.TcpSshProperties.Address;
            var port = propertiesConfig.Control.TcpSshProperties.Port == 0 ? (method == "https" ? 443 : 80) : propertiesConfig.Control.TcpSshProperties.Port;
            var username = propertiesConfig.Control.TcpSshProperties.Username ?? "";
            var password = propertiesConfig.Control.TcpSshProperties.Password ?? "";

            var baseUri = new Uri(string.Format("{0}://{1}:{2}", method, address, port));

            var credentialCache = new System.Net.CredentialCache();
            if (!string.IsNullOrEmpty(username))
            {
                var credential = new System.Net.NetworkCredential(username, password);
                credentialCache.Add(baseUri, "Basic", credential);
                credentialCache.Add(baseUri, "Digest", credential);
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                Credentials = credentialCache
            };

            // HttpClient's own default is 100 s, which leaves a hung processor looking online for that long while
            // requests pile up. A timeout fails the request like any other error (offline, comm monitor told).
            var requestTimeoutMs = propertiesConfig.RequestTimeoutMs > 0
                ? propertiesConfig.RequestTimeoutMs
                : MegapixelHeliosPropertiesConfig.DefaultRequestTimeoutMs;

            _client = new HttpClient(handler)
            {
                BaseAddress = baseUri,
                Timeout = TimeSpan.FromMilliseconds(requestTimeoutMs),
            };

			if (_client == null)
			{
				this.LogError("Failed to construct '{Name}' using method {Method}", name, propertiesConfig.Control.Method);

				return;
			}

            // Each processor keeps its own levels: a static set would let the last processor configured decide
            // them for every processor in the system.
            if (propertiesConfig.Brightness != null)
            {
                _brightnessLevels.High = propertiesConfig.Brightness.High;
                _brightnessLevels.Medium = propertiesConfig.Brightness.Medium;
                _brightnessLevels.Low = propertiesConfig.Brightness.Low;
            }
            else
            {
                this.LogWarning("No brightness levels configured; using defaults High {High}, Medium {Medium}, Low {Low}",
                    _brightnessLevels.High, _brightnessLevels.Medium, _brightnessLevels.Low);
            }

            _commandDelayAfterPowerOnMs = Math.Max(0, propertiesConfig.CommandDelayAfterPowerOnMs);

            // Each input is a routing input port, so a tie line from a decoder lands on it and routing to the
            // processor selects it.
            _inputs = propertiesConfig.InputsOrDefault();
            foreach (var input in _inputs.Where(i => !string.IsNullOrEmpty(i.Key)))
            {
                var ingestName = input.IngestName;
                InputPorts.Add(new RoutingInputPort(input.Key, eRoutingSignalType.AudioVideo, ConnectionTypeFor(input),
                    new Action(() => RecallInputByName(ingestName)), this));
            }

            _monitor = new HeliosResponseMonitor(this, propertiesConfig.WarningTimeoutMs, propertiesConfig.ErrorTimeoutMs);

			CurrentPresetIdFeedback = new IntFeedback(() => CurrentPresetId);
			CurrentPresetNameFeedback = new StringFeedback(() => CurrentPresetName);

            TestPatternIsOnFeedback = new BoolFeedback(() => TestPatternIsOn);
            BrightnessFeedback = new IntFeedback(() => Brightness);
            IsOnline = new BoolFeedback(() => DeviceIsOnline);
            ResponseCodeFeedback = new IntFeedback(() => ResponseCode);

            Hdmi1InvalidFeedback = new BoolFeedback(() => Hdmi1Invalid);
            Hdmi2InvalidFeedback = new BoolFeedback(() => Hdmi2Invalid);
            Sdi1InvalidFeedback = new BoolFeedback(() => Sdi1Invalid);
            Sdi2InvalidFeedback = new BoolFeedback(() => Sdi2Invalid);

            Hdmi1ResolutionFeedback = new StringFeedback(() => Hdmi1Resolution);
            Hdmi2ResolutionFeedback = new StringFeedback(() => Hdmi2Resolution);
            Sdi1ResolutionFeedback = new StringFeedback(() => Sdi1Resolution);
            Sdi2ResolutionFeedback = new StringFeedback(() => Sdi2Resolution);
            Hdmi1FormatFeedback = new StringFeedback(() => Hdmi1Format);
            Hdmi2FormatFeedback = new StringFeedback(() => Hdmi2Format);
            Sdi1FormatFeedback = new StringFeedback(() => Sdi1Format);
            Sdi2FormatFeedback = new StringFeedback(() => Sdi2Format);

            // Matches what the device reports when an input has no signal
            Hdmi1Resolution = "No input";
            Hdmi2Resolution = "No input";
            Sdi1Resolution = "No input";
            Sdi2Resolution = "No input";

            CurrentInputNameFeedback = new StringFeedback(() => CurrentInputName);

            RedundancyRoleIsMainFeedback = new BoolFeedback(() => RedundancyRoleIsMain);
            RedundancyRoleIsBackupFeedback = new BoolFeedback(() => RedundancyRoleIsBackup);
            RedundancyRoleIsOfflineFeedback = new BoolFeedback(() => RedundancyRoleIsOffline);

            RedundancyStateIsActiveFeedback = new BoolFeedback(() => RedundancyStateIsActive);
            RedundancyStateIsStandbyFeedback = new BoolFeedback(() => RedundancyStateIsStandby);
            RedundancyStateIsMixedFeedback = new BoolFeedback(() => RedundancyStateIsMixed);

            RedundancyRoleFeedback = new StringFeedback("redundancyRole", () => RedundancyRoleName);
            RedundancyStateFeedback = new StringFeedback("redundancyState", () => RedundancyStateName);
            RedundancyDesiredFeedback = new StringFeedback("redundancyDesired", () => _redundancyDesired);
            RedundancyModeFeedback = new StringFeedback("redundancyMode", () => _redundancyMode);

            IsOnline.OutputChange += (sender, args) => this.LogInformation("Is online is on updated: {IsOnline}", args.BoolValue);
            PowerIsOnFeedback.OutputChange += (sender, args) => this.LogInformation("Power is on updated: {PowerIsOn}", args.BoolValue);
            RedundancyRoleIsMainFeedback.OutputChange += (sender, args) => this.LogDebug("Redundancy role main updated: {RedundancyRoleIsMain}", args.BoolValue);
            RedundancyRoleIsBackupFeedback.OutputChange += (sender, args) => this.LogDebug("Redundancy role backup updated: {RedundancyRoleIsBackup}", args.BoolValue);
            RedundancyRoleIsOfflineFeedback.OutputChange += (sender, args) => this.LogDebug("Redundancy role offline updated: {RedundancyRoleIsOffline}", args.BoolValue);
            RedundancyStateIsActiveFeedback.OutputChange += (sender, args) => this.LogDebug("Redundancy state active updated: {RedundancyStateIsActive}", args.BoolValue);
            RedundancyStateIsStandbyFeedback.OutputChange += (sender, args) => this.LogDebug("Redundancy state standby updated: {RedundancyStateIsStandby}", args.BoolValue);
            RedundancyStateIsMixedFeedback.OutputChange += (sender, args) => this.LogDebug("Redundancy state mixed updated: {RedundancyStateIsMixed}", args.BoolValue);

            _presets = propertiesConfig.Presets;

            var pollInterval = propertiesConfig.PollTimeMs > 0 ? propertiesConfig.PollTimeMs : 15000; // Default poll time is 15 seconds

            _pollTimer = new CTimer((o) => Poll(), null, pollInterval, pollInterval);

            WarmupTime = 1500; // Default warmup time is 1.5 seconds
            CooldownTime = 1500; // Default cooldown time is 1.5 seconds
            base.WarmupTime = WarmupTime;
            base.CooldownTime = CooldownTime;
        }

        private static eRoutingPortConnectionType ConnectionTypeFor(MegapixelHeliosInputConfig input)
        {
            if (!string.IsNullOrEmpty(input.ConnectionType)
                && Enum.TryParse<eRoutingPortConnectionType>(input.ConnectionType, true, out var parsed))
                return parsed;

            var name = (input.IngestName ?? input.Key ?? string.Empty).ToLowerInvariant();
            if (name.Contains("sdi")) return eRoutingPortConnectionType.Sdi;
            if (name.Contains("dp") || name.Contains("displayport")) return eRoutingPortConnectionType.DisplayPort;
            return eRoutingPortConnectionType.Hdmi;
        }

        /// <inheritdoc />
        protected override bool CustomActivate()
        {
            _monitor?.Start();
            return base.CustomActivate();
        }

        /// <summary>
        /// The core messengers (power, display, communication monitor) plus <see cref="MegapixelHeliosMessenger"/>
        /// for input, test pattern, brightness and redundancy.
        /// </summary>
        protected override void CreateMobileControlMessengers()
        {
            base.CreateMobileControlMessengers();

            var mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();
            if (mc == null) return;

            mc.AddDeviceMessenger(new MegapixelHeliosMessenger($"{Key}-helios", $"/device/{Key}", this));
        }

        #endregion

		#region Overrides of EssentialsBridgeableDevice

		/// <summary>
		/// Links the plugin device to the EISC bridge
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new MegapixelHeliosBridgeJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			this.LogInformation("Linking to Trilist '{TrilistId}'", trilist.ID.ToString("X"));
			this.LogInformation("Linking to Bridge Type {BridgeType}", GetType().Name);

			// links to bridge
			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			//if(StatusFeedbacks != null) StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
			//if(OnlineFeedback != null) OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

			trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, PowerOn);
			trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, PowerOff);

            trilist.SetSigTrueAction(joinMap.Poll.JoinNumber, () => Poll());

            trilist.SetSigTrueAction(joinMap.TestPatternOn.JoinNumber, TestPatternOn);
            trilist.SetSigTrueAction(joinMap.TestPatternOff.JoinNumber, TestPatternOff);

            trilist.SetStringSigAction(joinMap.RecallInputByName.JoinNumber, RecallInputByName);
            CurrentInputNameFeedback.LinkInputSig(trilist.StringInput[joinMap.RecallInputByName.JoinNumber]);

            Hdmi1InvalidFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Hdmi1Invalid.JoinNumber]);
            Hdmi2InvalidFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Hdmi2Invalid.JoinNumber]);

            Hdmi1ResolutionFeedback.LinkInputSig(trilist.StringInput[joinMap.Hdmi1Resolution.JoinNumber]);
            Hdmi2ResolutionFeedback.LinkInputSig(trilist.StringInput[joinMap.Hdmi2Resolution.JoinNumber]);
            Sdi1ResolutionFeedback.LinkInputSig(trilist.StringInput[joinMap.Sdi1Resolution.JoinNumber]);
            Sdi2ResolutionFeedback.LinkInputSig(trilist.StringInput[joinMap.Sdi2Resolution.JoinNumber]);
            Hdmi1FormatFeedback.LinkInputSig(trilist.StringInput[joinMap.Hdmi1Format.JoinNumber]);
            Hdmi2FormatFeedback.LinkInputSig(trilist.StringInput[joinMap.Hdmi2Format.JoinNumber]);
            Sdi1FormatFeedback.LinkInputSig(trilist.StringInput[joinMap.Sdi1Format.JoinNumber]);
            Sdi2FormatFeedback.LinkInputSig(trilist.StringInput[joinMap.Sdi2Format.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.HotplugHdmi1.JoinNumber, HotplugHdmi1);
            trilist.SetSigTrueAction(joinMap.HotplugHdmi2.JoinNumber, HotplugHdmi2);

            trilist.SetSigTrueAction(joinMap.BrightnessHigh.JoinNumber, () => SetBrightness(_brightnessLevels.High));
            trilist.SetSigTrueAction(joinMap.BrightnessMedium.JoinNumber, () => SetBrightness(_brightnessLevels.Medium));
            trilist.SetSigTrueAction(joinMap.BrightnessLow.JoinNumber, () => SetBrightness(_brightnessLevels.Low));

            trilist.SetUShortSigAction(joinMap.Brightness.JoinNumber, a => SetBrightness(a));

            trilist.SetSigTrueAction(joinMap.SetRedundancyRoleToMain.JoinNumber, SetRedundancyRoleToMain);
            trilist.SetSigTrueAction(joinMap.SetRedundancyRoleToBackup.JoinNumber, SetRedundancyRoleToBackup);
            trilist.SetSigTrueAction(joinMap.SetRedundancyRoleToOffline.JoinNumber, SetRedundancyRoleToOffline);

            RedundancyRoleIsMainFeedback.LinkInputSig(trilist.BooleanInput[joinMap.SetRedundancyRoleToMain.JoinNumber]);
            RedundancyRoleIsBackupFeedback.LinkInputSig(trilist.BooleanInput[joinMap.SetRedundancyRoleToBackup.JoinNumber]);
            RedundancyRoleIsOfflineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.SetRedundancyRoleToOffline.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.SetRedundancyStateToMain.JoinNumber, SetRedundancyStateToMain);
            trilist.SetSigTrueAction(joinMap.SetRedundancyStateToBackup.JoinNumber, SetRedundancyStateToBackup);

            RedundancyStateIsActiveFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RedundancyStateIsActive.JoinNumber]);
            RedundancyStateIsStandbyFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RedundancyStateIsStandby.JoinNumber]);
            RedundancyStateIsMixedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RedundancyStateIsMixed.JoinNumber]);

			trilist.SetUShortSigAction(joinMap.RecallPresetById.JoinNumber, a => RecallPresetById(a));
			trilist.SetStringSigAction(joinMap.RecallPresetByName.JoinNumber, RecallPresetByName);

			PowerIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);
			PowerIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOff.JoinNumber]);

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            ResponseCodeFeedback.LinkInputSig(trilist.UShortInput[joinMap.ResponseCode.JoinNumber]);

            TestPatternIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.TestPatternOn.JoinNumber]);
            TestPatternIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.TestPatternOff.JoinNumber]);

            BrightnessFeedback.LinkInputSig(trilist.UShortInput[joinMap.Brightness.JoinNumber]);

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

				UpdateFeedbacks();
			};
		}
		#endregion

        #region Feedbacks and Responses

        private void UpdateFeedbacks()
        {
            IsOnline.FireUpdate();
            ResponseCodeFeedback.FireUpdate();

            PowerIsOnFeedback.FireUpdate();
            CurrentPresetIdFeedback.FireUpdate();
            CurrentPresetNameFeedback.FireUpdate();

            Hdmi1InvalidFeedback.FireUpdate();
            Hdmi2InvalidFeedback.FireUpdate();
            Sdi1InvalidFeedback.FireUpdate();
            Sdi2InvalidFeedback.FireUpdate();
            Hdmi1ResolutionFeedback.FireUpdate();
            Hdmi2ResolutionFeedback.FireUpdate();
            Sdi1ResolutionFeedback.FireUpdate();
            Sdi2ResolutionFeedback.FireUpdate();
            Hdmi1FormatFeedback.FireUpdate();
            Hdmi2FormatFeedback.FireUpdate();
            Sdi1FormatFeedback.FireUpdate();
            Sdi2FormatFeedback.FireUpdate();
            CurrentInputNameFeedback.FireUpdate();

            BrightnessFeedback.FireUpdate();
            TestPatternIsOnFeedback.FireUpdate();
            IsOnline.FireUpdate();


            RedundancyRoleIsMainFeedback.FireUpdate();
            RedundancyRoleIsBackupFeedback.FireUpdate();
            RedundancyRoleIsOfflineFeedback.FireUpdate();

            RedundancyStateIsActiveFeedback.FireUpdate();
            RedundancyStateIsStandbyFeedback.FireUpdate();
            RedundancyStateIsMixedFeedback.FireUpdate();
        }

		private JToken IsValidJson(string contentString)
		{
			if (string.IsNullOrEmpty(contentString)) return null;

			contentString = contentString.Trim();
			if ((!contentString.StartsWith("{") || !contentString.EndsWith("}")) &&
				(!contentString.StartsWith("[") || !contentString.EndsWith("]"))) return null;

			try
			{
				var jToken = JToken.Parse(contentString);
				return jToken;
			}
			catch (JsonReaderException jex)
			{
				this.LogWarning(jex, "Device response is not valid JSON");
				return null;
			}
			catch (Exception ex)
			{
				this.LogError(ex, "Unexpected error validating device response");
				return null;
			}
		}

		private void OnResponseReceived(string contentString)
		{
			try
            {
				var jToken = IsValidJson(contentString);
				if (jToken == null)
				{
					return;
				}

                var feedback = JsonConvert.DeserializeObject<RootDevObject>(contentString);
                if (feedback.Dev.Display != null)
                {
                    if (feedback.Dev.Display.Blackout != null)
                    {
                        PowerIsOn = !(bool)feedback.Dev.Display.Blackout;
                    }

                    if (feedback.Dev.Display.Brightness != null)
                    {
                        Brightness = (int)feedback.Dev.Display.Brightness;
                    }

                    if (feedback.Dev.Display.Redundancy != null)
                    {
                        RedundancyRoleIsMain = (bool)(feedback.Dev.Display.Redundancy.Role == eRedundancyRole.main);
                        RedundancyRoleIsBackup = (bool)(feedback.Dev.Display.Redundancy.Role == eRedundancyRole.backup);
                        RedundancyRoleIsOffline = (bool)(feedback.Dev.Display.Redundancy.Role == eRedundancyRole.offline);
                        RedundancyStateIsActive = (bool)(feedback.Dev.Display.Redundancy.State == eRedundancyState.active);
                        RedundancyStateIsMixed = (bool)(feedback.Dev.Display.Redundancy.State == eRedundancyState.mixed);
                        RedundancyStateIsStandby = (bool)(feedback.Dev.Display.Redundancy.State == eRedundancyState.standby);

                        if (feedback.Dev.Display.Redundancy.Desired != null)
                            _redundancyDesired = feedback.Dev.Display.Redundancy.Desired;
                        if (feedback.Dev.Display.Redundancy.Mode != null)
                            _redundancyMode = feedback.Dev.Display.Redundancy.Mode;

                        RedundancyRoleFeedback.FireUpdate();
                        RedundancyStateFeedback.FireUpdate();
                        RedundancyDesiredFeedback.FireUpdate();
                        RedundancyModeFeedback.FireUpdate();
                    }
                }

                if (feedback.Dev.Ingest != null)
                {
                    if (feedback.Dev.Ingest.TestPattern != null)
                    {
                        TestPatternIsOn = (bool)feedback.Dev.Ingest.TestPattern.Enabled;
                    }

                    if (feedback.Dev.Ingest.Input != null)
                    {
                        CurrentInputName = (string)feedback.Dev.Ingest.Input;
                    }

                    if (feedback.Dev.Ingest.Inputs != null)
                    {
                        if (feedback.Dev.Ingest.Inputs.Hdmi1 != null)
                        {
                            Hdmi1Invalid = !(bool)feedback.Dev.Ingest.Inputs.Hdmi1.Valid;
                            Hdmi1Resolution = feedback.Dev.Ingest.Inputs.Hdmi1.Resolution;
                            Hdmi1Format = feedback.Dev.Ingest.Inputs.Hdmi1.Format;
                        }
                        if (feedback.Dev.Ingest.Inputs.Hdmi2 != null)
                        {
                            Hdmi2Invalid = !(bool)feedback.Dev.Ingest.Inputs.Hdmi2.Valid;
                            Hdmi2Resolution = feedback.Dev.Ingest.Inputs.Hdmi2.Resolution;
                            Hdmi2Format = feedback.Dev.Ingest.Inputs.Hdmi2.Format;
                        }
                        if (feedback.Dev.Ingest.Inputs.Sdi1 != null)
                        {
                            Sdi1Invalid = !(bool)feedback.Dev.Ingest.Inputs.Sdi1.Valid;
                            Sdi1Resolution = feedback.Dev.Ingest.Inputs.Sdi1.Resolution;
                            Sdi1Format = feedback.Dev.Ingest.Inputs.Sdi1.Format;
                        }
                        if (feedback.Dev.Ingest.Inputs.Sdi2 != null)
                        {
                            Sdi2Invalid = !(bool)feedback.Dev.Ingest.Inputs.Sdi2.Valid;
                            Sdi2Resolution = feedback.Dev.Ingest.Inputs.Sdi2.Resolution;
                            Sdi2Format = feedback.Dev.Ingest.Inputs.Sdi2.Format;
                        }
                    }
                }
			}
			catch (Exception ex)
			{
				this.LogError(ex, "Error processing device response");
			}
        }

        #endregion

        #region Requests


        /// <summary>
        /// Send a command, waiting first for the hold after a power on (see <c>commandDelayAfterPowerOnMs</c>).
        /// Polls and power commands go straight to <see cref="DispatchRequest"/>.
        /// </summary>
        private async Task SendCommandAsync(string method, string path, string content)
        {
            var wait = _commandsHeldUntil - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                this.LogDebug("Holding {Method} {Path} for {Ms} ms after power on", method, path, (int)wait.TotalMilliseconds);
                await Task.Delay(wait);
            }

            await DispatchRequest(method, path, content);
        }

        private async Task DispatchRequest(string method, string path, string content)
        {
            try
            {
                using (var requestMessage = new HttpRequestMessage(new HttpMethod(method), path)
                {
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json"),
                })
                using (var response = await _client.SendAsync(requestMessage))
                {
                    if (response == null)
                    {
                        DeviceIsOnline = false;
                        IsOnline.FireUpdate();
                        _monitor?.ReportNoResponse();
                        return;
                    }

                    var previousFailures = System.Threading.Interlocked.Exchange(ref _consecutiveRequestFailures, 0);
                    if (previousFailures >= RequestFailureLogLimit)
                        this.LogInformation("Device responding again after {FailedRequestCount} failed requests", previousFailures);

                    var contentString = await response.Content.ReadAsStringAsync();
                    var code = (int)response.StatusCode;
                    var previousCode = ResponseCode;
                    ResponseCode = code;

                    this.LogDebug("Processing response of length:{Length} with status code: {StatusCode} from path: {Path}", contentString.Length, code, path);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (code != previousCode)
                            this.LogWarning("{Method} {Path} returned HTTP {StatusCode}; device marked offline", method, path, code);
                        DeviceIsOnline = false;
                        _monitor?.ReportNoResponse();
                        return;
                    }

                    DeviceIsOnline = true;
                    _monitor?.ReportResponse();
                    OnResponseReceived(contentString);
                }
            }
            catch (Exception ex)
            {
                DeviceIsOnline = false;
                _monitor?.ReportNoResponse();

                var failures = System.Threading.Interlocked.Increment(ref _consecutiveRequestFailures);
                if (failures <= RequestFailureLogLimit)
                    this.LogError(ex, "{Method} {Path} request failed", method, path);
                if (failures == RequestFailureLogLimit)
                    this.LogWarning("Request failures continuing; suppressing further request errors until the device responds");
            }
        }

        /// <summary>
		/// Polls the device public API
		/// </summary>
        /// <example>
        /// requestType: GET
        /// path: "/api/v1/public"
        /// content: ""
        /// </example>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed.
		/// </remarks>
		public void Poll() => PollAsync();

        public Task PollAsync() => DispatchRequest("GET", "/api/v1/public", string.Empty);

        /// <summary>
        /// Polls the device private API
        /// </summary>
        /// <example>
        /// requestType: GET
        /// path: "/api/v1/data"
        /// content: ""
        /// </example>
        /// <remarks>
        /// Manufacturer public API paths will not change. Private API paths will change with firmware releases.
        /// </remarks>
        public void PollPrivateApi() => _ = DispatchRequest("GET", "/api/v1/data", string.Empty);

        /// <summary>
        /// Force hotplug on Hdmi1
        /// </summary>
        /// <example>
        /// requestType: PATCH
        /// path: "api/v1/data"
        /// content: { "dev": { "ingest": { "inputs": { "hdmi1": { "debug": { "reset": true } } } } } }
        /// combined: "api/v1/data?dev.ingest.inputs.hdmi1.debug.reset=true"
        /// </example>
        /// <remarks>
        /// Call uses private API 'data' path as opposed to 'public'.
        /// Manufacturer public API paths will not change. Private API paths will change with firmware releases.
        /// </remarks>
        public void HotplugHdmi1()
        {
            // PATCH "api/v1/data?dev.ingest.inputs.hdmi1.debug.reset=true"
            var payload = new
            {
                dev = new
                {
                    ingest = new
                    {
                        inputs = new
                        {
                            hdmi1 = new
                            {
                                debug = new
                                {
                                    reset = true
                                }
                            }
                        }
                    }
                }
            };

            var _ = SendCommandAsync("PATCH", "/api/v1/data", JsonConvert.SerializeObject(payload));
        }

        /// <summary>
        /// Force hotplug on Hdmi2
        /// </summary>
        /// <example>
        /// requestType: PATCH
        /// path: "api/v1/data"
        /// content: { "dev": { "ingest": { "inputs": { "hdmi2": { "debug": { "reset": true } } } } } }
        /// combined: "api/v1/data?dev.ingest.inputs.hdmi2.debug.reset=true"
        /// </example>
        /// <remarks>
        /// Call uses private API 'data' path as opposed to 'public'.
        /// Manufacturer public API paths will not change. Private API paths will change with firmware releases.
        /// </remarks>
        public void HotplugHdmi2()
        {
            var payload = new
            {
                dev = new
                {
                    ingest = new
                    {
                        inputs = new
                        {
                            hdmi2 = new
                            {
                                debug = new
                                {
                                    reset = true
                                }
                            }
                        }
                    }
                }
            };

            var _ = SendCommandAsync("PATCH", "/api/v1/data", JsonConvert.SerializeObject(payload));
        }

        /// <summary>
        /// Poll for redundancy role and state.
        /// </summary>
        /// <remarks>
        /// requestType: GET
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "redundancy": "" } } }
        /// </remarks>
        public void GetRedundancyState()
        {
            var _ = DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);
        }

        /// <summary>
        /// Sets the redundancy role to main and polls for current redundancy role.
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "redundancy": { "role": main } } } }
        /// </remarks>
        public void SetRedundancyRoleToMain()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new RedundancyObject
                        {
                            Role = eRedundancyRole.main
                        }
                    }
                }
            };

            var _ = Task.Run(async () => {
                await SendCommandAsync("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));
                await Task.Delay(3000);
                await DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);
            });
        }

        /// <summary>
        /// Sets the redundancy role to backup and polls for current redundancy role.
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "redundancy": { "role": backup } } } }
        /// </remarks>
        public void SetRedundancyRoleToBackup()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new RedundancyObject
                        {
                            Role = eRedundancyRole.backup
                        }
                    }
                }
            };

            var _ = Task.Run(async () => {
                await SendCommandAsync("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));
                await Task.Delay(3000);
                await DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);
            });
        }

        /// <summary>
        /// Sets the redundancy role to offline and polls for current redundancy role.
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "redundancy": { "role": offline } } } }
        /// </remarks>
        public void SetRedundancyRoleToOffline()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new RedundancyObject
                        {
                            Role = eRedundancyRole.offline
                        }
                    }
                }
            };

            var _ = Task.Run(async () => {
                await SendCommandAsync("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));
                await Task.Delay(3000);
                await DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);
            });
        }

        /// <summary>
        /// Sets the redundancy state to main and polls for current redundancy state.
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "redundancy": { "state": main } } } }
        /// </remarks>
        public void SetRedundancyStateToMain()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new RedundancyObject
                        {
                            State = eRedundancyState.main
                        }
                    }
                }
            };

            var _ = Task.Run(async () => {
                await SendCommandAsync("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));
                await Task.Delay(3000);
                await DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);
            });
        }

        /// <summary>
        /// Sets the redundancy state to backup and polls for current redundancy state.
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "redundancy": { "state": backup } } } }
        /// </remarks>
        public void SetRedundancyStateToBackup()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new RedundancyObject
                        {
                            State = eRedundancyState.backup
                        }
                    }
                }
            };

            var _ = Task.Run(async () => {
                await SendCommandAsync("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));
                await Task.Delay(3000);
                await DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);
            });
        }

		/// <summary>
		/// Power On (blackout: false)
		/// </summary>
		/// <remarks>
		/// requestType: PATCH
		/// path: "/api/v1/public"
		/// content: { "dev": { "display": { "blackout": false } } }
		/// </remarks>
		public override void PowerOn()
		{
			var jsonObject = new RootDevObject
			{
				Dev = new DevObject
				{
					Display = new DisplayObject
					{
						Blackout = false
					}
				}
			};

			var content = JsonConvert.SerializeObject(jsonObject);

            // Commands sent right after a power on are ignored by the processor, so hold them back for a while.
            if (_commandDelayAfterPowerOnMs > 0)
                _commandsHeldUntil = DateTime.UtcNow.AddMilliseconds(_commandDelayAfterPowerOnMs);

            var _ = Task.Run(async () =>
            {
                await DispatchRequest("PATCH", "/api/v1/public", content);
                await Task.Delay(2000);
                await PollAsync();
            });

            if (!PowerIsOnFeedback.BoolValue && !_IsWarmingUp && !_IsCoolingDown)
            {
                _IsWarmingUp = true;
                IsWarmingUpFeedback.FireUpdate();

                if (WarmupTimer != null)
                {
                    WarmupTimer.Stop();
                    WarmupTimer.Dispose();
                }

                WarmupTimer = new System.Timers.Timer(WarmupTime) { AutoReset = false };
                WarmupTimer.Elapsed += (s, e) =>
                {
                    this.LogVerbose("Warmup timer ending.");
                    _IsWarmingUp = false;
                    IsWarmingUpFeedback.FireUpdate();
                };
                WarmupTimer.Start();
            }
		}

		/// <summary>
		/// Power Off (blackout: true)
		/// </summary>
		/// <remarks>
		/// requestType: PATCH
		/// path: "/api/v1/public"
		/// content: { "dev": { "display": { "blackout": true } } }
		/// </remarks>
		public override void PowerOff()
		{
			var jsonObject = new RootDevObject
			{
				Dev = new DevObject
				{
					Display = new DisplayObject
					{
						Blackout = true
					}
				}
			};

            var content = JsonConvert.SerializeObject(jsonObject);

            var _ = Task.Run(async () =>
            {
                await DispatchRequest("PATCH", "/api/v1/public", content);
                await Task.Delay(2000);
                await PollAsync();
            });

            _IsCoolingDown = true;
            IsCoolingDownFeedback.FireUpdate();

            if (CooldownTimer != null)
            {
                CooldownTimer.Stop();
                CooldownTimer.Dispose();
            }

            CooldownTimer = new System.Timers.Timer(CooldownTime) { AutoReset = false };
            CooldownTimer.Elapsed += (s, e) =>
            {
                this.LogVerbose("Cooldown timer ending.");
                _IsCoolingDown = false;
                IsCoolingDownFeedback.FireUpdate();
            };
            CooldownTimer.Start();
		}

        /// <summary>
        /// Toggle device power
        /// </summary>
        public override void PowerToggle()
        {
            if (PowerIsOn)
            {
                PowerOff();
            }
            else
            {
                PowerOn();
            }
        }

        /// <summary>
        /// Brightiness (Brightness: 50)
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "display": { "brightness": 50 } } }
        /// </remarks>
        public void SetBrightness(ushort brightness)
        {
            if (brightness <= 0 || brightness >= 100)
            {
                this.LogError("SetBrightness: Value sent {Brightness} out of range", brightness);
                return;
            }

            var jsonObject = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Brightness = (int)brightness
                    }
                }
            };

            var content = JsonConvert.SerializeObject(jsonObject);
            var _ = Task.Run(async () =>
            {
                await SendCommandAsync("PATCH", "/api/v1/public", content);
            });
        }

        /// <summary>
        /// Test Pattern On (enable: true) - do not invoke pattern type
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "ingest": { "testPattern": { "enabled": true } } } }
        /// </remarks>
        public void TestPatternOn()
        {
            var jsonObject = new RootDevObject
            {
                Dev = new DevObject
                {
                    Ingest = new IngestObject
                    {
                        TestPattern = new TestPatternObject
                        {
                            Enabled = true
                        }
                    }
                }
            };

            var content = JsonConvert.SerializeObject(jsonObject);
            var _ = Task.Run(async () =>
            {
                await SendCommandAsync("PATCH", "/api/v1/public", content);
                await Task.Delay(2000);
                await PollAsync();
            });
        }

        /// <summary>
        /// Test Pattern Off (enable: false)
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: { "dev": { "ingest": { "testPattern": { "enabled": false } } } }
        /// </remarks>
        public void TestPatternOff()
        {
            var jsonObject = new RootDevObject
            {
                Dev = new DevObject
                {
                    Ingest = new IngestObject
                    {
                        TestPattern = new TestPatternObject
                        {
                            Enabled = false
                        }
                    }
                }
            };

            var content = JsonConvert.SerializeObject(jsonObject);
            var _ = Task.Run(async () =>
            {
                await SendCommandAsync("PATCH", "/api/v1/public", content);
                await Task.Delay(2000);
                await PollAsync();
            });
        }

		/// <summary>
		/// Queries device for preset list
		/// </summary>
		/// <remarks>
		/// requestType: GET
		/// path: "/api/v1/presets/list"
		/// </remarks>
		public void GetPresetsList()
		{
            var _ = Task.Run(async () =>
            {
                await DispatchRequest("GET", "/api/v1/presets/list", string.Empty);
            });
        }

		/// <summary>
		/// Recalls preset using device configured presetId
		/// </summary>
		/// <remarks>
		/// requestType: POST
		/// path: "/api/v1/presets/{id}/apply"
		/// content: ""
		/// </remarks>
		/// <param name="id"></param>
		public void RecallPresetById(uint id)
		{
			if (id == 0) return;

            var _ = Task.Run(async () =>
            {
                await SendCommandAsync("POST", string.Format("/api/v1/presets/{0}/apply", id), string.Empty);
            });
        }

		/// <summary>
		/// Recalls preset using device configured presetName
		/// </summary>
		/// <remarks>
		/// requestType: POST
		/// path: "/api/v1/presets/apply"
		/// content: "{ "presetName": "{name}" }
		/// </remarks>
		/// <param name="name"></param>
		public void RecallPresetByName(string name)
		{
			var jsonObject = new PresetNameObject()
			{
				PresetName = name
			};

			var content = JsonConvert.SerializeObject(jsonObject);
            var _ = Task.Run(async () =>
            {
                await SendCommandAsync("POST", "/api/v1/presets/apply", content);
            });
        }

        /// <summary>
        /// Recalls input using device configured input name
        /// </summary>
        /// <remarks>
        /// requestType: PATCH
        /// path: "/api/v1/public"
        /// content: "{ "dev": { "ingest": { "input": "{name}" } } }
        /// </remarks>
        /// <param name="name"></param>
        public void RecallInputByName(string name)
        {
            var jsonObject = new RootDevObject
            {
                Dev = new DevObject
                {
                    Ingest = new IngestObject
                    {
                        Input = name
                    }
                }
            };

            var content = JsonConvert.SerializeObject(jsonObject);
            var _ = Task.Run(async () =>
            {
                await SendCommandAsync("PATCH", "/api/v1/public", content);
                await Task.Delay(3000);
                await PollAsync();
            });
        }

        /// <inheritdoc />
        public void SelectInput(string input)
        {
            var match = _inputs.FirstOrDefault(i =>
                string.Equals(i.Key, input, StringComparison.OrdinalIgnoreCase)
                || string.Equals(i.IngestName, input, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                this.LogWarning("SelectInput: no input '{Input}' configured", input);
                return;
            }

            RecallInputByName(match.IngestName);
        }

        /// <inheritdoc />
        /// <remarks>Uses the redundancy state commands (<c>state: main|backup</c>), as the SIMPL bridge's
        /// SetRedundancyStateToMain / ToBackup joins do.</remarks>
        public void SetActiveController(string mainOrBackup)
        {
            switch ((mainOrBackup ?? string.Empty).ToLowerInvariant())
            {
                case "main":
                case "primary":
                    SetRedundancyStateToMain();
                    break;
                case "backup":
                    SetRedundancyStateToBackup();
                    break;
                default:
                    this.LogWarning("SetActiveController: '{Controller}' is not main or backup", mainOrBackup);
                    break;
            }
        }

        #endregion
    }
}

