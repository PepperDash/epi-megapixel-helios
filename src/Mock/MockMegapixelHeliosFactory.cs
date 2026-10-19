using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Builds <see cref="MockMegapixelHeliosDevice"/> (type <c>mockMegapixelHelios</c>). Takes the same properties as
    /// <c>megapixelHelios</c> (the <c>control</c> block is not needed) plus <c>mockRedundancyRole</c>
    /// (<c>main</c> or <c>backup</c>) to say which processor of a pair it stands in for.
    /// </summary>
    public class MockMegapixelHeliosFactory : EssentialsPluginDeviceFactory<MockMegapixelHeliosDevice>
    {
        public MockMegapixelHeliosFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";
            TypeNames = new List<string> { "mockMegapixelHelios" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogDebug("[{Key}] Factory Attempting to create new device from type: {Type}", dc.Key, dc.Type);

            var props = dc.Properties?.ToObject<MegapixelHeliosPropertiesConfig>() ?? new MegapixelHeliosPropertiesConfig();
            var role = (dc.Properties as JObject)?["mockRedundancyRole"]?.ToString();

            return new MockMegapixelHeliosDevice(dc.Key, dc.Name, props, role);
        }
    }
}
