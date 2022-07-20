using UnityEngine;
using BeauUtil;
using BeauUtil.UI;
using System;
using TMPro;
using UnityEngine.UI;
using Aqua.Cameras;
using EasyAssetStreaming;
using ScriptableBake;
using BeauRoutine;
using Aqua.Animation;
using System.Collections;

namespace Aqua.Shop
{
    public class ShopPreview : MonoBehaviour, IBaked, ISceneLoadHandler {

        #region Inspector

        [Header("Science")]
        [SerializeField] private ActiveGroup m_ScienceRootGroup = null;
        [SerializeField] private ActiveGroup m_SciencePreviewGroup = null;
        [SerializeField] private StreamingQuadTexture m_SciencePreview = null;

        [Header("Exploration")]
        [SerializeField] private ActiveGroup m_ExplorationRootGroup = null;
        [SerializeField] private Transform m_SubRotateJoint = null;
        [SerializeField] private ShopPreviewShipItem[] m_ExplorationItems = null;
        [SerializeField] private TweenSettings m_SubRotateTween = new TweenSettings(0.5f);

        #endregion // Inspector

        [NonSerialized] private Routine m_ExplorePreviewTransition;
        [NonSerialized] private Routine m_SciencePreviewTransition;
        [NonSerialized] private bool m_PreviewEnabled;
        [NonSerialized] private ShopPreviewShipItem m_CurrentSubPreview;
        [NonSerialized] private ShopBoard.CategoryId m_CurrentCategory;

        private void Start() {
            m_ScienceRootGroup.ForceActive(false);
            m_SciencePreviewGroup.ForceActive(false);
            m_ExplorationRootGroup.ForceActive(false);
        }

        public void SetCategory(ShopBoard.CategoryId category) {
            m_ScienceRootGroup.SetActive(category == ShopBoard.CategoryId.Science);
            m_ExplorationRootGroup.SetActive(category == ShopBoard.CategoryId.Exploration);
            m_CurrentCategory = category;
        }

        public void ClearCategory() {
            m_ScienceRootGroup.SetActive(false);
            m_ExplorationRootGroup.SetActive(false);
            m_CurrentCategory = ShopBoard.CategoryId.NONE;
        }

        public void ShowPreview(InvItem item) {
            switch(m_CurrentCategory) {
                case ShopBoard.CategoryId.Exploration: {
                    foreach(var preview in m_ExplorationItems) {
                        if (preview.ItemId != item.Id()) {
                            continue;
                        }

                        m_CurrentSubPreview = preview;
                        m_PreviewEnabled = true;
                        SetAsPreview(preview);
                        m_ExplorePreviewTransition.Replace(this, RotateSubJoint(preview.Rotation));
                        break;
                    }
                    break;
                }

                case ShopBoard.CategoryId.Science: {
                    m_SciencePreviewGroup.SetActive(true);
                    var hiRes = item.SketchPath();
                    if (!string.IsNullOrEmpty(hiRes)) {
                        m_SciencePreview.Path = hiRes;
                    } else {
                        m_SciencePreview.Path = string.Empty;
                    }
                    break;
                }
            }
        }

        public void HidePreview() {
            if (m_CurrentSubPreview != null) {
                if (!m_CurrentSubPreview.IsPurchased) {
                    SetAsHidden(m_CurrentSubPreview);
                }
                m_CurrentSubPreview = null;
                m_ExplorePreviewTransition.Replace(this, RotateSubJoint(0));
            }

            if (m_SciencePreviewGroup.Active) {
                m_SciencePreview.Unload();
                m_SciencePreviewGroup.SetActive(false);
            }
        }

        public IEnumerator AnimatePurchase() {
            if (m_CurrentSubPreview != null) {
                foreach(var particleSystem in m_CurrentSubPreview.WeldParticles) {
                    particleSystem.Play(true);
                }
                Services.Audio.PostEvent("Shop.Weld");
                yield return 2.2f;
                Services.UI.WorldFaders.Flash(Color.white, 0.3f);
                SetAsActive(m_CurrentSubPreview);
                Services.Audio.PostEvent("Shop.FinishWelding");
                yield return 0.5;
            }
        }

        private IEnumerator RotateSubJoint(float newAngle) {
            yield return m_SubRotateJoint.RotateTo(newAngle, m_SubRotateTween, Axis.Y, Space.Self);
        }

        #region ISceneLoadHandler

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            foreach(var preview in m_ExplorationItems) {
                if (Save.Inventory.HasUpgrade(preview.ItemId)) {
                    SetAsActive(preview);
                } else {
                    SetAsHidden(preview);
                }
            }
        }

        #endregion // ISceneLoadHandler

        #if UNITY_EDITOR

        int IBaked.Order => 0;

        bool IBaked.Bake(BakeFlags flags) {
            m_SubRotateJoint = GameObject.Find("GrabberEnd_end")?.transform;
            m_ExplorationItems = FindObjectsOfType<ShopPreviewShipItem>();
            return true;
        }

        private void Reset() {
            m_SubRotateJoint = GameObject.Find("GrabberEnd_end")?.transform;
            m_ExplorationItems = FindObjectsOfType<ShopPreviewShipItem>();
        }

        #endif // UNITY_EDITOR

        static public void SetAsPreview(ShopPreviewShipItem item) {
            foreach(var mesh in item.Meshes) {
                mesh.enabled = true;
                mesh.sharedMaterial = item.PreviewMaterial;
            }
        }

        static public void SetAsActive(ShopPreviewShipItem item) {
            foreach(var mesh in item.Meshes) {
                mesh.enabled = true;
                mesh.sharedMaterial = item.ActiveMaterial;
            }
            item.IsPurchased = true;
        }

        static public void SetAsHidden(ShopPreviewShipItem item) {
            foreach(var mesh in item.Meshes) {
                mesh.enabled = false;
            }
            item.IsPurchased = false;
        }

    }
}