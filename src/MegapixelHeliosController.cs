
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
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;


namespace MegapixelHelios
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	public class MegapixelHeliosController : EssentialsBridgeableDevice
	{
		private static readonly string Separator = new string('-', 50);

		#region IRestfulComms

		private readonly IRestfulComms _client;

        private int _responseCode;
		public int ResponseCode
		{
			get { return _responseCode; }
			set
			{
				if (value == _responseCode) return;
				_responseCode = value;
				ResponseCodeFeedback.FireUpdate();
			}
		}

		public IntFeedback ResponseCodeFeedback { get; private set; }


		private string _responseContent;
		public string ResponseContent
		{
			get { return _responseContent; }
			set
			{
				if (value == _responseContent) return;
				_responseContent = value;
				ResponseContentFeedback.FireUpdate();
			}
		}

		public StringFeedback ResponseContentFeedback { get; private set; }


		private string _responseError;
		public string ResponseError
		{
			get { return _responseError; }
			set
			{
				if (value == _responseError) return;
				_responseError = value;
				ResponseErrorFeedback.FireUpdate();
			}
		}

		public StringFeedback ResponseErrorFeedback { get; private set; }

		#endregion


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

		public BoolFeedback PowerIsOnFeedback { get; set; }

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


        private List<MegaPixelHeliosPresetConfig> _presets;

        private CTimer _pollTimer;

        private bool _isOnline;
        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                _isOnline = value;
                IsOnlineFeedback.FireUpdate();
            }
        }

        public BoolFeedback IsOnlineFeedback;


		/// <summary>
		/// Reports online feedback through the bridge
		/// </summary>
		//public BoolFeedback OnlineFeedback { get; private set; }

		/// <summary>
		/// Reports socket status feedback through the bridge
		/// </summary>
		//public IntFeedback StatusFeedback { get; private set; }

		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key">device key</param>
		/// <param name="name">device name</param>
		/// <param name="propertiesConfig">properties configuration object</param>
		/// <param name="client">IRestfulComms client</param>
		public MegapixelHeliosController(string key, string name, MegapixelHeliosPropertiesConfig propertiesConfig, IRestfulComms client)
			: base(key, name)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);

			MegapixelHeliosDebug.ResetDebugLevels();


			if (propertiesConfig == null || propertiesConfig.Control == null)
			{
				Debug.Console(MegapixelHeliosDebug.Trace, this, "Configuration or control object is null, unable to construct new {0} instance.  Check configuration.", name);
				return;
			}

			_client = client;
			if (_client == null)
			{
				Debug.Console(MegapixelHeliosDebug.Trace, this, Debug.ErrorLogLevel.Error,
					"Failed to construct '{1}' using method {0}",
					propertiesConfig.Control.Method, name);
				return;
			}

			_client.ResponseReceived += OnResponseReceived;

			//OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
			//StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

            BrightnessLevel.High = propertiesConfig.Brightness.High;
            BrightnessLevel.Medium = propertiesConfig.Brightness.Medium;
            BrightnessLevel.Low = propertiesConfig.Brightness.Low;

			PowerIsOnFeedback = new BoolFeedback(() => PowerIsOn);
			CurrentPresetIdFeedback = new IntFeedback(() => CurrentPresetId);
			CurrentPresetNameFeedback = new StringFeedback(() => CurrentPresetName);

            TestPatternIsOnFeedback = new BoolFeedback(() => TestPatternIsOn);
            BrightnessFeedback = new IntFeedback(() => Brightness);
            IsOnlineFeedback = new BoolFeedback(() => IsOnline);

			ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
			ResponseContentFeedback = new StringFeedback(() => ResponseContent);
			ResponseErrorFeedback = new StringFeedback(() => ResponseError);

            RedundancyRoleIsMainFeedback = new BoolFeedback(() => RedundancyRoleIsMain);
            RedundancyRoleIsBackupFeedback = new BoolFeedback(() => RedundancyRoleIsBackup);
            RedundancyRoleIsOfflineFeedback = new BoolFeedback(() => RedundancyRoleIsOffline);

            RedundancyStateIsActiveFeedback = new BoolFeedback(() => RedundancyStateIsActive);
            RedundancyStateIsStandbyFeedback = new BoolFeedback(() => RedundancyStateIsStandby);
            RedundancyStateIsMixedFeedback = new BoolFeedback(() => RedundancyStateIsMixed);

            _presets = propertiesConfig.Presets;
		}

		/// <summary>
		/// Initializes plugin
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

            StartPollTimer();
		}

        private void StartPollTimer()
        {
            _pollTimer = new CTimer((o) => Poll(), null, 15000, 15000);
        }

		private void UpdateFeedbacks()
		{
			//OnlineFeedback.FireUpdate();
			//StatusFeedback.FireUpdate();

			PowerIsOnFeedback.FireUpdate();
			CurrentPresetIdFeedback.FireUpdate();
			CurrentPresetNameFeedback.FireUpdate();

            BrightnessFeedback.FireUpdate();
            TestPatternIsOnFeedback.FireUpdate();
            IsOnlineFeedback.FireUpdate();

			ResponseCodeFeedback.FireUpdate();
			ResponseContentFeedback.FireUpdate();
			ResponseErrorFeedback.FireUpdate();

            RedundancyRoleIsMainFeedback.FireUpdate();
            RedundancyRoleIsBackupFeedback.FireUpdate();
            RedundancyRoleIsOfflineFeedback.FireUpdate();

            RedundancyStateIsActiveFeedback.FireUpdate();
            RedundancyStateIsStandbyFeedback.FireUpdate();
            RedundancyStateIsMixedFeedback.FireUpdate();
		}

		#region Overrides of EssentialsBridgeableDevice

		/// <summary>
		/// Links the plugin device to the EISC bridge
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
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

            trilist.SetSigTrueAction(joinMap.TestPatternOn.JoinNumber, TestPatternOn);
            trilist.SetSigTrueAction(joinMap.TestPatternOff.JoinNumber, TestPatternOff);

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

            IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            TestPatternIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.TestPatternOn.JoinNumber]);
            TestPatternIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.TestPatternOff.JoinNumber]);

            BrightnessFeedback.LinkInputSig(trilist.UShortInput[joinMap.Brightness.JoinNumber]);

			ResponseCodeFeedback.LinkInputSig(trilist.UShortInput[joinMap.ResponseCode.JoinNumber]);
			ResponseContentFeedback.LinkInputSig(trilist.StringInput[joinMap.ResponseContent.JoinNumber]);

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

				UpdateFeedbacks();
			};
		}

		#endregion


		private JToken IsValidJson(string contentString)
		{
			if (string.IsNullOrEmpty(contentString)) return null;

			contentString = contentString.Trim();
			if ((!contentString.StartsWith("{") || !contentString.EndsWith("}")) &&
				(!contentString.StartsWith("[") || !contentString.EndsWith("]"))) return null;

			try
			{
				var jToken = JToken.Parse(contentString);
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson: obj {0}", jToken == null ? "is null" : "is not null");
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

		public void TestOnResponseReceived()
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
			var contentString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

			var sender = new object();
			var args = new GenericClientResponseEventArgs
			{
				Code = 200,
				ContentString = contentString
			};

			OnResponseReceived(sender, args);
		}

		private void OnResponseReceived(object sender, GenericClientResponseEventArgs args)
		{
			try
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this,
					"OnResponseReceived: Code = {0} | ContentString = {1}",
					args.Code, args.ContentString);

				ResponseCode = args.Code;

				if (string.IsNullOrEmpty(args.ContentString))
				{
					Debug.Console(MegapixelHeliosDebug.Notice, this, "OnResponseReceived: args.ContentString is null or empty");
					return;
				}

				ResponseContent = args.ContentString;

				var jToken = IsValidJson(args.ContentString);
				if (jToken == null)
				{
					Debug.Console(MegapixelHeliosDebug.Notice, this, "OnResponseReceived: IsValidJson failed, passing ContentString as string");
					return;
				}


                var feedback = JsonConvert.DeserializeObject<RootDevObject>(ResponseContent);

                if (feedback.Dev.Display != null)
                {

                    if (feedback.Dev.Display.Blackout != null)
                    {
                        PowerIsOn = (bool)feedback.Dev.Display.Blackout;
                    }

                    if (feedback.Dev.Display.Brightness != null)
                    {
                        Brightness = (int)feedback.Dev.Display.Brightness;
                    }

                    if (feedback.Dev.Display.Redundancy != null)
                    {
                        RedundancyRoleIsMain = feedback.Dev.Display.Redundancy.Role == eRedundancyRole.main;
                        RedundancyRoleIsBackup = feedback.Dev.Display.Redundancy.Role == eRedundancyRole.backup;
                        RedundancyRoleIsOffline = feedback.Dev.Display.Redundancy.Role == eRedundancyRole.offline;
                        RedundancyStateIsActive = feedback.Dev.Display.Redundancy.State == eRedundancyState.active;
                        RedundancyStateIsMixed = feedback.Dev.Display.Redundancy.State == eRedundancyState.mixed;
                        RedundancyStateIsStandby = feedback.Dev.Display.Redundancy.State == eRedundancyState.standby;                        
                    }

                }

                if (feedback.Dev.Ingest != null)
                {
                    if (feedback.Dev.Ingest.TestPattern != null)
                    {
                        TestPatternIsOn = feedback.Dev.Ingest.TestPattern.Enabled;
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

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed.
		/// </remarks>
		public void Poll()
		{
			// TODO [ ] Update Poll method as needed for the plugin being developed
			// Example: _client.SendRequest(REQUEST_TYPE, REQUEST_PATH, REQUEST_CONTENT);
            GetRedundancyState(); 
		}

        /// <summary>
        /// Poll for redundancy state.
        /// </summary>
        public void GetRedundancyState()
        {
            _client.SendRequest("GET", "/api/v1/public?dev.display.redundancy", string.Empty);

        }

        /// <summary>
        /// Sets the redundancy role to main and polls for current redundancy role.
        /// </summary>
        public void SetRedundancyRoleToMain()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new Redundancy
                        {
                            Role = eRedundancyRole.main
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
        /// Sets the redundancy role to backup and polls for current redundancy role.
        /// </summary>
        public void SetRedundancyRoleToBackup()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new Redundancy
                        {
                            Role = eRedundancyRole.backup
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
        /// Sets the redundancy role to main and polls for current redundancy role.
        /// </summary>
        public void SetRedundancyRoleToOffline()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new Redundancy
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
        /// Sets the redundancy state to active and polls for current redundancy state.
        /// </summary>
        public void SetRedundancyStateToMain()
        {
            var content = new RootDevObject 
            {
                Dev = new DevObject 
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new Redundancy
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
        /// Sets the redundancy state to standby and polls for current redundancy state.
        /// </summary>
        public void SetRedundancyStateToBackup()
        {
            var content = new RootDevObject
            {
                Dev = new DevObject
                {
                    Display = new DisplayObject
                    {
                        Redundancy = new Redundancy
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
		/// path: "/api/v1/public
		/// content: { "dev": { "display": { "blackout": false } } }
		/// </remarks>
		public void PowerOn()
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

            Debug.Console(MegapixelHeliosDebug.Notice, this, "PowerOn: content-'{0}'", content);
			_client.SendRequest("PATCH", "/api/v1/public", content);
		}

		/// <summary>
		/// Power Off (blackout: true)
		/// </summary>
		/// <remarks>
		/// requestType: PATCH
		/// path: "/api/v1/public"
		/// content: { "dev": { "display": { "blackout": true } } }
		/// </remarks>
		public void PowerOff()
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

			Debug.Console(MegapixelHeliosDebug.Notice, this, "PowerOff: content-'{0}'", content);
			_client.SendRequest("PATCH", "/api/v1/public", content);
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

            Debug.Console(MegapixelHeliosDebug.Notice, this, "SetBrightness: content-'{0}'", content);
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

            Debug.Console(MegapixelHeliosDebug.Notice, this, "TestPatternEnable: content-'{0}'", content);
            _client.SendRequest("PATCH", "/api/v1/public", content);
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

            Debug.Console(MegapixelHeliosDebug.Notice, this, "TestPatternEnable: content-'{0}'", content);
            _client.SendRequest("PATCH", "/api/v1/public", content);
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

			Debug.Console(MegapixelHeliosDebug.Notice, this, "RecallPresetByName: content-'{0}'", content);
			_client.SendRequest("POST", "/api/v1/presets/apply", content);
		}
	}
}

