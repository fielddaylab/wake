using System;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUWT;
using UnityEngine;
using UnityEngine.Serialization;
using Aqua;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace AquaAudio {
    
    [CreateAssetMenu(menuName = "Aqualab/Audio Event")]
    public class AudioEvent : ScriptableObject, IKeyValuePair<StringHash32, AudioEvent> {
        public enum PlaybackMode : byte {
            Sample,
            Stream
        }

        #region Inspector

        [SerializeField, AutoEnum] private PlaybackMode m_Mode = PlaybackMode.Sample;
        [SerializeField, AutoEnum] private AudioBusId m_Bus = AudioBusId.SFX;
        
        [SerializeField, StreamingPath("mp3")] private string m_StreamingPath = null;
        [SerializeField, FormerlySerializedAs("m_Clips"), Required] private AudioClip[] m_Samples = null;

        [SerializeField] private FloatRange m_Volume = new FloatRange(1);
        [SerializeField] private FloatRange m_Pitch = new FloatRange(1);
        [SerializeField] private FloatRange m_Delay = new FloatRange(0);
        [SerializeField] private bool m_Loop = false;
        [SerializeField] private bool m_RandomizeStartingPosition = false;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;
        [NonSerialized] private RandomDeck<AudioClip> m_ClipDeck;

        #region IKeyValuePair

        StringHash32 IKeyValuePair<StringHash32, AudioEvent>.Key { get { return Id(); } }
        AudioEvent IKeyValuePair<StringHash32, AudioEvent>.Value { get { return this; } }

        #endregion // IKeyValuePair

        public StringHash32 Id() { return !m_Id.IsEmpty ? m_Id : (m_Id = name); }
        public PlaybackMode Mode() { return m_Mode; }
        public AudioBusId Bus() { return m_Bus; }
        public bool Looping() { return m_Loop; }

        public bool CanPlay() {
            if (m_Mode == PlaybackMode.Stream) {
                return !string.IsNullOrEmpty(m_StreamingPath);
            } else {
                return m_Samples.Length > 0;
            }
        }

        public string StreamingPath() {
            Assert.True(m_Mode == PlaybackMode.Stream, "Event '{0}' is not a stream event", name);
            return m_StreamingPath;
        }

        #region Streaming

        public void LoadStream(UWTStreamPlayer inPlayer, System.Random inRandom, out AudioPropertyBlock outProperties, out float outDelay) {
            Assert.True(m_Mode == PlaybackMode.Stream, "Event '{0}' is not a stream event", name);

            inPlayer.SetURLFromStreamingAssets(m_StreamingPath);
            inPlayer.Loop = m_Loop;

            outProperties.Volume = m_Volume.Generate(inRandom);
            outProperties.Pitch = 1;
            outDelay = m_Delay.Generate(inRandom);

            outProperties.Mute = false;
            outProperties.Pause = false;
        }

        #endregion // Streaming

        #region Sample

        public void LoadSample(AudioSource inSource, System.Random inRandom, out AudioPropertyBlock outProperties, out float outDelay) {
            Assert.True(m_Mode == PlaybackMode.Sample, "Event '{0}' is not a sample event", name);

            inSource.clip = GetNextClip(inRandom);
            inSource.loop = m_Loop;

            if (m_Loop && m_RandomizeStartingPosition)
                inSource.time = inRandom.NextFloat(inSource.clip.length);
            else
                inSource.time = 0;

            outProperties.Volume = m_Volume.Generate(inRandom);
            outProperties.Pitch = m_Pitch.Generate(inRandom);
            outDelay = m_Delay.Generate(inRandom);

            outProperties.Mute = false;
            outProperties.Pause = false;
        }

        private AudioClip GetNextClip(System.Random inRandom) {
            if (m_ClipDeck == null)
                m_ClipDeck = new RandomDeck<AudioClip>(m_Samples);

            return m_ClipDeck.Next(inRandom);
        }

        #endregion // Sample

        #if UNITY_EDITOR

        static public void BuildManifestFromEventString(string eventId, SceneManifestBuilder manifest) {
            AudioEvent evt = ValidationUtils.FindAsset<AudioEvent>(eventId);
            if (evt != null) {
                manifest.Assets.Add(evt);
                if (evt.Mode() == AudioEvent.PlaybackMode.Stream) {
                    manifest.Paths.Add(evt.StreamingPath());
                }
            }
        }

        // TODO:    Add commands to generate AudioEvent from an AudioClip,
        //          or combine multiple AudioClips into a single AudioEvent

        [UnityEditor.CustomEditor(typeof(AudioEvent)), UnityEditor.CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor {
            
            private SerializedProperty m_ModeProperty;
            private SerializedProperty m_BusProperty;
            private SerializedProperty m_StreamingPathProperty;
            private SerializedProperty m_SamplesProperty;
            private SerializedProperty m_VolumeProperty;
            private SerializedProperty m_PitchProperty;
            private SerializedProperty m_DelayProperty;
            private SerializedProperty m_LoopProperty;
            private SerializedProperty m_RandomizeStartingPositionProperty;

            private void OnEnable() {
                m_ModeProperty = serializedObject.FindProperty("m_Mode");
                m_BusProperty = serializedObject.FindProperty("m_Bus");
                m_StreamingPathProperty = serializedObject.FindProperty("m_StreamingPath");
                m_SamplesProperty = serializedObject.FindProperty("m_Samples");
                m_VolumeProperty = serializedObject.FindProperty("m_Volume");
                m_PitchProperty = serializedObject.FindProperty("m_Pitch");
                m_DelayProperty = serializedObject.FindProperty("m_Delay");
                m_LoopProperty = serializedObject.FindProperty("m_Loop");
                m_RandomizeStartingPositionProperty = serializedObject.FindProperty("m_RandomizeStartingPosition");
            }

            public override void OnInspectorGUI() {
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUILayout.PropertyField(m_BusProperty);
                EditorGUILayout.PropertyField(m_ModeProperty);
                EditorGUILayout.Space();

                if (m_ModeProperty.hasMultipleDifferentValues) {
                    EditorGUILayout.HelpBox("Cannot edit multiple objects with different modes", MessageType.Warning);
                } else {
                    switch((PlaybackMode) m_ModeProperty.intValue) {
                        case PlaybackMode.Sample: {
                            RenderSample();
                            break;
                        }
                        case PlaybackMode.Stream: {
                            RenderStream();
                            break;
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }

            private void RenderSample() {
                EditorGUILayout.LabelField("Sample", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_SamplesProperty, true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_VolumeProperty, true);
                EditorGUILayout.PropertyField(m_PitchProperty, true);
                EditorGUILayout.PropertyField(m_DelayProperty, true);

                EditorGUILayout.PropertyField(m_LoopProperty, true);
                if (!m_LoopProperty.hasMultipleDifferentValues && m_LoopProperty.boolValue) {
                    EditorGUILayout.PropertyField(m_RandomizeStartingPositionProperty);
                }
            }

            private void RenderStream() {
                EditorGUILayout.LabelField("Stream", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_StreamingPathProperty);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_VolumeProperty, true);
                EditorGUILayout.PropertyField(m_DelayProperty, true);

                EditorGUILayout.PropertyField(m_LoopProperty, true);
            }
        }

        #endif // UNITY_EDITOR
    }
}