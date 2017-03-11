// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.NpcSensor.ScriptEvents
{
    [TranslatedScriptEvent("npcsensor")]
    public class NpcSensorEvent : IScriptDetectedEvent
    {
        [TranslatedScriptEventParameter(0)]
        public LSLKey NpcId;
        public List<DetectInfo> Detected { get; set; }
        [TranslatedScriptEventParameter(1)]
        public int NumDetected { get { return Detected.Count; } }
    }
}
