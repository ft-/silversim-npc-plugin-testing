// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Npc
{
    [ScriptApiName("Npc")]
    [LSLImplementation]
    [Description("OSSL Npc API")]
    public class NpcApi : IScriptApi, IPlugin
    {
        public NpcApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_CREATOR_OWNED = 1;
        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_NOT_OWNED = 2;
        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_SENSE_AS_AGENT = 4;
        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_OBJECT_GROUP = 8;

        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_SIT_NOW = 0;

        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_FLY = 0;
        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_NO_FLY = 1;
        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_LAND_AT_TARGET = 2;
        [APILevel(APIFlags.OSSL)]
        public const int OS_NPC_RUNNING = 4;

        [APILevel(APIFlags.OSSL, "osNpcCreate")]
        public LSLKey NpcCreate(ScriptInstance instance, string firstName, string lastName, Vector3 position, string cloneFrom)
        {
            lock(instance)
            {
                ((Script)instance).CheckThreatLevel("osNpcCreate", Script.ThreatLevelType.High);
            }
            throw new NotImplementedException("osNpcCreate(string, string, vector, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcCreate")]
        public LSLKey NpcCreate(ScriptInstance instance, string firstName, string lastName, Vector3 position, string cloneFrom, int options)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osNpcCreate", Script.ThreatLevelType.High);
            }
            throw new NotImplementedException("osNpcCreate(string, string, vector, string, integer)");
        }

        [APILevel(APIFlags.OSSL, "osNpcGetPos")]
        public Vector3 NpcGetPos(ScriptInstance instance, LSLKey npc)
        {
            throw new NotImplementedException("osNpcGetPos(key)");
        }

        [APILevel(APIFlags.OSSL, "osNpcGetRot")]
        public Quaternion NpcGetRot(ScriptInstance instance, LSLKey npc)
        {
            throw new NotImplementedException("osNpcGetRot(key)");
        }

        [APILevel(APIFlags.OSSL, "osNpcGetOwner")]
        public LSLKey NpcGetOwner(ScriptInstance instance, LSLKey npc)
        {
            throw new NotImplementedException("osNpcGetOwner(key)");
        }

        [APILevel(APIFlags.OSSL, "osNpcLoadAppearance")]
        public void NpcLoadAppearance(ScriptInstance instance, LSLKey npc, string notecard)
        {
            throw new NotImplementedException("osNpcLoadAppearance(key, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcMoveTo")]
        public void NpcMoveTo(ScriptInstance instance, LSLKey npc, Vector3 position)
        {
            throw new NotImplementedException("osNpcMoveTo(key, vector)");
        }

        [APILevel(APIFlags.OSSL, "osNpcMoveToTarget")]
        public void NpcMoveToTarget(ScriptInstance instance, LSLKey npc, Vector3 target, int options)
        {
            throw new NotImplementedException("osNpcMoveToTarget(key, vector, integer)");
        }

        [APILevel(APIFlags.OSSL, "osNpcRemove")]
        public void NpcRemove(ScriptInstance instance, LSLKey npc)
        {
            throw new NotImplementedException("osNpcRemove(key)");
        }

        [APILevel(APIFlags.OSSL, "osNpcSaveAppearance")]
        public LSLKey NpcSaveAppearance(ScriptInstance instance, LSLKey npc, string notecard)
        {
            throw new NotImplementedException("osNpcSaveAppearance(key, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcSay")]
        public void NpcSay(ScriptInstance instance, LSLKey npc, string message)
        {
            throw new NotImplementedException("osNpcSay(key, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcSay")]
        public void NpcSay(ScriptInstance instance, LSLKey npc, int channel, string message)
        {
            throw new NotImplementedException("osNpcSay(key, integer, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcShout")]
        public void NpcShout(ScriptInstance instance, LSLKey npc, string message)
        {
            throw new NotImplementedException("osNpcShout(key, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcShout")]
        public void NpcShout(ScriptInstance instance, LSLKey npc, int channel, string message)
        {
            throw new NotImplementedException("osNpcShout(key, integer, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcWhisper")]
        public void NpcWhisper(ScriptInstance instance, LSLKey npc, string message)
        {
            throw new NotImplementedException("osNpcWhisper(key, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcWhisper")]
        public void NpcWhisper(ScriptInstance instance, LSLKey npc, int channel, string message)
        {
            throw new NotImplementedException("osNpcWhisper(key, integer, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcSetRot")]
        public void NpcSetRot(ScriptInstance instance, LSLKey npc, Quaternion rot)
        {
            throw new NotImplementedException("osNpcSetRot(key, rotation)");
        }

        [APILevel(APIFlags.OSSL, "osNpcSit")]
        public void NpcSit(ScriptInstance instance, LSLKey npc, LSLKey target, int options)
        {
            throw new NotImplementedException("osNpcSit(key, key, integer)");
        }

        [APILevel(APIFlags.OSSL, "osNpcStand")]
        public void NpcStand(ScriptInstance instance, LSLKey npc)
        {
            throw new NotImplementedException("osNpcStand(key)");
        }

        [APILevel(APIFlags.OSSL, "osNpcStopMoveToTarget")]
        public void NpcStopMoveToTarget(ScriptInstance instance, LSLKey npc)
        {
            throw new NotImplementedException("osNpcStopMoveToTarget(key)");
        }

        [APILevel(APIFlags.OSSL, "osIsNpc")]
        public int IsNpc(ScriptInstance instance, LSLKey npc)
        {
            lock(instance)
            {
                IAgent agent;
                if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(npc.AsUUID, out agent))
                {
                    return agent.IsNpc.ToLSLBoolean();
                }
            }
            return 0;
        }

        [APILevel(APIFlags.OSSL, "osNpcTouch")]
        public void NpcTouch(ScriptInstance instance, LSLKey npc, LSLKey objectKey, int linkNum)
        {
            throw new NotImplementedException("osNpcTouch(key, key, integer)");
        }
    }
}
