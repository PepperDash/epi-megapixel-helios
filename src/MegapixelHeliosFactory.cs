using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Plugin device factory for devices that use IBasicCommunication
	/// </summary>
	public class MegapixelHeliosFactory : EssentialsPluginDeviceFactory<MegapixelHeliosDevice>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		public MegapixelHeliosFactory()
		{
			MinimumEssentialsFrameworkVersion = "3.0.0";

			TypeNames = new List<string>() { "megapixelHelios" };
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			Debug.LogDebug("[{Key}] Factory Attempting to create new device from type: {Type}", dc.Key, dc.Type);

			// get the plugin device properties configuration object & check for null
			var propertiesConfig = dc.Properties.ToObject<MegapixelHeliosPropertiesConfig>();
			if (propertiesConfig == null)
			{
				Debug.LogError("[{Key}] Factory: failed to read properties config for {Name}", dc.Key, dc.Name);
				return null;
			}

            if (propertiesConfig.Control == null)
            {
                Debug.LogError("[{Key}] Factory: control configuration is missing", dc.Key);
                return null;
            }

            if (propertiesConfig.Control.Method != eControlMethod.Http &&
                propertiesConfig.Control.Method != eControlMethod.Https)
            {
                Debug.LogError("[{Key}] Factory: control method {Method} is not supported; use HTTP or HTTPS", dc.Key, propertiesConfig.Control.Method);
                return null;
            }

            return new MegapixelHeliosDevice(dc.Key, dc.Name, propertiesConfig);
        }
	}
}

