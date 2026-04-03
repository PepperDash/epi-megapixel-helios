
using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;
using MegapixelHelios.GenericClient;
using MegapixelHelios.JsonObjects;
using MegapixelHelios.Parameters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using Feedback = PepperDash.Essentials.Core.Feedback;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Essentials.Devices.Common.Displays;
using System.Net.Http;
using System.Threading.Tasks;
using PepperDash.Core.Logging;

namespace MegapixelHelios
{    
    /// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	public class MegapixelHeliosController : TwoWayDisplayBase, IBridgeAdvanced, IOnline, IHasPowerControlWithFeedback, IHasFeedback
	{

        #region Fields, Properties, Feedbacks, Lists, CTimer
        private List<MegaPixelHeliosPresetConfig> _presets;
        private static readonly string Separator = new string('-', 50);
        private CTimer _pollTimer;

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

        public BoolFeedback Sdi1IsValidFeedback { get; set; }

        private bool _sdi1IsValid;
        public bool Sdi1Invalid
        {
            get { return _sdi1IsValid; }
            set
            {
                if (_sdi1IsValid == value) return;
                _sdi1IsValid = value;
                Sdi1IsValidFeedback.FireUpdate();
            }
        }

        public BoolFeedback Sdi2IsValidFeedback { get; set; }

        private bool _sdi2IsValid;
        public bool Sdi2Invalid
        {
            get { return _sdi2IsValid; }
            set
            {
                if (_sdi2IsValid == value) return;
                _sdi2IsValid = value;
                Sdi2IsValidFeedback.FireUpdate();
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
                Debug.Console(MegapixelHeliosDebug.Notice, this, "Current Input change. Input: {0}", _currentInputName);
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
                Debug.Console(1, this, "WARNING: ExecuteSwitch cannot handle type {0}", selector.GetType());
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
		/// <param name="client">IRestfulComms client</param>
		public MegapixelHeliosController(string key, string name, MegapixelHeliosPropertiesConfig propertiesConfig)
			: base(key, name)
		{
			Debug.Console(MegapixelHeliosDebug.Trace, this, "Constructing new {0} instance", name);

			MegapixelHeliosDebug.ResetDebugLevels();


			if (propertiesConfig == null || propertiesConfig.Control == null)
			{
				Debug.Console(MegapixelHeliosDebug.Trace, this, "Configuration or control object is null, unable to construct new {0} instance.  Check configuration.", name);
				return;
			}

            var method = propertiesConfig.Control.Method.ToString().ToLower();
            var address = propertiesConfig.Control.TcpSshProperties.Address;
            var port = propertiesConfig.Control.TcpSshProperties.Port == 0 ? (method == "https" ? 443 : 80) : propertiesConfig.Control.TcpSshProperties.Port;

            _client = new HttpClient
            {
                BaseAddress = new Uri(
                    string.Format(
                        "{0}://{1}:{2}",
                        method,
                        address,
                        port)),
            };

			if (_client == null)
			{
				Debug.Console(MegapixelHeliosDebug.Trace, this, Debug.ErrorLogLevel.Error,
					"Failed to construct '{1}' using method {0}",
					propertiesConfig.Control.Method, name);

				return;
			}

            BrightnessLevel.High = propertiesConfig.Brightness.High;
            BrightnessLevel.Medium = propertiesConfig.Brightness.Medium;
            BrightnessLevel.Low = propertiesConfig.Brightness.Low;

			CurrentPresetIdFeedback = new IntFeedback(() => CurrentPresetId);
			CurrentPresetNameFeedback = new StringFeedback(() => CurrentPresetName);

            TestPatternIsOnFeedback = new BoolFeedback(() => TestPatternIsOn);
            BrightnessFeedback = new IntFeedback(() => Brightness);
            IsOnline = new BoolFeedback(() => DeviceIsOnline);

            Hdmi1InvalidFeedback = new BoolFeedback(() => Hdmi1Invalid);
            Hdmi2InvalidFeedback = new BoolFeedback(() => Hdmi2Invalid);
            Sdi1IsValidFeedback = new BoolFeedback(() => Sdi1Invalid);
            Sdi2IsValidFeedback = new BoolFeedback(() => Sdi2Invalid);

            CurrentInputNameFeedback = new StringFeedback(() => CurrentInputName);               

            RedundancyRoleIsMainFeedback = new BoolFeedback(() => RedundancyRoleIsMain);
            RedundancyRoleIsBackupFeedback = new BoolFeedback(() => RedundancyRoleIsBackup);
            RedundancyRoleIsOfflineFeedback = new BoolFeedback(() => RedundancyRoleIsOffline);

            RedundancyStateIsActiveFeedback = new BoolFeedback(() => RedundancyStateIsActive);
            RedundancyStateIsStandbyFeedback = new BoolFeedback(() => RedundancyStateIsStandby);
            RedundancyStateIsMixedFeedback = new BoolFeedback(() => RedundancyStateIsMixed);

            IsOnline.OutputChange += (sender, args) => Debug.Console(0, this, "Is online is on updated: {0}", args.BoolValue);
            PowerIsOnFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Power is on updated: {0}", args.BoolValue);
            CurrentInputNameFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Current input name is updated: {0}", args.StringValue);
            RedundancyRoleIsMainFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Redundancy role main updated: {0}", args.BoolValue);
            RedundancyRoleIsBackupFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Redundancy role backup updated: {0}", args.BoolValue);
            RedundancyRoleIsOfflineFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Redundancy role offline updated: {0}", args.BoolValue);
            RedundancyStateIsActiveFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Redundancy state active updated: {0}", args.BoolValue);
            RedundancyStateIsStandbyFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Redundancy state standby updated: {0}", args.BoolValue);
            RedundancyStateIsMixedFeedback.OutputChange += (sender, args) => Debug.Console(0, this, "Redundancy state mixed updated: {0}", args.BoolValue);

            _presets = propertiesConfig.Presets;

            var pollInterval = propertiesConfig.PollTimeMs > 0 ? propertiesConfig.PollTimeMs : 15000; // Default poll time is 15 seconds

            _pollTimer = new CTimer((o) => Poll(), null, pollInterval, pollInterval);

            WarmupTime = 1500; // Default warmup time is 1.5 seconds
            CooldownTime = 1500; // Default cooldown time is 1.5 seconds
            base.WarmupTime = WarmupTime;
            base.CooldownTime = CooldownTime;
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

			Debug.Console(MegapixelHeliosDebug.Notice, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(MegapixelHeliosDebug.Notice, "Linking to Bridge Type {0}", GetType().Name);

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

            trilist.SetSigTrueAction(joinMap.HotplugHdmi1.JoinNumber, HotplugHdmi1);
            trilist.SetSigTrueAction(joinMap.HotplugHdmi2.JoinNumber, HotplugHdmi2);

            trilist.SetSigTrueAction(joinMap.BrightnessHigh.JoinNumber, () => SetBrightness(BrightnessLevel.High));
            trilist.SetSigTrueAction(joinMap.BrightnessMedium.JoinNumber, () => SetBrightness(BrightnessLevel.Medium));
            trilist.SetSigTrueAction(joinMap.BrightnessLow.JoinNumber, () => SetBrightness(BrightnessLevel.Low));

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

        public override FeedbackCollection<Feedback> Feedbacks
        {
            get
            {
                var list = base.Feedbacks;
                list.AddRange(new List<Feedback>
                {

                });
                return list;
            }
        }
		#endregion

        #region Feedbacks and Responses

        private void UpdateFeedbacks()
        {
            IsOnline.FireUpdate();

            PowerIsOnFeedback.FireUpdate();
            CurrentPresetIdFeedback.FireUpdate();
            CurrentPresetNameFeedback.FireUpdate();

            Hdmi1InvalidFeedback.FireUpdate();
            Hdmi2InvalidFeedback.FireUpdate();
            Sdi1IsValidFeedback.FireUpdate();
            Sdi2IsValidFeedback.FireUpdate();
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
				//Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson: obj {0}", jToken == null ? "is null" : "is not null");
				return jToken;
			}
			catch (JsonReaderException jex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson Exception Message: {0}", jex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson Stack Trace: {0}", jex.StackTrace);
				if (jex.InnerException != null)
					Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson Inner Exception: {0}", jex.InnerException);

				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson Exception Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson Inner Exception: {0}", ex.InnerException);

				return null;
			}
		}

        private void DispatchErrorOnReceived(object sender, GenericClientDispatchErrorOnReceivedEventArgs args)
        {
            if (args.errorState)
                DeviceIsOnline = false;
        }

		private void OnResponseReceived(object sender, GenericClientResponseEventArgs args)
		{
			try
			{

				var jToken = IsValidJson(args.ContentString);
				if (jToken == null)
				{
					Debug.Console(MegapixelHeliosDebug.Notice, this, "OnResponseReceived: IsValidJson failed, passing ContentString as string");
					return;
				}

                var feedback = JsonConvert.DeserializeObject<RootDevObject>(args.ContentString);
                //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Begin parsing deserialized JSON objects");
                if (feedback.Dev.Display != null)
                {
                    if (feedback.Dev.Display.Blackout != null)
                    {
                        //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Display.Blackout");
                        PowerIsOn = !(bool)feedback.Dev.Display.Blackout;
                    }
                    
                    if (feedback.Dev.Display.Brightness != null)
                    {
                        //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Display.Brightness");
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
                        //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Display.Redundancy");
                    }
                }

                if (feedback.Dev.Ingest != null)
                {
                    if (feedback.Dev.Ingest.TestPattern != null)
                    {
                        TestPatternIsOn = (bool)feedback.Dev.Ingest.TestPattern.Enabled;
                        //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Ingest.TestPattern");
                    }

                    if (feedback.Dev.Ingest.Input != null)
                    {
                        CurrentInputName = (string)feedback.Dev.Ingest.Input;
                    }
                    else
                    {
                        Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Ingest.Input = Null");
                    }

                    if (feedback.Dev.Ingest.Inputs != null)
                    {
                        if (feedback.Dev.Ingest.Inputs.Hdmi1 != null)
                        {
                            Hdmi1Invalid = !(bool)feedback.Dev.Ingest.Inputs.Hdmi1.Valid;
                            //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Ingest.Inputs.Hdmi1.Valid");
                        }
                        if (feedback.Dev.Ingest.Inputs.Hdmi2 != null)
                        {
                            Hdmi2Invalid = !(bool)feedback.Dev.Ingest.Inputs.Hdmi2.Valid;
                            //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Ingest.Inputs.Hdmi2.Valid");
                        }
                        if (feedback.Dev.Ingest.Inputs.Sdi1 != null)
                        {
                            Sdi1Invalid = !(bool)feedback.Dev.Ingest.Inputs.Sdi1.Valid;
                            //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Ingest.Inputs.Sdi1.Valid");
                        }
                        if (feedback.Dev.Ingest.Inputs.Sdi2 != null)
                        {
                            Sdi2Invalid = !(bool)feedback.Dev.Ingest.Inputs.Sdi2.Valid;
                            //Debug.Console(MegapixelHeliosDebug.Notice, "OnResponseReceived: Parse deserialized JSON object: Dev.Ingest.Inputs.Sdi2.Valid");
                        }
                    }
                }
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Exception Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Inner Exception {0}", ex.InnerException);
			}
        }

        #endregion

        #region Requests


        private async Task DispatchRequest(string method, string path, string content)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(new HttpMethod(method), path)
                {
                    Content = new StringContent(content)
                };

                var response = await _client.SendAsync(requestMessage);
                if (response == null)
                {
                    DeviceIsOnline = false;
                    IsOnline.FireUpdate();
                    return;
                }

                using (response)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    var code = (int)response.StatusCode;
                    OnResponseReceived(this, new GenericClientResponseEventArgs { Code = code, ContentString = contentString });
                }
            }
            catch (Exception ex)
            {
                Debug.Console(MegapixelHeliosDebug.Notice, this, Debug.ErrorLogLevel.Error, "DispatchRequest Exception Message: {0}", ex.Message);
                Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "DispatchRequest Stack Trace: {0}", ex.StackTrace);
                if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "DispatchRequest Inner Exception {0}", ex.InnerException);
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
		public void Poll() => DispatchRequest("GET", "/api/v1/public", string.Empty).ContinueWith(task =>
                                       {
                                           if (task.IsFaulted)
                                           {
                                               this.LogError(task.Exception.Flatten().InnerException, "Poll request failed");
                                               DeviceIsOnline = false;
                                               IsOnline.FireUpdate();
                                           }
                                       });

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
        public void PollPrivateApi() => DispatchRequest("GET", "/api/v1/data", string.Empty).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                this.LogError(task.Exception.Flatten().InnerException, "PollPrivateApi request failed");
                DeviceIsOnline = false;
                IsOnline.FireUpdate();
            }
        });

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

            var _ = DispatchRequest("PATCH", "/api/v1/data", JsonConvert.SerializeObject(payload)).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogError(task.Exception.Flatten().InnerException, "PollPrivateApi request failed");
                    DeviceIsOnline = false;
                    IsOnline.FireUpdate();
                }
            });
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

            var _ = DispatchRequest("PATCH", "/api/v1/data", JsonConvert.SerializeObject(payload)).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogError(task.Exception.Flatten().InnerException, "PollPrivateApi request failed");
                    DeviceIsOnline = false;
                    IsOnline.FireUpdate();
                }
            });
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
            var _ = DispatchRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogError(task.Exception.Flatten().InnerException, "PollPrivateApi request failed");
                    DeviceIsOnline = false;
                    IsOnline.FireUpdate();
                }
            });

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

            var _ = DispatchRequest("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content)).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogError(task.Exception.Flatten().InnerException, "PollPrivateApi request failed");
                    DeviceIsOnline = false;
                    IsOnline.FireUpdate();
                }

                GetRedundancyState();
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

            var _ = DispatchRequest("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content)).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    this.LogError(task.Exception.Flatten().InnerException, "PollPrivateApi request failed");
                    DeviceIsOnline = false;
                    IsOnline.FireUpdate();
                }

                GetRedundancyState();
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

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));

                Thread.Sleep(3000);

                GetRedundancyState();
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

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));

                Thread.Sleep(3000);

                GetRedundancyState();
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

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", JsonConvert.SerializeObject(content));

                Thread.Sleep(3000);

                GetRedundancyState();
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
			if (string.IsNullOrEmpty(content))
			{
				Debug.Console(MegapixelHeliosDebug.Notice, "PowerOn: failed to serialzie request content");
				return;
			}

            Debug.Console(MegapixelHeliosDebug.Verbose, this, "PowerOn: content-'{0}'", content);

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", content);
                Thread.Sleep(2000);
                Poll();
            });

            if (!PowerIsOnFeedback.BoolValue && !_IsWarmingUp && !_IsCoolingDown)
            {
                _IsWarmingUp = true;
                IsWarmingUpFeedback.FireUpdate();
                // Fake power-up cycle

                if (WarmupTimer != null)
                {
                    WarmupTimer.Stop();
                    WarmupTimer.Dispose();
                }

                WarmupTimer = new CTimer(o =>
                {
                    Debug.Console(MegapixelHeliosDebug.Verbose, this, "Warmup timer ending.");
                    _IsWarmingUp = false;
                    PowerIsOn = true;
                    IsWarmingUpFeedback.FireUpdate();
                }, WarmupTime);
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
			if (string.IsNullOrEmpty(content))
			{
				Debug.Console(MegapixelHeliosDebug.Notice, "PowerOff: failed to serialzie request content");
				return;
			}

			Debug.Console(MegapixelHeliosDebug.Verbose, this, "PowerOff: content-'{0}'", content);

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", content);
                Thread.Sleep(2000);
                Poll();
            });

            _IsCoolingDown = true;
            PowerIsOn = false;
            IsCoolingDownFeedback.FireUpdate();
            // Fake cool-down cycle

            if (CooldownTimer != null)
            {
                WarmupTimer.Stop();
                WarmupTimer.Dispose();
            }

            CooldownTimer = new CTimer(o =>
            {
                Debug.Console(MegapixelHeliosDebug.Verbose, this, "Cooldown timer ending.");
                _IsCoolingDown = false;
                IsCoolingDownFeedback.FireUpdate();
            }, CooldownTime);
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
                Debug.Console(MegapixelHeliosDebug.Notice, "SetBrightness: Value sent {0} out of range", brightness);
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
            if (string.IsNullOrEmpty(content))
            {
                Debug.Console(MegapixelHeliosDebug.Notice, "SetBrightness: failed to serialzie request content");
                return;
            }

            Debug.Console(MegapixelHeliosDebug.Verbose, this, "SetBrightness: content-'{0}'", content);
            _client.SendRequest("PATCH", "/api/v1/public", content);
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
            if (string.IsNullOrEmpty(content))
            {
                Debug.Console(MegapixelHeliosDebug.Notice, "TestPatternEnable: failed to serialzie request content");
                return;
            }

            Debug.Console(MegapixelHeliosDebug.Verbose, this, "TestPatternEnable: content-'{0}'", content);

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", content);
                Thread.Sleep(2000);
                Poll();
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
            if (string.IsNullOrEmpty(content))
            {
                Debug.Console(MegapixelHeliosDebug.Notice, "TestPatternEnable: failed to serialzie request content");
                return;
            }

            Debug.Console(MegapixelHeliosDebug.Verbose, this, "TestPatternEnable: content-'{0}'", content);

            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", content);
                Thread.Sleep(2000);
                Poll();
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
			_client.SendRequest("GET", "/api/v1/presets/list", string.Empty);
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

			_client.SendRequest("POST", string.Format("/api/v1/presets/{0}/apply", id), string.Empty);
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
			if (string.IsNullOrEmpty(content))
			{
				Debug.Console(MegapixelHeliosDebug.Notice, "RecallPresetByName: failed to serialzie request content");
				return;
			}

			Debug.Console(MegapixelHeliosDebug.Verbose, this, "RecallPresetByName: content-'{0}'", content);
			_client.SendRequest("POST", "/api/v1/presets/apply", content);
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
            if (string.IsNullOrEmpty(content))
            {
                Debug.Console(MegapixelHeliosDebug.Notice, "RecallInputByName: failed to serialzie request content");
                return;
            }

            Debug.Console(MegapixelHeliosDebug.Verbose, this, "RecallInputByName: content-'{0}'", content);
            
            CrestronInvoke.BeginInvoke((o) =>
            {
                _client.SendRequest("PATCH", "/api/v1/public", content);
                Thread.Sleep(3000);
                Poll();
            });
        }

        #endregion
    }
}

