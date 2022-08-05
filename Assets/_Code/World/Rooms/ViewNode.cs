using System;
using System.Collections;
using Aqua.Cameras;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.View {
    public sealed class ViewNode : MonoBehaviour, IKeyValuePair<StringHash32, ViewNode> {
        public SerializedHash32 Id;
        public CameraPose Camera;
        public ActiveGroup Group;
        public ScriptInteractionGroup InteractionGroup;
        public Canvas UI;
        public SerializedHash32[] GroupIds;

        public ScriptAudioTrackVariants AudioLayers;
        public SerializedHash32 AudioLayerId;

        public Action OnLoad;
        public Action OnEnter;
        public Action OnExit;

        #region IKeyValuePair

        public StringHash32 Key { get { return Id; } }
        public ViewNode Value { get { return this; } }

        #endregion // IKeyValuePair
    }
}