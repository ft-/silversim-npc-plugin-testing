// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.NpcSensor.ScriptEvents
{
    [TranslatedScriptEvent("no_npcsensor")]
    public class NpcNoSensorEvent : IScriptEvent
    {
        [TranslatedScriptEventParameter(0)]
        public LSLKey NpcId;
    }
}
