using System.Collections;
using Aqua.Animation;
using Aqua.Cameras;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using TMPro;
using UnityEngine;

namespace Aqua.Title
{
    public class TitleConfig : MonoBehaviour, IBaked
    {
        public CameraPose LoadingPose;
        public CameraPose FullPose;
        public CameraPose[] IntroPosePairs;
        public AmbientTransform WhaleTransform;
        public CameraDrift Drift;
        public Transform[] Specters;
        public Transform[] Dreams;

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            foreach(var specter in Specters) {
                specter.gameObject.SetActive(false);
            }

            foreach(var dream in Dreams) {
                if (dream) {
                    dream.gameObject.SetActive(false);
                }
            }

            return true;
        }

        #endif // UNITY_EDITOR
    }
}