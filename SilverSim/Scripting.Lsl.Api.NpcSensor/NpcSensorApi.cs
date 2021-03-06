﻿// SilverSim is distributed under the terms of the
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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Npc;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Lsl.Api.NpcSensor.ScriptEvents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Sensor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.NpcSensor
{
    [ScriptApiName("NpcSensor")]
    [LSLImplementation]
    [Description("OSSL NpcSensor API")]
    [PluginName("LSL_NpcSensor")]
    public class NpcSensorApi : IScriptApi, IPlugin, IPluginShutdown
    {
        public class SensorInfo
        {
            public readonly ScriptInstance Instance;
            public double Timeout;
            public double TimeoutToElapse;
            public bool IsRepeating;
            public NpcAgent Npc;
            public UUID OwnObjectID;
            public UUID OwnerID;
            public bool IsAttached;
            public int SearchType;
            public string SearchName;
            public double SearchRadius;
            public double SearchArc;
            public double SearchArcCosine;
            public Vector3 SensePoint;
            public Quaternion SenseRotation;
            public Vector3 SenseForwardVector = Vector3.UnitX;
            public UUID SearchKey;

            public readonly RwLockedDictionary<UUID, DetectInfo> SensorHits = new RwLockedDictionary<UUID, DetectInfo>();

            public SensorInfo(ScriptInstance instance, NpcAgent npc, bool isRepeating, double timeout, string sName, LSLKey sKey, int sType, double sRadius, double sArc)
            {
                Instance = instance;
                Npc = npc;
                OwnerID = instance.Part.Owner.ID;
                IsAttached = instance.Part.ObjectGroup.IsAttached;
                Timeout = timeout;
                TimeoutToElapse = timeout;
                IsRepeating = isRepeating;
                SearchName = sName;
                SearchType = sType;
                SearchRadius = sRadius;
                SearchArc = sArc.Clamp(0, Math.PI);
                SearchArcCosine = SearchArc.SensorArcToCosine();
                SearchKey = (sKey.AsBoolean) ? sKey.AsUUID : UUID.Zero;
            }

            public void UpdateSenseLocation()
            {
                SensePoint = Npc.GlobalPosition;
                SenseRotation = Npc.GlobalRotation;
                SenseForwardVector = Vector3.UnitX * SenseRotation;
            }
        }

        public class SceneInfo : ISceneListener, IAgentListener
        {
            public bool IgnorePhysicsLocationUpdates => false;

            private static readonly ILog m_Log = LogManager.GetLogger("LSL_NPCSENSORS");

            public SceneInterface Scene;
            public readonly RwLockedDictionary<UUID, ObjectGroup> KnownObjects = new RwLockedDictionary<UUID, ObjectGroup>();
            public readonly RwLockedDictionary<UUID, IAgent> KnownAgents = new RwLockedDictionary<UUID, IAgent>();
            public readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<ScriptInstance, SensorInfo>> SensorRepeats = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<ScriptInstance, SensorInfo>>(() => new RwLockedDictionary<ScriptInstance, SensorInfo>());
            public readonly System.Timers.Timer m_Timer = new System.Timers.Timer(1);
            public readonly object m_TimerLock = new object();
            /* when sensor repeats are active, these are the operating limits */
            private int m_LastTickCount;
            private const double MIN_SENSOR_INTERVAL = 0.2;
            private const double MAX_SENSOR_INTERVAL = 3600;
            public Thread m_ObjectWorkerThread;
            public bool m_StopThread;
            private readonly BlockingQueue<ObjectUpdateInfo> m_ObjectUpdates = new BlockingQueue<ObjectUpdateInfo>();

            public SceneInfo(SceneInterface scene)
            {
                Scene = scene;
                Scene.SceneListeners.Add(this);
                m_Timer.Elapsed += SensorRepeatTimer;
                m_ObjectWorkerThread = ThreadManager.CreateThread(SensorUpdateThread);
                m_ObjectWorkerThread.Start();
            }

            public void Stop()
            {
                m_StopThread = true;
                if (!m_ObjectWorkerThread.Join(10000))
                {
                    m_ObjectWorkerThread.Abort();
                }
                m_Timer.Stop();
                m_Timer.Elapsed -= SensorRepeatTimer;
                m_Timer.Dispose();
                Scene.SceneListeners.Remove(this);
                SensorRepeats.Clear();
                KnownAgents.Clear();
                KnownObjects.Clear();
                Scene = null;
            }

            public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
            {
                m_ObjectUpdates.Enqueue(info);
            }

            public void ScheduleUpdate(ObjectInventoryUpdateInfo info, UUID fromSceneID)
            {
                /* intentionally ignored */
            }

            public void ScheduleUpdate(AgentUpdateInfo info, UUID fromSceneID)
            {
                /* intentionally ignored */
            }

            private void SensorUpdateThread()
            {
                Thread.CurrentThread.Name = "NpcSensor Repeat Thread for " + Scene.ID.ToString();
                while (!m_StopThread)
                {
                    ObjectUpdateInfo info;
                    try
                    {
                        info = m_ObjectUpdates.Dequeue(1000);
                    }
                    catch (NullReferenceException)
                    {
                        break;
                    }
                    catch
                    {
                        continue;
                    }

                    if (m_StopThread)
                    {
                        break;
                    }

                    try
                    {
                        if (info.IsKilled || info.Part.LinkNumber != ObjectGroup.LINK_ROOT)
                        {
                            KnownObjects.Remove(info.Part.ID);
                            continue;
                        }
                        else if (info.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                        {
                            ObjectGroup grp = info.Part.ObjectGroup;
                            /* we can get the registering updates multiple times, so we process them that way */
                            KnownObjects[info.Part.ID] = grp;
                            foreach (RwLockedDictionary<ScriptInstance, SensorInfo> sens in SensorRepeats.Values)
                            {
                                foreach (KeyValuePair<ScriptInstance, SensorInfo> kvp in sens)
                                {
                                    if (kvp.Value.SensorHits.ContainsKey(grp.ID))
                                    {
                                        continue;
                                    }
                                    AddIfSensed(kvp.Value, grp);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        /* never crash in this location */
                        m_Log.Debug("Unexpected exception", e);
                    }
                }
            }

            private void SensorRepeatTimer(object o, EventArgs args)
            {
                int elapsedTimeInMsecs;
                lock (m_TimerLock)
                {
                    /* Stop timer when not needed */
                    if (SensorRepeats.Count == 0)
                    {
                        m_Timer.Stop();
                        return;
                    }
                    int newTickCount = Environment.TickCount;
                    elapsedTimeInMsecs = newTickCount - m_LastTickCount;
                    m_LastTickCount = newTickCount;
                }

                double elapsedTimeInSecs = elapsedTimeInMsecs / 1000f;

                foreach (RwLockedDictionary<ScriptInstance, SensorInfo> sens in SensorRepeats.Values)
                {
                    foreach (KeyValuePair<ScriptInstance, SensorInfo> kvp in sens)
                    {
                        kvp.Value.TimeoutToElapse -= elapsedTimeInSecs;
                        if (kvp.Value.TimeoutToElapse <= 0)
                        {
                            try
                            {
                                kvp.Value.TimeoutToElapse += kvp.Value.Timeout;
                                if (kvp.Value.SensorHits.Count != 0)
                                {
                                    kvp.Value.Instance.PostEvent(new NpcSensorEvent
                                    {
                                        NpcId = kvp.Value.Npc.ID,
                                        Detected = GetDistanceSorted(kvp.Value.SensePoint, kvp.Value.SensorHits.Values)
                                    });
                                }
                                else
                                {
                                    kvp.Value.Instance.PostEvent(new NpcNoSensorEvent
                                    {
                                        NpcId = kvp.Value.Npc.ID
                                    });
                                }

                                /* re-evaluate sensor data */
                                kvp.Value.UpdateSenseLocation();
                                CleanRepeatSensor(kvp.Value);
                                if ((kvp.Value.SearchType & SENSE_AGENTS) != 0)
                                {
                                    foreach (IAgent agent in KnownAgents.Values)
                                    {
                                        AddIfSensed(kvp.Value, agent);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                /* do not loose sensors just of something happening in here */
                                m_Log.Debug("Unexpected exception", e);
                            }
                        }
                    }
                }
            }

            /* private constants */
            private const int SENSE_AGENTS = 0x33;
            private const int SENSE_OBJECTS = 0xE;
            private const int AGENT = 0x01;
            private const int ACTIVE = 0x02;
            private const int PASSIVE = 0x04;
            private const int SCRIPTED = 0x08;
            private const int AGENT_BY_USERNAME = 0x10;
            private const int NPC = 0x20;

            private void CleanRepeatSensor(SensorInfo sensor)
            {
                /* it is a lot faster to re-check the detect list than going through the big object list.
                 * The nice improvement of that is that our repeat sensor does not need an initial scan after every interval.
                 */
                var newSensorHits = new List<DetectInfo>();
                foreach (KeyValuePair<UUID, DetectInfo> kvp in sensor.SensorHits)
                {
                    IAgent agent;
                    ObjectGroup objgrp;

                    if (KnownAgents.TryGetValue(kvp.Key, out agent))
                    {
                        DetectInfo di = kvp.Value;
                        ExtensionMethods.FillDetectInfoFromObject(ref di, agent);
                        if (CheckIfSensed(sensor, agent))
                        {
                            newSensorHits.Add(di);
                        }
                    }
                    else if (KnownObjects.TryGetValue(kvp.Key, out objgrp))
                    {
                        DetectInfo di = kvp.Value;
                        ExtensionMethods.FillDetectInfoFromObject(ref di, objgrp);
                        if (CheckIfSensed(sensor, objgrp))
                        {
                            newSensorHits.Add(di);
                        }
                    }
                }

                foreach (DetectInfo di in newSensorHits)
                {
                    sensor.SensorHits[di.Key] = di;
                }
            }

            private void EvalSensor(SensorInfo sensor)
            {
                if ((sensor.SearchType & SENSE_AGENTS) != 0)
                {
                    foreach (IAgent agent in Scene.RootAgents)
                    {
                        AddIfSensed(sensor, agent);
                    }
                }

                if ((sensor.SearchType & SENSE_OBJECTS) != 0)
                {
                    foreach (ObjectGroup grp in KnownObjects.Values)
                    {
                        AddIfSensed(sensor, grp);
                    }
                }
            }

            private List<DetectInfo> GetDistanceSorted(Vector3 basePos, ICollection<DetectInfo> unsortedCollection)
            {
                var list = new List<DetectInfo>();
                foreach (DetectInfo input_di in unsortedCollection)
                {
                    double input_dist = (basePos - input_di.Position).LengthSquared;
                    int beforePos = 0;

                    for (beforePos = 0; beforePos < list.Count; ++beforePos)
                    {
                        DetectInfo output_di = list[beforePos];
                        double cur_dist = (basePos - output_di.Position).LengthSquared;
                        if (cur_dist > input_dist)
                        {
                            break;
                        }
                    }

                    list.Insert(beforePos, input_di);
                }

                return list;
            }

            public void StartSensor(SensorInfo sensor)
            {
                sensor.UpdateSenseLocation();
                if (sensor.IsRepeating)
                {
                    SensorRepeats[sensor.Npc.ID][sensor.Instance] = sensor;
                }
                else
                {
                    EvalSensor(sensor);
                }

                if (sensor.SensorHits.Count != 0)
                {
                    sensor.Instance.PostEvent(new NpcSensorEvent
                    {
                        NpcId = sensor.Npc.ID,
                        Detected = GetDistanceSorted(sensor.SensePoint, sensor.SensorHits.Values)
                    });
                }
                else
                {
                    sensor.Instance.PostEvent(new NpcNoSensorEvent
                    {
                        NpcId = sensor.Npc.ID
                    });
                }

                if (sensor.IsRepeating)
                {
                    double mintimerreq = -1;
                    foreach (RwLockedDictionary<ScriptInstance, SensorInfo> sens in SensorRepeats.Values)
                    {
                        foreach (KeyValuePair<ScriptInstance, SensorInfo> kvp in sens)
                        {
                            if (mintimerreq < 0 || kvp.Value.Timeout < mintimerreq)
                            {
                                mintimerreq = kvp.Value.Timeout;
                            }
                        }
                    }

                    mintimerreq = mintimerreq.Clamp(MIN_SENSOR_INTERVAL, MAX_SENSOR_INTERVAL);

                    lock (m_TimerLock)
                    {
                        if (mintimerreq < 0)
                        {
                            m_Timer.Stop();
                        }
                        else
                        {
                            m_Timer.Interval = mintimerreq;

                            if (!m_Timer.Enabled)
                            {
                                /* load a new value into LastTickCount, timer was disabled */
                                m_LastTickCount = Environment.TickCount;
                                m_Timer.Start();
                            }
                        }
                    }
                }
            }

            private void AddIfSensed(SensorInfo sensor, IObject obj)
            {
                if (CheckIfSensed(sensor, obj))
                {
                    var detInfo = new DetectInfo();
                    ExtensionMethods.FillDetectInfoFromObject(ref detInfo, obj);
                    sensor.SensorHits[obj.ID] = detInfo;
                }
            }

            private bool CheckArcAndRange(SensorInfo sensor, IObject obj)
            {
                Vector3 fromPos = sensor.SensePoint;
                Vector3 targetPos = obj.GlobalPosition;
                Vector3 object_direction = targetPos - fromPos;
                double distance = object_direction.Length;
                if (distance > sensor.SearchRadius)
                {
                    return false;
                }
                return object_direction.DirectionToCosine(sensor.SenseForwardVector) >= sensor.SearchArcCosine;
            }

            private bool CheckIfSensed(SensorInfo sensor, IObject obj)
            {
                if (sensor.SearchKey != UUID.Zero && sensor.SearchKey != obj.ID)
                {
                    return false;
                }
                if (obj.ID == sensor.OwnObjectID)
                {
                    return false;
                }

                ObjectGroup grp = obj as ObjectGroup;
                if (grp != null)
                {
                    if (grp.IsAttached)
                    {
                        /* ignore those */
                    }
                    else if (
                        ((sensor.SearchType & ACTIVE) != 0 && grp.IsPhysics) ||
                        ((sensor.SearchType & PASSIVE) != 0 && !grp.IsPhysics)
                        )
                    {
                        if (sensor.SearchName.Length != 0 && sensor.SearchName != obj.Name)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    return CheckArcAndRange(sensor, obj);
                }

                IAgent agent = obj as IAgent;
                if (agent != null)
                {
                    if ((sensor.SearchType & SENSE_AGENTS) == 0)
                    {
                        return false;
                    }

                    if (agent.IsNpc)
                    {
                        if ((sensor.SearchType & NPC) != 0 && sensor.SearchName.Length != 0 &&
                            sensor.SearchName != agent.NamedOwner.FullName ||
                            (sensor.SearchName != agent.NamedOwner.FirstName + " Resident" && agent.NamedOwner.LastName.Length == 0))
                        {
                            return false;
                        }
                    }
                    else if ((sensor.SearchType & AGENT) != 0 && sensor.SearchName.Length != 0 &&
                            (sensor.SearchName != agent.NamedOwner.FullName ||
                            (sensor.SearchName != agent.NamedOwner.FirstName + " Resident" && agent.NamedOwner.LastName.Length == 0)))
                    {
                        return false;
                    }
                    else if ((sensor.SearchType & AGENT_BY_USERNAME) != 0 && sensor.SearchName.Length != 0 &&
                            (sensor.SearchName != (agent.NamedOwner.FirstName + ".resident").ToLower() && agent.NamedOwner.LastName.Length == 0) ||
                            (sensor.SearchName != agent.NamedOwner.FullName.Replace(' ', '.') && agent.NamedOwner.LastName.Length != 0))
                    {
                        return false;
                    }

                    if (agent.IsNpc && (sensor.SearchType & NPC) == 0)
                    {
                        return false;
                    }
                    if ((sensor.SearchType & SENSE_AGENTS) == 0)
                    {
                        if (agent.SittingOnObject != null && (sensor.SearchType & PASSIVE) == 0)
                        {
                            return false;
                        }
                        if (agent.SittingOnObject == null && (sensor.SearchType & ACTIVE) == 0)
                        {
                            return false;
                        }
                    }
                    return CheckArcAndRange(sensor, obj);
                }
                return false;
            }

            public void AddedAgent(IAgent agent)
            {
                if (agent.IsInScene(Scene))
                {
                    KnownAgents[agent.ID] = agent;
                }
            }

            public void AgentChangedScene(IAgent agent)
            {
                if (agent.IsInScene(Scene))
                {
                    KnownAgents[agent.ID] = agent;
                }
                else
                {
                    KnownAgents.Remove(agent.ID);
                }
            }

            public void RemovedAgent(IAgent agent)
            {
                KnownAgents.Remove(agent.ID);
            }
        }

        private readonly RwLockedDictionary<UUID, SceneInfo> m_Scenes = new RwLockedDictionary<UUID, SceneInfo>();
        private SceneList m_SceneList;

        public void Startup(ConfigurationLoader loader)
        {
            m_SceneList = loader.Scenes;
            m_SceneList.OnRegionAdd += Scenes_OnRegionAdd;
            m_SceneList.OnRegionRemove += Scenes_OnRegionRemove;
        }

        public void Shutdown()
        {
            m_SceneList.OnRegionAdd -= Scenes_OnRegionAdd;
            m_SceneList.OnRegionRemove -= Scenes_OnRegionRemove;
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        private void Scenes_OnRegionAdd(SceneInterface obj)
        {
            m_Scenes.Add(obj.ID, new SceneInfo(obj));
        }

        private void Scenes_OnRegionRemove(SceneInterface obj)
        {
            SceneInfo sceneInfo;
            if (m_Scenes.Remove(obj.ID, out sceneInfo))
            {
                sceneInfo.Stop();
            }
        }

        private bool TryGetNpc(SceneInterface scene, UUID npc, out NpcAgent npcAgent)
        {
            IAgent agent;
            if(!scene.Agents.TryGetValue(npc, out agent))
            {
                npcAgent = null;
                return false;
            }
            npcAgent = agent as NpcAgent;
            return npcAgent != null;
        }

        [APILevel(APIFlags.ASSL, "npcsensor")]
        [StateEventDelegate]
        public delegate void State_npcsensor(LSLKey npcid, int num_detected);

        [APILevel(APIFlags.ASSL, "no_npcsensor")]
        [StateEventDelegate]
        public delegate void State_no_npcsensor(LSLKey npcid);

        /* advertise script events that need to be translated */
        [TranslatedScriptEventsInfo]
        public static readonly Type[] TranslatedEvents = new Type[] { typeof(NpcSensorEvent), typeof(NpcNoSensorEvent) };

        [APILevel(APIFlags.ASSL, "npcSensor")]
        public void Sensor(ScriptInstance instance, LSLKey npc, string name, LSLKey id, int type, double radius, double arc)
        {
            if (type == 0)
            {
                return;
            }
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                SceneInfo sceneInfo;
                NpcAgent npcAgent;
                if (m_Scenes.TryGetValue(scene.ID, out sceneInfo) &&
                    TryGetNpc(scene, npc.AsUUID, out npcAgent))
                {
                    sceneInfo.StartSensor(new SensorInfo(instance, npcAgent, false, 0, name, id, type, radius, arc) { OwnObjectID = npcAgent.ID });
                }
            }
        }

        [APILevel(APIFlags.ASSL, "npcSensorRepeat")]
        public void SensorRepeat(ScriptInstance instance, LSLKey npc, string name, LSLKey id, int type, double range, double arc, double rate)
        {
            if (type == 0)
            {
                type = ~0;
            }
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                SceneInfo sceneInfo;
                NpcAgent npcAgent;
                if (m_Scenes.TryGetValue(scene.ID, out sceneInfo) &&
                    TryGetNpc(scene, npc.AsUUID, out npcAgent))
                {
                    sceneInfo.StartSensor(new SensorInfo(instance, npcAgent, true, rate, name, id, type, range, arc) { OwnObjectID = npcAgent.ID });
                }
            }
        }

        [APILevel(APIFlags.ASSL, "npcSensorRemove")]
        public void SensorRemove(ScriptInstance instance, LSLKey npcid)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                SceneInfo sceneInfo;
                RwLockedDictionary<ScriptInstance, SensorInfo> npcSensors;
                if (m_Scenes.TryGetValue(scene.ID, out sceneInfo) &&
                    sceneInfo.SensorRepeats.TryGetValue(npcid.AsUUID, out npcSensors))
                {
                    npcSensors.Remove(instance);
                    sceneInfo.SensorRepeats.RemoveIf(npcid.AsUUID, (RwLockedDictionary<ScriptInstance, SensorInfo> info) => info.Count == 0);
                }
            }
        }

        [ExecutedOnScriptRemove]
        [ExecutedOnScriptReset]
        public void RemoveSensors(ScriptInstance instance)
        {
            SceneInterface scene = instance.Part?.ObjectGroup?.Scene;
            SceneInfo sceneInfo;
            if (scene != null && m_Scenes.TryGetValue(scene.ID, out sceneInfo))
            {
                var npcs = new List<UUID>();
                foreach(KeyValuePair<UUID, RwLockedDictionary<ScriptInstance, SensorInfo>> kvp in sceneInfo.SensorRepeats)
                {
                    SensorInfo info;
                    if(kvp.Value.Remove(instance, out info) && !npcs.Contains(info.Npc.ID))
                    {
                        npcs.Add(info.Npc.ID);
                    }
                }
                foreach(UUID npcId in npcs)
                {
                    sceneInfo.SensorRepeats.RemoveIf(npcId, (RwLockedDictionary<ScriptInstance, SensorInfo> info) => info.Count == 0);
                }
            }
        }

        [ExecutedOnDeserialization("npcsensor")]
        public void Deserialize(ScriptInstance instance, List<object> args)
        {
            if (args.Count < 7)
            {
                return;
            }
            var script = (Script)instance;
            lock (script)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                SceneInfo sceneInfo;
                NpcAgent npcAgent;
                for (int argi = 0; argi < args.Count; argi += 7)
                {
                    if(args.Count - argi < 7)
                    {
                        break;
                    }

                    if (m_Scenes.TryGetValue(scene.ID, out sceneInfo) &&
                        TryGetNpc(scene, (UUID)args[argi + 0], out npcAgent))
                    {
                        var info = new SensorInfo(instance,
                            npcAgent,
                            true,
                            (double)args[argi + 1],
                            (string)args[argi + 2],
                            (UUID)args[argi + 3],
                            (int)args[argi + 4],
                            (double)args[argi + 5],
                            (double)args[argi + 6])
                        { OwnObjectID = npcAgent.ID };
                        sceneInfo.StartSensor(info);
                    }
                }
            }
        }

        [ExecutedOnSerialization("npcsensor")]
        public void Serialize(ScriptInstance instance, List<object> res)
        {
            var script = (Script)instance;
            lock (script)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                SceneInfo sceneInfo;
                if (m_Scenes.TryGetValue(scene.ID, out sceneInfo) && sceneInfo.SensorRepeats.Count != 0)
                {
                    res.Add("npcsensor");
                    int idx = res.Count;
                    res.Add(0);
                    int count = 0;
                    foreach(RwLockedDictionary<ScriptInstance, SensorInfo> dict in sceneInfo.SensorRepeats.Values)
                    {
                        SensorInfo info;
                        if (dict.TryGetValue(instance, out info))
                        {
                            res.Add(info.Npc.ID);
                            res.Add(info.Timeout);
                            res.Add(info.SearchName);
                            res.Add(info.SearchKey);
                            res.Add(info.SearchType);
                            res.Add(info.SearchRadius);
                            res.Add(info.SearchArc);
                            count += 7;
                        }
                    }
                    res[idx] = count;
                }
            }
        }
    }
}
