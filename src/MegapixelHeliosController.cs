﻿
using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;
using MegapixelHelios.GenericClient;
using MegapixelHelios.JsonObjects;
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

		public BoolFeedback PowerIsOnFeedback { get; set; }


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

		private Dictionary<string, MegaPixelHeliosPresetConfig> _presetsConfig; 

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

			PowerIsOnFeedback = new BoolFeedback(() => PowerIsOn);
			CurrentPresetIdFeedback = new IntFeedback(()=> CurrentPresetId);
			CurrentPresetNameFeedback = new StringFeedback(() => CurrentPresetName);

			ResponseCodeFeedback = new IntFeedback(() => ResponseCode);
			ResponseContentFeedback = new StringFeedback(() => ResponseContent);
			ResponseErrorFeedback = new StringFeedback(() => ResponseError);
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

			trilist.SetUShortSigAction(joinMap.RecallPresetById.JoinNumber, a => RecallPresetById(a));
			trilist.SetStringSigAction(joinMap.RecallPresetByName.JoinNumber, RecallPresetByName);

			PowerIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);
			PowerIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOff.JoinNumber]);

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

				var jToken = IsValidJson(args.ContentString);
				if (jToken == null)
				{
					Debug.Console(MegapixelHeliosDebug.Notice, this, "OnResponseReceived: IsValidJson failed, passing ContentString as string");
					ResponseContent = args.ContentString;
					return;
				}

				ProcessDataToken(jToken);
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Exception Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "OnResponseReceived Inner Exception {0}", ex.InnerException);
			}
		}

		// process data token
		private void ProcessDataToken(JToken jToken)
		{
			var dataToken = jToken.SelectToken("data");
			if (dataToken == null)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessDataToken: selectToken 'data' failed");
				return;
			}

			Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessDataToken: dataToken.Type is '{0}'", dataToken.Type.ToString());

			try
			{
				switch (dataToken.Type)
				{
					case JTokenType.Array:
						{
							//var dataList = dataToken.ToObject<List<PadmData>>();
							//ProcessDataArray(dataList);

							break;
						}
					case JTokenType.Object:
						{
							//var dataObj = dataToken.ToObject<PadmRequest>();
							//ProcessDataObject(dataObj);

							//CrestronInvoke.BeginInvoke(o =>
							//{
							//    var timer = new CTimer(g => GetLoadGroups(), 5000);
							//});

							break;
						}
					default:
						{
							Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessDataToken: unhandled JTokenType '{0}'", dataToken.Type.ToString());
							break;
						}
				}
			}
			catch (JsonSerializationException jex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessDataToken Exception Message: {0}", jex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessDataToken Stack Trace: {0}", jex.StackTrace);
				if (jex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessDataToken Inner Exception {0}", jex.InnerException);
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessDataToken Exception Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessDataToken Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, "ProcessDataToken Inner Exception {0}", ex.InnerException);
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
		public void PowerOn()
		{
			// PATCH /api/v1/public
			// CONTENT: { "dev": { "display": { "blackout": false } } }
			var content = JsonConvert.SerializeObject(new MegapixelHeliosRestRequests.SetBlackoutState(false));
			if (string.IsNullOrEmpty(content))
			{
				Debug.Console(MegapixelHeliosDebug.Notice, "PowerOn: failed to serialzie request content");
				return;
			}

			_client.SendRequest("PATCH", "/api/v1/public", content);
		}

		/// <summary>
		/// Power Off (blackout: true)
		/// </summary>
		public void PowerOff()
		{
			// PATCH /api/v1/public
			// CONTENT: { "dev": { "display": { "blackout": true } } }
			var content = JsonConvert.SerializeObject(new MegapixelHeliosRestRequests.SetBlackoutState(true));
			if (string.IsNullOrEmpty(content))
			{
				Debug.Console(MegapixelHeliosDebug.Notice, "PowerOff: failed to serialzie request content");
				return;
			}

			_client.SendRequest("PATCH", "/api/v1/public", content);
		}

		/// <summary>
		/// Prints the configured presets to console
		/// </summary>
		public void PrintPresetsConfig()
		{
			if (_presetsConfig == null)
			{
				Debug.Console(MegapixelHeliosDebug.Trace, this, "Presets configuration is null");
				return;
			}

			foreach(var p in _presetsConfig)
				Debug.Console(MegapixelHeliosDebug.Trace, this, 
					"Preset: key-'{0}', label-'{1}', presetId-'{2}', presetName-'{3}'",
					p.Key, p.Value.Label, p.Value.PreseId, p.Value.PresetName);
		}

		/// <summary>
		/// Queries device for preset list 
		/// </summary>
		public void GetPresetsList()
		{
			// GET /api/v1/presets/list
			_client.SendRequest("GET", "/api/v1/presets/list", string.Empty);
		}

		/// <summary>
		/// Recalls preset using device configured presetId
		/// </summary>
		/// <param name="id"></param>
		public void RecallPresetById(uint id)
		{
			if (id == 0) return;

			// POST /api/v1/presets/{id}/apply
			_client.SendRequest("POST", string.Format("/api/v1/presets/{0}/apply", id), string.Empty);
		}

		/// <summary>
		/// Recalls preset using device configured presetName
		/// </summary>
		/// <param name="name"></param>
		public void RecallPresetByName(string name)
		{
			// POST /api/v1/presets/apply
			// CONTENT: { "presetName": "{name}" }
			var content = JsonConvert.SerializeObject(new MegapixelHeliosRestRequests.RecallPresetByName(name));
			if (string.IsNullOrEmpty(content))
			{
				Debug.Console(MegapixelHeliosDebug.Notice, "RecallPresetByName: failed to serialzie request content");
				return;
			}

			_client.SendRequest("POST", "/api/v1/presets/apply", content);
		}
	}
}
