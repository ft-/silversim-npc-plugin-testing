// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Npc;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Npc
{
    [ScriptApiName("Npc")]
    [LSLImplementation]
    [Description("OSSL Npc API")]
    public class NpcApi : IScriptApi, IPlugin
    {
        NpcManager m_NpcManager;
        public NpcApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            List<NpcManager> npcManagers = loader.GetServicesByValue<NpcManager>();
            if(npcManagers.Count == 0)
            {
                throw new ConfigurationLoader.ConfigurationErrorException("No NPC manager configured");
            }
            m_NpcManager = npcManagers[0];
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
            return NpcCreate(instance, firstName, lastName, position, cloneFrom, 0);
        }

        [APILevel(APIFlags.OSSL, "osNpcCreate")]
        public LSLKey NpcCreate(ScriptInstance instance, string firstName, string lastName, Vector3 position, string cloneFrom, int options)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osNpcCreate", Script.ThreatLevelType.High);

                ObjectPart part = instance.Part;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPartInventoryItem resitem;
                if (!part.Inventory.TryGetValue(cloneFrom, out resitem))
                {
                    instance.ShoutError("Inventory item not found");
                }
                else if(resitem.AssetType != AssetType.Notecard)
                {
                    instance.ShoutError("Inventory item not a notecard");
                }
                AssetData data = scene.AssetService[resitem.ID];
                Notecard nc = new Notecard(data);
                UGI group = (options & OS_NPC_OBJECT_GROUP) != 0 ? part.Group : UGI.Unknown;
                NpcOptions npcOptions = NpcOptions.None;
                if((options & OS_NPC_SENSE_AS_AGENT) != 0)
                {
                    npcOptions |= NpcOptions.SenseAsAgent;
                }
                NpcAgent agent = m_NpcManager.CreateNpc(scene.ID, part.Owner, group, firstName, lastName, position, nc, npcOptions);
                return agent.ID;
            }
        }

        bool TryGetNpc(ScriptInstance instance, UUID npc, out NpcAgent agent)
        {
            ObjectPart part = instance.Part;

            if (!m_NpcManager.TryGetNpc(npc.AsUUID, out agent))
            {
                instance.ShoutError("NPC not found");
                return false;
            }
            else if (agent.NpcOwner != UUI.Unknown && agent.NpcOwner != part.Owner)
            {
                instance.ShoutError("NPC not owned by you");
                return false;
            }
            else
            {
                return true;
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcGetPos")]
        public Vector3 NpcGetPos(ScriptInstance instance, LSLKey npc)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (!TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    return Vector3.Zero;
                }
                else
                {
                    return npcAgent.GlobalPosition;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcGetRot")]
        public Quaternion NpcGetRot(ScriptInstance instance, LSLKey npc)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (!TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    return Quaternion.Identity;
                }
                else
                {
                    return npcAgent.GlobalRotation;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcGetOwner")]
        public LSLKey NpcGetOwner(ScriptInstance instance, LSLKey npc)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (!m_NpcManager.TryGetNpc(npc.AsUUID, out npcAgent))
                {
                    return UUID.Zero;
                }
                else
                {
                    return npcAgent.NpcOwner.ID;
                }
            }
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
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    m_NpcManager.RemoveNpc(npcAgent.ID);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSaveAppearance")]
        public LSLKey NpcSaveAppearance(ScriptInstance instance, LSLKey npc, string notecard)
        {
            throw new NotImplementedException("osNpcSaveAppearance(key, string)");
        }

        [APILevel(APIFlags.OSSL, "osNpcSay")]
        public void NpcSay(ScriptInstance instance, LSLKey npc, string message)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoSay(message);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSay")]
        public void NpcSay(ScriptInstance instance, LSLKey npc, int channel, string message)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoSay(channel, message);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcShout")]
        public void NpcShout(ScriptInstance instance, LSLKey npc, string message)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoShout(message);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcShout")]
        public void NpcShout(ScriptInstance instance, LSLKey npc, int channel, string message)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoShout(channel, message);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcWhisper")]
        public void NpcWhisper(ScriptInstance instance, LSLKey npc, string message)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoWhisper(message);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcWhisper")]
        public void NpcWhisper(ScriptInstance instance, LSLKey npc, int channel, string message)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoWhisper(channel, message);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSetRot")]
        public void NpcSetRot(ScriptInstance instance, LSLKey npc, Quaternion rot)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.GlobalRotation = rot;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSit")]
        public void NpcSit(ScriptInstance instance, LSLKey npc, LSLKey target, int options)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
#warning options not handled yet
                    npcAgent.DoSit(target.AsUUID);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcStand")]
        public void NpcStand(ScriptInstance instance, LSLKey npc)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.UnSit();
                }
            }
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
            NpcAgent npcAgent;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    npcAgent.DoTouch(objectKey, linkNum);
                }
            }
        }
    }
}
