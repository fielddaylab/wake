using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aqua.Scripting {
    public sealed class ScriptPostProcess : ScriptComponent {
        #region Inspector

        [SerializeField] private Volume m_Volume = null;
        [SerializeField] private VolumeProfile[] m_Profiles = null;

        #endregion // Inspector

        [NonSerialized] private Volume m_CrossfadeVolume;
        private Routine m_CrossfadeRoutine;

        private void Awake() {
            m_CrossfadeVolume = gameObject.AddComponent<Volume>();
            m_CrossfadeVolume.weight = 0;
            m_CrossfadeVolume.sharedProfile = null;
        }

        [LeafMember("SetPostProcessProfile")]
        public void SetProfile(StringSlice profileName, float crossFade = 0) {
            VolumeProfile profile = GetProfile(profileName);
            if (profile != null) {
                SetProfile(profile, crossFade);
            } else {
                Log.Error("[ScriptPostProcess] Profile with name '{0}' does not exist on this object '{1}'", profileName, Parent.Id());
            }
        }

        public void SetProfile(VolumeProfile profile, float crossFade = 0) {
            if (crossFade <= 0) {
                m_CrossfadeRoutine.Stop();
                m_Volume.sharedProfile = profile;
                m_Volume.weight = 1;
                ClearCrossFadeVolume();
                return;
            }

            if (m_CrossfadeRoutine) {
                CopyCrossFadeToMain();
            } else if (m_Volume.sharedProfile == profile) {
                return;
            }

            m_CrossfadeVolume.sharedProfile = profile;
            m_CrossfadeVolume.weight = 0;
            m_CrossfadeVolume.enabled =  true;
            m_Volume.weight = 1;
            m_CrossfadeRoutine.Replace(this, CrossFade(crossFade)).Tick();
        }

        private IEnumerator CrossFade(float duration) {
            yield return Routine.Combine(
                Tween.OneToZero((f) => m_Volume.weight = f, duration),
                Tween.ZeroToOne((f) => m_CrossfadeVolume.weight = f, duration)
            );
            CopyCrossFadeToMain();
        }

        private void CopyCrossFadeToMain() {
            m_Volume.sharedProfile = m_CrossfadeVolume.sharedProfile;
            m_Volume.weight = m_CrossfadeVolume.weight;
            ClearCrossFadeVolume();
        }

        private void ClearCrossFadeVolume() {
            m_CrossfadeVolume.sharedProfile = null;
            m_CrossfadeVolume.weight = 0;
            m_CrossfadeVolume.enabled = false;
        }

        public VolumeProfile GetProfile(StringSlice profileName) {
            foreach(var profile in m_Profiles) {
                if (profile.name == profileName) {
                    return profile;
                }
            }

            return null;
        }
    }
}