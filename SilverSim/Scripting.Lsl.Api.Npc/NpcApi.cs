// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.Profile;
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
        [APILevel(APIFlags.ASSL, "npcCreate")]
        [ThreatLevelRequired(ThreatLevel.High, "osNpcCreate")]
        public LSLKey NpcCreate(ScriptInstance instance, string firstName, string lastName, Vector3 position, string cloneFrom)
        {
            return NpcCreate(instance, firstName, lastName, position, cloneFrom, 0);
        }

        [APILevel(APIFlags.OSSL, "osNpcCreate")]
        [APILevel(APIFlags.ASSL, "npcCreate")]
        [ThreatLevelRequired(ThreatLevel.High, "osNpcCreate")]
        public LSLKey NpcCreate(ScriptInstance instance, string firstName, string lastName, Vector3 position, string cloneFrom, int options)
        {
            lock (instance)
            {
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
        [APILevel(APIFlags.ASSL, "npcGetPos")]
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
        [APILevel(APIFlags.ASSL, "npcGetRot")]
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
        [APILevel(APIFlags.ASSL, "npcGetOwner")]
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
        [APILevel(APIFlags.ASSL, "npcLoadAppearance")]
        public void NpcLoadAppearance(ScriptInstance instance, LSLKey npc, string notecard)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                if (!TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    return;
                }

                ObjectPart part = instance.Part;
                ObjectPartInventoryItem item;
                AssetData asset;
                
                if(!part.Inventory.TryGetValue(notecard, out item))
                {
                    instance.ShoutError("Inventory item \"" + notecard + "\" not found");
                }
                else if(item.AssetType != AssetType.Notecard)
                {
                    instance.ShoutError("Inventory item \"" + notecard + "\" not a Notecard");
                }
                else if(!npcAgent.AssetService.TryGetValue(item.AssetID, out asset))
                {
                    instance.ShoutError("Could not load asset for inventory item \"" + notecard + "\"");
                }
                else
                {
                    npcAgent.LoadAppearanceFromNotecard(new Notecard(asset));
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcMoveTo")]
        [APILevel(APIFlags.ASSL, "npcMoveTo")]
        public void NpcMoveTo(ScriptInstance instance, LSLKey npc, Vector3 position)
        {
            throw new NotImplementedException("osNpcMoveTo(key, vector)");
        }

        [APILevel(APIFlags.OSSL, "osNpcMoveToTarget")]
        [APILevel(APIFlags.ASSL, "npcMoveToTarget")]
        public void NpcMoveToTarget(ScriptInstance instance, LSLKey npc, Vector3 target, int options)
        {
            throw new NotImplementedException("osNpcMoveToTarget(key, vector, integer)");
        }

        [APILevel(APIFlags.OSSL, "osNpcRemove")]
        [APILevel(APIFlags.ASSL, "npcRemove")]
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

        LSLKey SaveAppearance(ScriptInstance instance, NpcAgent agent, string notecard)
        {
            ObjectPart part = instance.Part;
            SceneInterface scene = part.ObjectGroup.Scene;
            Notecard nc = (Notecard)agent.Appearance;
            AssetData asset = nc.Asset();
            asset.Name = "Saved Appearance";
            asset.ID = UUID.Random;
            scene.AssetService.Store(asset);

            ObjectPartInventoryItem item = new ObjectPartInventoryItem();
            item.AssetID = asset.ID;
            item.AssetType = AssetType.Notecard;
            item.Creator = part.Owner;
            item.ParentFolderID = part.ID;
            item.InventoryType = InventoryType.Notecard;
            item.LastOwner = part.Owner;
            item.Permissions.Base = InventoryPermissionsMask.Every;
            item.Permissions.Current = InventoryPermissionsMask.Every;
            item.Permissions.Group = InventoryPermissionsMask.None;
            item.Permissions.NextOwner = InventoryPermissionsMask.All;
            item.Permissions.EveryOne = InventoryPermissionsMask.None;

            item.Name = notecard;
            item.Description = "Saved Appearance";
            part.Inventory.Add(item);
            return UUID.Zero;
        }

        [APILevel(APIFlags.OSSL, "osNpcSaveAppearance")]
        [APILevel(APIFlags.ASSL, "npcSaveAppearance")]
        public LSLKey NpcSaveAppearance(ScriptInstance instance, LSLKey npc, string notecard)
        {
            NpcAgent agent;
            lock(instance)
            {
                if(TryGetNpc(instance, npc.AsUUID, out agent))
                {
                    return SaveAppearance(instance, agent, notecard);
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.ASSL, "npcSendInstantMessage")]
        public void NpcSendInstantMessage(ScriptInstance instance, LSLKey npc, LSLKey user, string message)
        {
            NpcAgent npcAgent;
            IAgent agent;
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if(TryGetNpc(instance, npc.AsUUID, out npcAgent) &&
                    scene.Agents.TryGetValue(user.AsUUID, out agent))
                {
                    GridInstantMessage gim = new GridInstantMessage();
                    gim.FromAgent = npcAgent.Owner;
                    gim.Dialog = GridInstantMessageDialog.MessageFromAgent;
                    gim.FromGroup = UGI.Unknown;
                    gim.IMSessionID = UUID.Random;
                    gim.Message = message;
                    gim.Position = npcAgent.GlobalPosition;
                    gim.RegionID = scene.ID;
                    agent.IMSend(gim);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSay")]
        [APILevel(APIFlags.ASSL, "npcSay")]
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
        [APILevel(APIFlags.ASSL, "npcSay")]
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
        [APILevel(APIFlags.ASSL, "npcShout")]
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
        [APILevel(APIFlags.ASSL, "npcShout")]
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
        [APILevel(APIFlags.ASSL, "npcWhisper")]
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
        [APILevel(APIFlags.ASSL, "npcWhisper")]
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
        [APILevel(APIFlags.ASSL, "npcSetRot")]
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
        [APILevel(APIFlags.ASSL, "npcSit")]
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
        [APILevel(APIFlags.ASSL, "npcStand")]
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

        [APILevel(APIFlags.OSSL, "osNpcSetProfileAbout")]
        [APILevel(APIFlags.ASSL, "npcSetProfileAbout")]
        public void NpcSetProfileAbout(ScriptInstance instance, LSLKey npc, string text)
        {
            lock(instance)
            {
                NpcAgent agent;
                if (TryGetNpc(instance, npc, out agent))
                {
                    ProfileProperties props;
                    props = agent.ProfileService.Properties[agent.Owner];
                    props.AboutText = text;
                    agent.ProfileService.Properties[agent.Owner, ServiceInterfaces.Profile.ProfileServiceInterface.PropertiesUpdateFlags.Properties] = props;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSetProfileWebURL")]
        [APILevel(APIFlags.ASSL, "npcSetProfileWebURL")]
        public void NpcSetProfileWebURL(ScriptInstance instance, LSLKey npc, string url)
        {
            lock(instance)
            {
                NpcAgent agent;
                if(TryGetNpc(instance, npc, out agent))
                {
                    ProfileProperties props;
                    props = agent.ProfileService.Properties[agent.Owner];
                    props.WebUrl = url;
                    agent.ProfileService.Properties[agent.Owner, ServiceInterfaces.Profile.ProfileServiceInterface.PropertiesUpdateFlags.Properties] = props;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcSetProfileImage")]
        [APILevel(APIFlags.ASSL, "npcSetProfileImage")]
        public void NpcSetProfileImage(ScriptInstance instance, LSLKey npc, string image)
        {
            UUID textureID = instance.GetTextureAssetID(image);

            lock (instance)
            {
                NpcAgent agent;
                if (TryGetNpc(instance, npc, out agent))
                {
                    ProfileProperties props;
                    props = agent.ProfileService.Properties[agent.Owner];
                    props.ImageID = textureID;
                    agent.ProfileService.Properties[agent.Owner, ServiceInterfaces.Profile.ProfileServiceInterface.PropertiesUpdateFlags.Properties] = props;
                }
            }
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
        [APILevel(APIFlags.ASSL, "npcTouch")]
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

        [APILevel(APIFlags.ASSL, "npcGetItemsInFolder")]
        public AnArray NpcGetItemsInFolder(ScriptInstance instance, LSLKey npc, LSLKey folder)
        {
            NpcAgent npcAgent;
            AnArray result = new AnArray();
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    foreach(InventoryItem item in npcAgent.InventoryService.Folder.GetItems(npc.AsUUID, folder.AsUUID))
                    {
                        result.Add(item.ID);
                        result.Add(item.Name);
                    }
                }
            }
            return result;
        }

        [APILevel(APIFlags.ASSL, "npcGetFoldersInFolder")]
        public AnArray NpcGetFoldersInFolder(ScriptInstance instance, LSLKey npc, LSLKey folderid)
        {
            NpcAgent npcAgent;
            AnArray result = new AnArray();
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    foreach (InventoryFolder folder in npcAgent.InventoryService.Folder.GetFolders(npc.AsUUID, folderid.AsUUID))
                    {
                        result.Add(folder.ID);
                        result.Add(folder.Name);
                    }
                }
            }
            return result;
        }

        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_NAME = 1;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_DESCRIPTION = 2;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_TYPE = 3;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_ASSET_TYPE = 4;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_ASSET_ID = 5;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_PARENT_FOLDER_ID = 6;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_BASE_MASK = 7;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_CURRENT_MASK = 8;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_EVERYONE_MASK = 9;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_GROUP_MASK = 10;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_NEXTOWNER_MASK = 11;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_FLAGS = 12;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_CREATOR = 13;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_CREATIONDATE = 14;
        [APILevel(APIFlags.ASSL)]
        public const int NPC_INVENTORY_VERSION = 15;

        public LSLKey NpcGetFolderForType(ScriptInstance instance, LSLKey npc, AssetType type)
        {
            NpcAgent npcAgent;
            UUID result = UUID.Zero;
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    InventoryFolder folder;
                    if (npcAgent.InventoryService.Folder.TryGetValue(npc.AsUUID, type, out folder))
                    {
                        result = folder.ID;
                    }
                }
            }
            return result;
        }

        [APILevel(APIFlags.ASSL, "npcGetRootFolder")]
        public LSLKey NpcGetRootFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.RootFolder);
        }

        [APILevel(APIFlags.ASSL, "npcGetClothingFolder")]
        public LSLKey NpcGetClothingFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Clothing);
        }

        [APILevel(APIFlags.ASSL, "npcGetBodypartsFolder")]
        public LSLKey NpcGetBodypartsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Bodypart);
        }

        [APILevel(APIFlags.ASSL, "npcGetObjectsFolder")]
        public LSLKey NpcGetObjectsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Object);
        }

        [APILevel(APIFlags.ASSL, "npcGetNotecardsFolder")]
        public LSLKey NpcGetNotecardsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Notecard);
        }

        [APILevel(APIFlags.ASSL, "npcGetScriptsFolder")]
        public LSLKey NpcGetScriptsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.LSLText);
        }

        [APILevel(APIFlags.ASSL, "npcGetTexturesFolder")]
        public LSLKey NpcGetTexturesFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Texture);
        }

        [APILevel(APIFlags.ASSL, "npcGetSoundsFolder")]
        public LSLKey NpcGetSoundsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Sound);
        }

        [APILevel(APIFlags.ASSL, "npcGetLandmarksFolder")]
        public LSLKey NpcGetLandmarksFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Landmark);
        }

        [APILevel(APIFlags.ASSL, "npcGetAnimationsFolder")]
        public LSLKey NpcGetAnimationsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Animation);
        }

        [APILevel(APIFlags.ASSL, "npcGetGesturesFolder")]
        public LSLKey NpcGetGesturesFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.Gesture);
        }

        [APILevel(APIFlags.ASSL, "npcGetCallingcardsFolder")]
        public LSLKey NpcGetCallingcardsFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.CallingCard);
        }

        [APILevel(APIFlags.ASSL, "npcGetTrashFolder")]
        public LSLKey NpcGetTrashFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.TrashFolder);
        }

        [APILevel(APIFlags.ASSL, "npcGetLostAndFoundFolder")]
        public LSLKey NpcGetLostAndFoundFolder(ScriptInstance instance, LSLKey npc)
        {
            return NpcGetFolderForType(instance, npc, AssetType.LostAndFoundFolder);
        }

        [APILevel(APIFlags.ASSL, "npcListenIM")]
        public void NpcListenIM(ScriptInstance instance, LSLKey npc, int maptochannel)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                ObjectPart part = instance.Part;
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent) && npcAgent.IsInScene(part.ObjectGroup.Scene))
                {
                    npcAgent.ListenIM(part.ID, instance.Item.ID, maptochannel);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "npcUnlistenIM")]
        public void NpcUnlistenIM(ScriptInstance instance, LSLKey npc)
        {
            NpcAgent npcAgent;
            lock (instance)
            {
                ObjectPart part = instance.Part;
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent) && npcAgent.IsInScene(part.ObjectGroup.Scene))
                {
                    npcAgent.UnlistenIM(part.ID, instance.Item.ID);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "npcListen")]
        public void NpcListen(ScriptInstance instance, LSLKey npc, int maptochannel)
        {
            NpcAgent npcAgent;
            lock(instance)
            {
                ObjectPart part = instance.Part;
                if(TryGetNpc(instance, npc.AsUUID, out npcAgent) && npcAgent.IsInScene(part.ObjectGroup.Scene))
                {
                    npcAgent.ListenAsNpc(part.ID, instance.Item.ID, maptochannel);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "npcUnlisten")]
        public void NpcUnlisten(ScriptInstance instance, LSLKey npc)
        {
            NpcAgent npcAgent;
            lock(instance)
            {
                ObjectPart part = instance.Part;
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent) && npcAgent.IsInScene(part.ObjectGroup.Scene))
                {
                    npcAgent.UnlistenAsNpc(part.ID, instance.Item.ID);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "npcGetItemData")]
        public AnArray NpcGetItemData(ScriptInstance instance, LSLKey npc, LSLKey itemid, AnArray paralist)
        {
            NpcAgent npcAgent;
            AnArray result = new AnArray();
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    InventoryItem item;
                    if(npcAgent.InventoryService.Item.TryGetValue(npc.AsUUID, itemid.AsUUID, out item))
                    {
                        foreach(IValue iv in paralist)
                        {
                            switch(iv.AsInt)
                            {
                                case NPC_INVENTORY_NAME:
                                    result.Add(item.Name);
                                    break;

                                case NPC_INVENTORY_DESCRIPTION:
                                    result.Add(item.Description);
                                    break;

                                case NPC_INVENTORY_TYPE:
                                    result.Add((int)item.InventoryType);
                                    break;

                                case NPC_INVENTORY_ASSET_TYPE:
                                    result.Add((int)item.AssetType);
                                    break;

                                case NPC_INVENTORY_PARENT_FOLDER_ID:
                                    result.Add(new LSLKey(item.ParentFolderID));
                                    break;

                                case NPC_INVENTORY_BASE_MASK:
                                    result.Add((int)item.Permissions.Base);
                                    break;

                                case NPC_INVENTORY_CURRENT_MASK:
                                    result.Add((int)item.Permissions.Current);
                                    break;

                                case NPC_INVENTORY_EVERYONE_MASK:
                                    result.Add((int)item.Permissions.EveryOne);
                                    break;

                                case NPC_INVENTORY_GROUP_MASK:
                                    result.Add((int)item.Permissions.Group);
                                    break;

                                case NPC_INVENTORY_NEXTOWNER_MASK:
                                    result.Add((int)item.Permissions.NextOwner);
                                    break;

                                case NPC_INVENTORY_FLAGS:
                                    result.Add((int)item.Flags);
                                    break;

                                case NPC_INVENTORY_CREATOR:
                                    result.Add(new LSLKey(item.Creator.ID));
                                    break;

                                case NPC_INVENTORY_CREATIONDATE:
                                    result.Add(item.CreationDate.AsULong);
                                    break;

                                default:
                                    result.Add(string.Empty);
                                    break;
                            }
                        }
                    }
                }
            }
            return result;
        }

        [APILevel(APIFlags.ASSL, "npcGetFolderData")]
        public AnArray NpcGetFolderData(ScriptInstance instance, LSLKey npc, LSLKey folderid, AnArray paralist)
        {
            NpcAgent npcAgent;
            AnArray result = new AnArray();
            lock (instance)
            {
                if (TryGetNpc(instance, npc.AsUUID, out npcAgent))
                {
                    InventoryFolder folder;
                    if (npcAgent.InventoryService.Folder.TryGetValue(npc.AsUUID, folderid.AsUUID, out folder))
                    {
                        foreach (IValue iv in paralist)
                        {
                            switch (iv.AsInt)
                            {
                                case NPC_INVENTORY_NAME:
                                    result.Add(folder.Name);
                                    break;

                                case NPC_INVENTORY_TYPE:
                                    result.Add((int)folder.InventoryType);
                                    break;

                                case NPC_INVENTORY_VERSION:
                                    result.Add(folder.Version);
                                    break;

                                case NPC_INVENTORY_PARENT_FOLDER_ID:
                                    result.Add(new LSLKey(folder.ParentFolderID));
                                    break;

                                default:
                                    result.Add(string.Empty);
                                    break;
                            }
                        }
                    }
                }
            }
            return result;
        }

        [ExecutedOnScriptRemove]
        public void ResetListeners(ScriptInstance instance)
        {
            Script script = (Script)instance;
            lock (script)
            {
                ObjectPart part = instance.Part;
                SceneInterface scene = part.ObjectGroup.Scene;
                m_NpcManager.UnlistenAsNpc(scene.ID, part.ID, instance.Item.ID);
                m_NpcManager.UnlistenIM(scene.ID, part.ID, instance.Item.ID);
            }
        }
    }
}
