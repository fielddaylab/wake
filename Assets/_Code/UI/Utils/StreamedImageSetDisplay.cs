using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using EasyAssetStreaming;

namespace Aqua {
    public class StreamedImageSetDisplay : MonoBehaviour {
        
        [Header("Streamed")]
        [Required] public GameObject StreamedGroup;
        [Required] public StreamingUGUITexture Streamed;
        
        [Header("Fallback")]
        public GameObject FallbackGroup;
        public Image Fallback;

        [Header("Video")]
        public GameObject VideoGroup;
        public StreamedVideoImage Video;

        public void Display(StreamedImageSet set) {
            if (!string.IsNullOrEmpty(set.Path)) {
                if (Video && IsVideoPath(set.Path)) {
                    HideStreaming(Streamed, StreamedGroup);
                    HideFallback(Fallback, FallbackGroup);
                    ShowVideo(Video, VideoGroup, set.Path);
                } else {
                    HideVideo(Video, VideoGroup);
                    HideFallback(Fallback, FallbackGroup);
                    ShowStreaming(Streamed, StreamedGroup, set.Path);
                }
            } else if (Fallback && set.Fallback != null) {
                HideVideo(Video, VideoGroup);
                HideStreaming(Streamed, StreamedGroup);
                ShowFallback(Fallback, FallbackGroup, set.Fallback);
            } else {
                Clear();
            }
        }

        public void Clear() {
            HideVideo(Video, VideoGroup);
            HideStreaming(Streamed, StreamedGroup);
            HideFallback(Fallback, FallbackGroup);
        }

        static private void ShowVideo(StreamedVideoImage video, GameObject group, string url) {
            if (!video) {
                return;
            }

            video.URL = url;
            group.SetActive(true);
        }

        static private void HideVideo(StreamedVideoImage video, GameObject group) {
            if (!video) {
                return;
            }

            video.URL = string.Empty;
            group.SetActive(false);
        }

        static private void ShowStreaming(StreamingUGUITexture image, GameObject group, string url) {
            if (!image) {
                return;
            }

            image.Path = url;
            group.SetActive(true);
        }

        static private void HideStreaming(StreamingUGUITexture image, GameObject group) {
            if (!image) {
                return;
            }

            image.Path = null;
            group.SetActive(false);
        }

        static private void ShowFallback(Image image, GameObject group, Sprite sprite) {
            if (!image) {
                return;
            }

            image.sprite = sprite;
            group.SetActive(true);
        }

        static private void HideFallback(Image image, GameObject group) {
            if (!image) {
                return;
            }

            image.sprite = null;;
            group.SetActive(false);
        }

        static private bool IsVideoPath(string path) {
            return path.EndsWith(".mp4") || path.EndsWith(".webm");
        }
    }
}