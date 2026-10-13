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
			MinimumEssentialsFrameworkVersion = "1.16.0";

			TypeNames = new List<string>() { "megapixelHelios" };
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			Debug.Console(MegapixelHeliosDebug.Notice, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

			// get the plugin device properties configuration object & check for null
			var propertiesConfig = dc.Properties.ToObject<MegapixelHeliosConfig>();
			if (propertiesConfig == null)
			{
				Debug.Console(MegapixelHeliosDebug.Trace, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
				return null;
			}

            return new MegapixelHeliosDevice(dc.Key, dc.Name, propertiesConfig);
        }
	}
}

