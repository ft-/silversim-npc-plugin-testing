// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;

namespace SilverSim.Scripting.Lsl.Api.NpcSensor
{
    [PluginName("LSL_NpcSensor")]
    public sealed class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new NpcSensorApi();
        }
    }
}
