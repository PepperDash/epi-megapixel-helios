using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using MegapixelHelios.GenericClient;

namespace MegapixelHelios
{
	/// <summary>
	/// Plugin device factory for devices that use IBasicCommunication
	/// </summary>
	public class MegapixelHelioFactory : EssentialsPluginDeviceFactory<MegapixelHeliosController>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		public MegapixelHelioFactory()
		{
			MinimumEssentialsFrameworkVersion = "1.16.0";

			TypeNames = new List<string>() { "megapixelhelios" };
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			Debug.Console(1, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

			// get the plugin device properties configuration object & check for null 
			var propertiesConfig = dc.Properties.ToObject<MegapixelHeliosPropertiesConfig>();
			if (propertiesConfig == null)
			{
				Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
				return null;
			}

			IRestfulComms client;

			switch (propertiesConfig.Control.Method)
			{
				case eControlMethod.Http:
					{
						Debug.Console(MegapixelHeliosDebug.Notice, "[{0}] buidling {1} client",
							dc.Key, propertiesConfig.Control.Method);

						client = new GenericClientHttp(string.Format("{0}-http", dc.Key), propertiesConfig.Control);
						break;
					}
				case eControlMethod.Https:
					{
						Debug.Console(MegapixelHeliosDebug.Notice, "[{0}] buidling {1} client",
							dc.Key, propertiesConfig.Control.Method);

						client = new GenericClientHttps(string.Format("{0}-https", dc.Key), propertiesConfig.Control);
						break;
					}
				default:
					{
						Debug.Console(MegapixelHeliosDebug.Trace, "[{0}] control method {1} not supported, check configuration and update to HTTP or HTTPS",
							dc.Key, propertiesConfig.Control.Method);

						client = null;
						break;
					}
			}

			if(client != null) return new MegapixelHeliosController(dc.Key, dc.Name, propertiesConfig, client);

			Debug.Console(MegapixelHeliosDebug.Trace, "[{0}] Factory Notice: No control object present for device {1}", dc.Key, dc.Name);
			return null;
		}
	}
}

