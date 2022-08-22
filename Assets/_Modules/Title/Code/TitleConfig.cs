using System.Collections;
using Aqua.Animation;
using Aqua.Cameras;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;

namespace Aqua.Title
{
    public class TitleConfig : MonoBehaviour
    {
        public CameraPose LoadingPose;
        public CameraPose FullPose;
        public CameraPose[] IntroPosePairs;
        public AmbientTransform WhaleTransform;
        public CameraDrift Drift;
    }
}