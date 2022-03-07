using UnityEngine;
using BeauUtil;
using Aqua.Cameras;
using System.Collections;
using BeauRoutine;
using BeauUtil.UI;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;
using Aqua.Scripting;
using BeauUtil.Debugger;
using AquaAudio;

namespace Aqua.Shop {
    public class ShopCameraCtrl : MonoBehaviour, IBakedComponent, IScenePreloader, ISceneLoadHandler, ISceneUnloadHandler {
        static public readonly StringHash32 BelowEntrance = "station";
        static public readonly StringHash32 ShipEntrance = "ship";
        static public readonly StringHash32 ExitEntrance = "shop";
        
        static public readonly StringHash32 Trigger_ShopReady = "ShopReady";
        static public readonly StringHash32 Trigger_ShopViewTable = "ShopViewTable";
        static public readonly StringHash32 Trigger_ShopExit = "ShopExit";

        #region Inspector

        [SerializeField, HideInInspector] private ShopTable[] m_Tables;
        [SerializeField] private CameraPose m_BasePose = null;
        [SerializeField] private CameraPose m_OffscreenPose = null;
        [SerializeField] private ShopkeeperCtrl m_Shopkeeper = null;
        [SerializeField] private ShopStock m_Stock = null;
        [SerializeField] private CanvasGroup m_BackButtonGroup = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private CanvasGroup m_CurrencyGroup = null;

        #endregion // Inspector

        [NonSerialized] private ShopTable m_SelectedTable;
        [NonSerialized] private Routine m_EnterExitAnim;
        [NonSerialized] private AudioHandle m_BGM;

        #region Routines

        private IEnumerator EnterAnimation() {
            Services.Input.PauseAll();
            Services.UI.ShowLetterbox();
            yield return 0.2f;
            yield return Services.Camera.MoveToPose(m_BasePose, 1.5f, Curve.Smooth);
            Services.Camera.AddShake(new Vector2(0, 0.1f), new Vector2(0.1f, 0.1f), 0.25f);
            yield return 0.1f;
            OnShopReady(false);
            Services.UI.HideLetterbox();
            Services.Input.ResumeAll();
            yield return m_CurrencyGroup.Show(0.2f);
        }

        private IEnumerator SelectTableAnimation(ShopTable table) {
            Services.Data.SetVariable("shop:table", table.Id);
            Services.Input.PauseAll();
            yield return Services.Camera.MoveToPosition(table.CameraPose.transform.position, null, 0.3f, Curve.CubeOut, Axis.X);
            yield return Services.Camera.MoveToPose(table.CameraPose, 0.2f, Curve.Smooth);
            Services.Input.ResumeAll();
            using(var scriptTable = TempVarTable.Alloc()) {
                scriptTable.Set("tableId", table.Id);
                Services.Script.TriggerResponse(Trigger_ShopViewTable, scriptTable);
            }
        }

        private IEnumerator DeselectTableAnimation() {
            Services.Data.SetVariable("shop:table", null);
            Services.Input.PauseAll();
            yield return Services.Camera.MoveToPose(m_BasePose, 0.2f, Curve.Smooth, CameraPoseProperties.Position | CameraPoseProperties.Height, Axis.Y);
            yield return Services.Camera.MoveToPose(m_BasePose, 0.3f, Curve.CubeOut);
            Services.Input.ResumeAll();
        }

        private IEnumerator ExitAnimation(ScriptThreadHandle inCutscene) {
            yield return Routine.Combine(
                m_BackButtonGroup.Hide(0.2f),
                m_CurrencyGroup.Hide(0.2f),
                inCutscene.Wait()
            );

            Services.Input.PauseAll();
            Services.UI.ShowLetterbox();
            Services.Camera.AddShake(new Vector2(0, 0.1f), new Vector2(0.1f, 0.1f), 0.25f);
            yield return 0.2f;
            Services.Audio.StopMusic(1f);
            yield return Services.Camera.MoveToPose(m_OffscreenPose, 1.5f, Curve.Smooth);
            Routine.Start(BackToMap());
        }

        static private IEnumerator BackToMap() {
            StateUtil.LoadSceneWithWipe("Ship", ExitEntrance);
            yield return 0.3f;
            Services.UI.HideLetterbox();
            Services.Input.ResumeAll();
        }

        private void OnShopReady(bool inbCrossfade) {
            Services.Audio.SetMusic(m_BGM, inbCrossfade ? 0.2f : 0);
            Services.Script.TriggerResponse(Trigger_ShopReady);
        }

        #endregion // Routines

        #region Callbacks

        private void OnTableClicked(PointerEventData eventData) {
            PointerListener.TryGetComponentUserData<ShopTable>(eventData, out ShopTable table);
            m_SelectedTable = table;
            m_Shopkeeper.SetTable(table);
            Routine.Start(this, SelectTableAnimation(table)).TryManuallyUpdate(0);
            SetTablesSelectable(false);
            m_Stock.SetItemsSelectable(true);
        }

        private void OnBackClicked() {
            if (m_SelectedTable != null) {
                m_SelectedTable = null;
                m_Shopkeeper.SetTable(null);
                Routine.Start(this, DeselectTableAnimation()).TryManuallyUpdate(0);
                SetTablesSelectable(true);
                m_Stock.SetItemsSelectable(false);
            } else {
                var thread = Services.Script.TriggerResponse(Trigger_ShopExit);
                m_EnterExitAnim.Replace(this, ExitAnimation(thread));
            }
        }

        #endregion // Callbacks

        #region Table Management

        private void SetTablesSelectable(bool selectable) {
            foreach(var table in m_Tables) {
                table.Clickable.gameObject.SetActive(selectable);
            }
        }

        #endregion // Table Management

        #region Loading

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext) {
            m_BGM = Services.Audio.PostEvent("ShopBGM", AudioPlaybackFlags.PreloadOnly);
            return null;
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            foreach(var table in m_Tables) {
                table.Clickable.UserData = table;
                table.Clickable.onClick.AddListener(OnTableClicked);
            }

            m_BackButton.onClick.AddListener(OnBackClicked);

            if (BootParams.BootedFromCurrentScene || Services.State.LastEntranceId == BelowEntrance || Services.State.LastEntranceId == ShipEntrance) {
                Services.Camera.SnapToPose(m_OffscreenPose);
                m_CurrencyGroup.Hide();
                m_EnterExitAnim = Routine.Start(this, EnterAnimation());
                m_EnterExitAnim.TryManuallyUpdate(0);
            } else {
                OnShopReady(true);
            }
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext) {
            Services.Audio.StopMusic(0.2f);
        }

        #endregion // Loading

        #if UNITY_EDITOR

        void IBakedComponent.Bake() {
            m_Tables = FindObjectsOfType<ShopTable>();
        }

        #endif // UNITY_EDITOR
    }
}