using System;
using System.Collections;
using Aqua.Cameras;
using Aqua.Profile;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.View {
    public sealed class ViewManager : SharedManager, ISceneLoadHandler, IScenePreloader, IBaked {
        static public readonly StringHash32 Trigger_ViewEnter = "ViewEnter";

        #region Inspector

        [SerializeField, Required] public ViewNode DefaultNode = null;
        [SerializeField] public CameraDrift Drift;
        [SerializeField, HideInInspector] private ViewNode[] m_AllNodes = null;
        [SerializeField, HideInInspector] private ViewLink[] m_AllLinks = null;
        [SerializeField] private TweenSettings m_DefaultTransition = new TweenSettings(0.3f, Curve.Smooth);

        #endregion // Inspector

        [NonSerialized] private ViewNode m_Current;
        private Routine m_Transition;

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            Services.Events.Register(GameEvents.ViewLockChanged, () => RefreshLinks(false), this);
        }

        protected override void OnDestroy() {
            Services.Events?.DeregisterAll(this);

            base.OnDestroy();
        }

        #endregion // Unity Events

        #region Transitions

        public void GoToNode(StringHash32 id) {
            TransitionToNode(GetNode(id), null);
        }

        public void GoToNode(ViewNode node) {
            TransitionToNode(node, null);
        }

        public void GoToNode(ViewLink link) {
            TransitionToNode(link.Node, link);
        }

        public void SnapToNode(StringHash32 id) {
            SnapToNode(GetNode(id));
        }

        public void SnapToNode(ViewNode node) {
            if (m_Current == node) {
                return;
            }

            if (m_Current) {
                Services.Events.Dispatch(GameEvents.ViewLeaving, m_Current.Id.Hash());
                DeactivateNode(m_Current, false, true);
            }

            m_Current = node;

            node.OnLoad?.Invoke();
            ActivateNode(m_Current, false, true);
            ActivateLinks(m_AllLinks, m_Current.GroupIds, false);

            Services.Camera.SnapToPose(m_Current.Camera);
            if (Drift != null) {
                Drift.Scale = node.CameraDriftScale;
                Drift.PushChanges();
            }
            Services.Events.Dispatch(GameEvents.ViewChanged, m_Current.Id.Source());
            Services.Events.Dispatch(GameEvents.ViewArrived, m_Current.Id.Hash());
            Save.Map.RecordVisitedLocation(m_Current.Id);

            using(var table = TempVarTable.Alloc()) {
                table.Set("viewId", m_Current.Id);
                Services.Script.TriggerResponse(Trigger_ViewEnter, table);
            }
        }

        private void TransitionToNode(ViewNode node, ViewLink link) {
            if (m_Current == node) {
                return;
            }

            if (m_Current == null) {
                SnapToNode(node);
                return;
            }

            m_Transition.Replace(this, TransitionRoutine(node, link));
            m_Transition.Tick();
        }

        private IEnumerator TransitionRoutine(ViewNode node, ViewLink link) {
            using(Script.DisableInput()) {
                Services.Script.KillLowPriorityThreads();

                Transform controlPoint = null;
                TweenSettings settings = m_DefaultTransition;
                if (link) {
                    controlPoint = link.TransitionControlPoint;
                    settings = link.Transition;
                }

                IEnumerator cameraTransition;
                if (controlPoint) {
                    cameraTransition = Services.Camera.MoveToPose(node.Camera, controlPoint.position, settings.Time, settings.Curve, Cameras.CameraPoseProperties.All, Axis.XYZ);
                } else {
                    cameraTransition = Services.Camera.MoveToPose(node.Camera, settings.Time, settings.Curve, Cameras.CameraPoseProperties.All, Axis.XYZ);
                }

                IEnumerator driftTransition;
                if (Drift && Drift.Scale != node.CameraDriftScale) {
                    driftTransition = Tween.Float(Drift.Scale, node.CameraDriftScale, (f) => {
                        Drift.Scale = f;
                        Drift.PushChanges();
                    }, settings.Time);
                } else {
                    driftTransition = null;
                }

                DeactivateLinks(m_AllLinks, false);

                ViewNode old = m_Current;
                m_Current = node;

                Services.Events.Dispatch(GameEvents.ViewLeaving, old.Id.Hash());

                if (old.AudioLayers) {
                    old.AudioLayers.SetLayerActive(old.AudioLayerId, false);
                }
                old.OnExit?.Invoke();
                m_Current = node;
                node.OnLoad?.Invoke();

                if (node.AudioLayers) {
                    node.AudioLayers.SetLayerActive(node.AudioLayerId, true);
                }

                Services.Events.Dispatch(GameEvents.ViewChanged, m_Current.Id.Source());

                yield return Routine.Combine(
                    cameraTransition, driftTransition);

                DeactivateNode(old, false, false);
                ActivateNode(m_Current, false, true);

                ActivateLinks(m_AllLinks, m_Current.GroupIds, false);

                Services.Events.Dispatch(GameEvents.ViewArrived, m_Current.Id.Hash());

                using(var table = TempVarTable.Alloc()) {
                    table.Set("viewId", m_Current.Id);
                    Services.Script.TriggerResponse(Trigger_ViewEnter, table);
                }
            }
        }

        #endregion // Transitions

        #region Nodes

        private ViewNode GetNode(StringHash32 id) {
            ViewNode node;
            if (id.IsEmpty || !m_AllNodes.TryGetValue(id, out node)) {
                node = DefaultNode;
            }
            return node;
        }

        static private void DeactivateNode(ViewNode node, bool force, bool invoke) {
            if (invoke) {
                node.OnExit?.Invoke();
            }
            if (node.AudioLayers) {
                node.AudioLayers.SetLayerActive(node.AudioLayerId, false);
            }
            if (node.UI) {
                node.UI.enabled = false;
            }
            node.Group.SetActive(false, force);
            if (node.InteractionGroup) {
                node.InteractionGroup.Lock();
            }
        }

        static private void ActivateNode(ViewNode node, bool force, bool invoke) {
            if (node.UI) {
                node.UI.enabled = true;
            }
            if (node.AudioLayers) {
                node.AudioLayers.SetLayerActive(node.AudioLayerId, true);
            }
            node.Group.SetActive(true, force);
            if (node.InteractionGroup) {
                node.InteractionGroup.Unlock();
            }
            if (invoke) {
                node.OnEnter?.Invoke();
            }

            Script.WriteVariable(GameVars.ViewId, node.Id);
        }

        static private void DeactivateLinks(ViewLink[] links, bool force) {
            MapData map = Save.Map;

            foreach(var link in links) {
                if (link.Group.Empty) {
                    link.gameObject.SetActive(false);
                } else {
                    link.Group.SetActive(false, force);
                }
            }
        }

        static private void ActivateLinks(ViewLink[] links, SerializedHash32[] activeGroups,  bool force) {
            if (activeGroups == null) {
                DeactivateLinks(links, force);
                return;
            }

            MapData map = Save.Map;

            foreach(var link in links) {
                bool isActive = ArrayUtils.Contains(activeGroups, link.GroupId) && (link.AlwaysAvailable || map.IsRoomUnlocked(link.Node.Id));
                if (link.Group.Empty) {
                    link.gameObject.SetActive(isActive);
                } else {
                    link.Group.SetActive(isActive, force);
                }
            }
        }

        #endregion // Nodes

        #region Links

        private void RefreshLinks(bool force) {
            ActivateLinks(m_AllLinks, m_Current?.GroupIds, force);
        }

        #endregion // Links

        #region ISceneLoad

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            Save.Map.UnlockRoom(DefaultNode.Id);
            RefreshLinks(true);
            
            foreach(var node in m_AllNodes) {
                DeactivateNode(node, true, false);
            }

            return null;
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            SnapToNode(DefaultNode);
        }

        #endregion // ISceneLoad

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order { get { return 2; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            m_AllNodes = FindObjectsOfType<ViewNode>();
            m_AllLinks = FindObjectsOfType<ViewLink>();
            Array.Sort(m_AllLinks, (a, b) => a.GroupId.Hash().CompareTo(b.GroupId.Hash()));

            return true;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}