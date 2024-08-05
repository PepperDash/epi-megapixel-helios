﻿
using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;
using MegapixelHelios.GenericClient;
using MegapixelHelios.JsonObjects;
using MegapixelHelios.JsonObjects.Blackout;
using MegapixelHelios.Parameters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;


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

		public BoolFeedback PowerIsOnFeedback { get; set; }
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

        public IntFeedback BrightnessFeedback { get; set; }

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

        private List<MegaPixelHeliosPresetConfig> _presets;

		public StringFeedback CurrentPresetNameFeedback { get; set; }

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

            //BrightnessSetting = new Brightness();



            BrightnessLevel.High = propertiesConfig.Brightness.High;
            BrightnessLevel.Medium = propertiesConfig.Brightness.Medium;
            BrightnessLevel.Low = propertiesConfig.Brightness.Low;

			PowerIsOnFeedback = new BoolFeedback(() => PowerIsOn);
            TestPatternIsOnFeedback = new BoolFeedback(() => TestPatternIsOn);
			CurrentPresetIdFeedback = new IntFeedback(() => CurrentPresetId);
			CurrentPresetNameFeedback = new StringFeedback(() => CurrentPresetName);
            IsOnlineFeedback = new BoolFeedback(() => IsOnline);

            BrightnessFeedback = new IntFeedback(() => Brightness);

			ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
			ResponseContentFeedback = new StringFeedback(() => ResponseContent);
			ResponseErrorFeedback = new StringFeedback(() => ResponseError);

            _presets = propertiesConfig.Presets;
		}

		/// <summary>
		/// Initializes plugin
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();
		}

		private void UpdateFeedbacks()
		{
			//OnlineFeedback.FireUpdate();
			//StatusFeedback.FireUpdate();

			PowerIsOnFeedback.FireUpdate();
			CurrentPresetIdFeedback.FireUpdate();
			CurrentPresetNameFeedback.FireUpdate();

			ResponseCodeFeedback.FireUpdate();
			ResponseContentFeedback.FireUpdate();
			ResponseErrorFeedback.FireUpdate();
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
			Debug.Console(MegapixelHeliosDebug.Trace, "Linking to Bridge Type {0}", GetType().Name);

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

			trilist.SetUShortSigAction(joinMap.RecallPresetById.JoinNumber, a => RecallPresetById(a));
			trilist.SetStringSigAction(joinMap.RecallPresetByName.JoinNumber, RecallPresetByName);

            trilist.SetUShortSigAction(joinMap.Brightness.JoinNumber, a => SetBrightness(a));

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

        /*
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
        */
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

				ProcessJToken(jToken, "dev.display.blackout");
                ProcessJToken(jToken, "dev.display.brightness");
                ProcessJToken(jToken, "dev.ingest.testPattern.enabled");
				//ProcessJToken(jToken, "presetName");
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Exception Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Inner Exception {0}", ex.InnerException);
			}
		}

		// process JToken
		private void ProcessJToken(JToken jToken, string path)
		{
			var token = jToken.SelectToken(path);
			if (token == null)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessJToken: selectToken '{0}' failed", path);
				return;
			}

			Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessJToken: '{0}' token.Type is '{1}'", path, token.Type.ToString());

			try
			{
				switch (token.Type)
				{
					case JTokenType.Object:
						{
							// [ ] TODO - update to process JSON objects
							break;
						}
					case JTokenType.Array:
						{
							// [ ] TODO - update to process JSON arrays
							break;
						}
					case JTokenType.Boolean:
						{
							// [ ] TODO - update to process JSON objects
							if (path == "dev.display.blackout")
							{
								Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken: '{0}' = {1}", path, token.Value<bool>());
								PowerIsOn = token.Value<bool>();
							}
                            if (path == "dev.ingest.testPattern.enabled")
                            {
                                Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken: '{0}' = {1}", path, token.Value<bool>());
                                TestPatternIsOn = token.Value<bool>();
                            }

							break;
						}
                    case JTokenType.Integer:
                        {
                            if (path == "dev.display.brightness")
                            {
                                Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken: '{0}' = {1}", path, token.Value<int>());
                                Brightness = token.Value<int>();
                            }

                            break;

                        }
					default:
						{
							Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessJToken: unhandled JTokenType '{0}'", token.Type.ToString());
							break;
						}
				}
			}
			catch (JsonSerializationException jex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessJToken Exception Message: {0}", jex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken Stack Trace: {0}", jex.StackTrace);
				if (jex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken Inner Exception {0}", jex.InnerException);
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessJToken Exception Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessJToken Inner Exception {0}", ex.InnerException);
			}
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			// TODO [ ] Update Poll method as needed for the plugin being developed
			// Example: _client.SendRequest(REQUEST_TYPE, REQUEST_PATH, REQUEST_CONTENT);
			throw new System.NotImplementedException();
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
            var jsonObject = new MegapixelHelios.JsonObjects.Blackout.RootDevObject
			{
                Dev = new MegapixelHelios.JsonObjects.Blackout.DevObject
				{
                    Display = new MegapixelHelios.JsonObjects.Blackout.DisplayObject
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

			Debug.Console(MegapixelHeliosDebug.Trace, this, "PowerOn: content-'{0}'", content);
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
            var jsonObject = new MegapixelHelios.JsonObjects.Blackout.RootDevObject
			{
                Dev = new MegapixelHelios.JsonObjects.Blackout.DevObject
				{
                    Display = new MegapixelHelios.JsonObjects.Blackout.DisplayObject
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

			Debug.Console(MegapixelHeliosDebug.Trace, this, "PowerOff: content-'{0}'", content);
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
                Debug.Console(MegapixelHeliosDebug.Notice, "SetBrightness: Value sent {0} out of range",brightness);
                return;
            }

            var jsonObject = new MegapixelHelios.JsonObjects.Brightness.RootDevObject
            {
                Dev = new MegapixelHelios.JsonObjects.Brightness.DevObject
                {
                    Display = new MegapixelHelios.JsonObjects.Brightness.DisplayObject
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

            Debug.Console(MegapixelHeliosDebug.Trace, this, "SetBrightness: content-'{0}'", content);
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
            var jsonObject = new MegapixelHelios.JsonObjects.TestPattern.RootDevObject
            {
                Dev = new MegapixelHelios.JsonObjects.TestPattern.DevObject
                {
                    Ingest = new MegapixelHelios.JsonObjects.TestPattern.IngestObject
                    {
                        TestPattern = new MegapixelHelios.JsonObjects.TestPattern.TestPatternObject
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

            Debug.Console(MegapixelHeliosDebug.Trace, this, "TestPatternEnable: content-'{0}'", content);
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
            var jsonObject = new MegapixelHelios.JsonObjects.TestPattern.RootDevObject
            {
                Dev = new MegapixelHelios.JsonObjects.TestPattern.DevObject
                {
                    Ingest = new MegapixelHelios.JsonObjects.TestPattern.IngestObject
                    {
                        TestPattern = new MegapixelHelios.JsonObjects.TestPattern.TestPatternObject
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

            Debug.Console(MegapixelHeliosDebug.Trace, this, "TestPatternEnable: content-'{0}'", content);
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

			Debug.Console(MegapixelHeliosDebug.Trace, this, "RecallPresetByName: content-'{0}'", content);
			_client.SendRequest("POST", "/api/v1/presets/apply", content);
		}
	}
}

