﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Npc
{
    public partial class NpcAgent : Scene.Agent.Agent
    {
        public override event Action<IObject> OnPositionChange;

        public NpcAgent(
            UUID agentId,
            string firstName,
            string lastName,
            Uri homeURI)
            : base(agentId, homeURI)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public override RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IAgentTeleportServiceInterface ActiveTeleportService
        {
            get
            {
                return null;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override AssetServiceInterface AssetService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Vector3 CameraAtAxis
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override Vector3 CameraLeftAxis
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override Vector3 CameraPosition
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override Quaternion CameraRotation
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override Vector3 CameraUpAxis
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override ClientInfo Client
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override DetectedTypeFlags DetectedType
        {
            get
            {
                return (SittingOnObject != null) ?
                    (DetectedTypeFlags.Npc | DetectedTypeFlags.Passive) :
                    (DetectedTypeFlags.Npc | DetectedTypeFlags.Active);
            }
        }

        public override EconomyServiceInterface EconomyService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override FriendsServiceInterface FriendsService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override GridUserServiceInterface GridUserService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override GroupsServiceInterface GroupsService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override InventoryServiceInterface InventoryService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsActiveGod
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInMouselook
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsNpc
        {
            get
            {
                return true;
            }
        }

        public override RwLockedDictionary<UUID, FriendStatus> KnownFriends
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int LastMeasuredLatencyTickCount
        {
            get
            {
                return 0;
            }

            set
            {
                throw new NotSupportedException("LastMeasuredLatencyTickCount");
            }
        }

        public override OfflineIMServiceInterface OfflineIMService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IPhysicsObject PhysicsActor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override PresenceServiceInterface PresenceService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ProfileServiceInterface ProfileService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override UUID SceneID
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override SessionInfo Session
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override List<GridType> SupportedGridTypes
        {
            get
            {
                return new List<GridType>();
            }
        }

        RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> m_TransmittedTerrainSerials = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>>(delegate () { return new RwLockedDictionary<uint, uint>(); });
        public override RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials
        {
            get
            {
                return m_TransmittedTerrainSerials;
            }
        }

        public override UserAccount UntrustedAccountInfo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override UserAgentServiceInterface UserAgentService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ulong AddNewFile(string filename, byte[] data)
        {
            throw new NotSupportedException("AddNewFile");
        }

        public override void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o)
        {
            /* ignored */
        }

        public override void ClearKnownFriends()
        {
            /* ignored */
        }

        public override void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo)
        {
            /* ignored */
        }

        public override void HandleMessage(ChildAgentPositionUpdate m)
        {
            /* ignored */
        }

        public override void HandleMessage(ChildAgentUpdate m)
        {
            /* ignored */
        }

        public override bool IMSend(GridInstantMessage im)
        {
            throw new NotImplementedException();
        }

        public override void ReleaseControls(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        public override void RemoveActiveTeleportService(IAgentTeleportServiceInterface service)
        {
            throw new NotImplementedException();
        }

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions)
        {
            throw new NotImplementedException();
        }

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID)
        {
            throw new NotImplementedException();
        }

        public override void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions)
        {
            RevokeAnimPermissions(sourceID, permissions);
        }

        readonly RwLockedList<UUID> m_SelectedObjects = new RwLockedList<UUID>();
        public override RwLockedList<UUID> SelectedObjects(UUID scene)
        {
            return m_SelectedObjects;
        }

        public override void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportHome(SceneInterface sceneInterface)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool UnSit()
        {
            throw new NotImplementedException();
        }

        public override void InvokeOnPositionUpdate()
        {
            var e = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (e != null)
            {
                foreach (Action<IObject> del in e.GetInvocationList().OfType<Action<IObject>>())
                {
                    del(this);
                }
            }

            SceneInterface currentScene = CurrentScene;
            if(null != currentScene)
            {
                currentScene.SendAgentObjectToAllAgents(this);
            }
        }

    }
}
