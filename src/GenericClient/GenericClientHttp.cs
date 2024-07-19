using System;
using System.Globalization;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using RequestType = Crestron.SimplSharp.Net.Http.RequestType;

namespace MegapixelHelios.GenericClient
{
	/// <summary>
	/// Http client
	/// </summary>
	public class GenericClientHttp : IRestfulComms
	{
		private static readonly string Separator = new String('-', 50);

		private readonly HttpClient _client;
		private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);

		public string Host { get; private set; }
		public int Port { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }
		public string AuthorizationBase64 { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="controlConfig"></param>
		public GenericClientHttp(string key, ControlPropertiesConfig controlConfig)
		{
			if (string.IsNullOrEmpty(key) || controlConfig == null)
			{
				Debug.Console(MegapixelHeliosDebug.Verbose, Debug.ErrorLogLevel.Error,
					"GenericClient key or host is null or empty, failed to create client for {0}", key);
				return;
			}

			Key = string.Format("{0}-client", key).ToLower();

			Host = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
				? String.Format("http://{0}:{1}",
					controlConfig.TcpSshProperties.Address.Replace("http://", ""),
					controlConfig.TcpSshProperties.Port)
				: String.Format("http://{0}",
					controlConfig.TcpSshProperties.Address.Replace("http://", ""));
			Port = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
				? controlConfig.TcpSshProperties.Port
				: 80;
			Username = controlConfig.TcpSshProperties.Username ?? "";
			Password = controlConfig.TcpSshProperties.Password ?? "";

			AuthorizationBase64 = EncodeBase64(Username, Password);

			Debug.Console(MegapixelHeliosDebug.Verbose, this, @"
{0}
>>>>> GenericClientHttps: 
Key = {1}
Host = {2}
Port = {3}
Username = {4}
Password = {5}
{0}", Separator, Key, Host, Port, Username, Password);

			_client = new HttpClient
			{
				UserName = Username,
				Password = Password,
				KeepAlive = false
			};

			DeviceManager.AddDevice(this);
		}

		#region IRestfulComms Members

		/// <summary>
		/// Implements IKeyed interface
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// Sends the request with the provided parameters
		/// </summary>
		/// <param name="requestType"></param>
		/// <param name="path"></param>
		/// <param name="content"></param>
		public void SendRequest(string requestType, string path, string content)
		{
			var reqType = (RequestType)Enum.Parse(typeof(RequestType), requestType, true);
			SendRequest(reqType, path, content);
		}

		/// <summary>
		/// Sends the request with the provided parameters
		/// </summary>
		/// <param name="requestType"></param>
		/// <param name="path"></param>
		/// <param name="content"></param>
		public void SendRequest(RequestType requestType, string path, string content)
		{
			var request = new HttpClientRequest
			{
				RequestType = requestType,
				Url = new UrlParser(String.Format("{0}/{1}", Host, path.TrimStart('/'))),
				ContentString = content
			};

			request.Header.SetHeaderValue("Content-Type", "application/json");
			request.Header.SetHeaderValue("Content-Length", content.Length.ToString(CultureInfo.InvariantCulture));

			if (!string.IsNullOrEmpty(AuthorizationBase64))
			{
				request.Header.SetHeaderValue("Authorization", AuthorizationBase64);
			}

			Debug.Console(MegapixelHeliosDebug.Notice, this, @"
{0}
>>>>> SendRequest
url: {1}
content: {2}
requestType: {3}
{0}", Separator, request.Url, request.ContentString, request.RequestType);

			if (_client.ProcessBusy)
				_requestQueue.Enqueue(() => RequestDispatch(request));
			else
				RequestDispatch(request);
		}

		// dispatches the recieved request
		private void RequestDispatch(HttpClientRequest request)
		{
			_client.DispatchAsync(request, (response, error) =>
			{
				if (response == null)
				{
					Debug.Console(MegapixelHeliosDebug.Notice, this, @"
{0}
>>>>> RequestDispatch
error: {1}
{0}", Separator, error);
					return;
				}

				OnResponseRecieved(new GenericClientResponseEventArgs(response.Code, response.ContentString));
			});
		}

		/// <summary>
		/// Client response event
		/// </summary>
		public event EventHandler<GenericClientResponseEventArgs> ResponseReceived;

		// client response event handler
		private void OnResponseRecieved(GenericClientResponseEventArgs args)
		{

			Debug.Console(MegapixelHeliosDebug.Notice, this, @"
{0}
>>>>> OnResponseReceived: 
args.Code = {1}
args.ContentString = {2}
{0}", Separator, args.Code, args.ContentString);

			CheckRequestQueue();


			if (args.Code != 200)
				ProcessErrorResponse(args);
			else
				ProcessSuccessResponse(args);
		}

		private void ProcessSuccessResponse(GenericClientResponseEventArgs args)
		{
			var jToken = IsValidJson(args.ContentString);
			if (jToken == null)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessSuccessResponse: IsValidJson obj is null");
				return;
			}

			// pass the response to the consuming class
			var handler = ResponseReceived;
			if (handler == null) return;

			handler(this, args);
		}

		private void ProcessErrorResponse(GenericClientResponseEventArgs args)
		{
			var jToken = IsValidJson(args.ContentString);
			if (jToken == null)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "ProcessErrorResponse: IsValidJson obj is null");
				return;
			}

			var errorArray = jToken.SelectToken("errors");
			if (errorArray == null) return;

			// pass the response to the consuming class
			var handler = ResponseReceived;
			if (handler == null) return;

			handler(this, args);
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
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson JsonReaderException.Message: {0}", jex.Message);
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson JsonReaderException.StackTrace: {0}", jex.StackTrace);
				if (jex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson JsonReaderException.InnerException: {0}", jex.InnerException);

				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson Exception.Message: {0}", ex.Message);
				Debug.Console(MegapixelHeliosDebug.Notice, this, "IsValidJson Exception.StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(MegapixelHeliosDebug.Verbose, this, "IsValidJson Exception.InnerException: {0}", ex.InnerException);

				return null;
			}
		}

		// Checks request queue and issues next request
		private void CheckRequestQueue()
		{
			Debug.Console(MegapixelHeliosDebug.Verbose, this, "CheckRequestQueue: _requestQueue.Count = {0}", _requestQueue.Count);
			var nextRequest = _requestQueue.TryToDequeue();
			Debug.Console(MegapixelHeliosDebug.Verbose, this, "CheckRequestQueue: _requestQueue.TryToDequeue was {0}",
				(nextRequest == null) ? "unsuccessful" : "successful");
			if (nextRequest != null)
			{
				nextRequest();
			}
		}

		// encodes username and password, returning a Base64 encoded string
		private string EncodeBase64(string username, string password)
		{
			if (string.IsNullOrEmpty(username))
			{
				return "";
			}

			try
			{
				var base64String =
					Convert.ToBase64String(
						Encoding.GetEncoding("ISO-8859-1")
							.GetBytes(string.Format("{0}:{1}", username, password)));
				return string.Format("Basic {0}", base64String);
			}
			catch (Exception err)
			{
				Debug.Console(MegapixelHeliosDebug.Verbose, this, Debug.ErrorLogLevel.Error, "EncodeBase64 Exception:\r{0}", err);
				return "";
			}
		}
	}
}