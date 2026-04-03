using System;
using System.Globalization;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;

namespace MegapixelHelios.GenericClient
{
	/// <summary>
	/// Http client
	/// </summary>
	public class GenericClientHttps : IKeyed, IRestfulComms
	{
		private static readonly string Separator = new String('-', 50);

        private readonly object @lock = new object();
		private readonly HttpsClient _client;

        public bool DispatchError { get; private set; }
		public string Host { get; private set; }
		public int Port { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }
		public string AuthorizationBase64 { get; set; }

        /// <summary>
        /// Client response event
        /// </summary>
        public event EventHandler<GenericClientResponseEventArgs> ResponseReceived;
        /// <summary>
        /// Client dispatch error OnResponse event
        /// </summary>
        public event EventHandler<GenericClientDispatchErrorOnReceivedEventArgs> DispatchErrorOnReceived;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="controlConfig"></param>
		public GenericClientHttps(string key, ControlPropertiesConfig controlConfig)
		{
			if (string.IsNullOrEmpty(key) || controlConfig == null)
			{
				Debug.Console(MegapixelHeliosDebug.Verbose, Debug.ErrorLogLevel.Error,
					"GenericClient key or host is null or empty, failed to create client for {0}", key);
				return;
			}

			Key = string.Format("{0}-client", key).ToLower();

			Host = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
				? String.Format("https://{0}:{1}",
					controlConfig.TcpSshProperties.Address.Replace("https://", ""),
					controlConfig.TcpSshProperties.Port)
				: String.Format("https://{0}",
					controlConfig.TcpSshProperties.Address.Replace("https://", ""));
			Port = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
				? controlConfig.TcpSshProperties.Port
				: 443;
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

			_client = new HttpsClient
			{
				UserName = Username,
				Password = Password,
				KeepAlive = true,
				HostVerification = false,
				PeerVerification = false
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
			var request = new HttpsClientRequest
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

			Debug.Console(MegapixelHeliosDebug.Verbose, this, @"
{0}
>>>>> SendRequest
url: {1}
content: {2}
requestType: {3}
{0}", Separator, request.Url, request.ContentString, request.RequestType);

            CMonitor.Enter(@lock);
            try
            {
                var response = _client.Dispatch(request);
                var json = "";

                using (var sr = new StreamReader(response.ContentStream))
                {
                    json = sr.ReadToEnd();
                }

                Debug.Console(1, "Received a response with length: {0} code: {1}", json.Length, response.Code);

                var handler = ResponseReceived;
                if (handler == null) return;
                handler(this, new GenericClientResponseEventArgs { Code = response.Code, ContentString = json });

                var errorHandler = DispatchErrorOnReceived;
                if (handler == null) return;
                errorHandler(this, new GenericClientDispatchErrorOnReceivedEventArgs { errorState = false });
            }
            catch (HttpsException ex)
            {
                Debug.Console(1, this, "Caught an exception dispatching a request: {0}", ex);

                var handler = ResponseReceived;
                if (handler == null) return;
                handler(this, new GenericClientResponseEventArgs { Code = ex.Response.Code, ContentString = string.Empty });

                var errorHandler = DispatchErrorOnReceived;
                if (handler == null) return;
                errorHandler(this, new GenericClientDispatchErrorOnReceivedEventArgs { errorState = true });
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Caught an exception dispatching a request: {0}", ex);

                var handler = ResponseReceived;
                if (handler == null) return;
                handler(this, new GenericClientResponseEventArgs { Code = 500, ContentString = string.Empty });

                var errorHandler = DispatchErrorOnReceived;
                if (handler == null) return;
                errorHandler(this, new GenericClientDispatchErrorOnReceivedEventArgs { errorState = true });
            }
            finally
            {
                CMonitor.Exit(@lock);
            };
		}

		#endregion

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