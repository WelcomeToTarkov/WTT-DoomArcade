using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = UnityEngine.Random;

namespace DoomArcade.Scripts.DoomClone
{
    public class VideoBG : MonoBehaviour
    {
        [SerializeField] VideoClip[] videoClips;
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;
        private VideoClip[] shuffledClips;

        void Start()
        {
            renderTexture = new RenderTexture(320, 240, 24);
            GetComponent<RawImage>().texture = renderTexture;

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = false;
            videoPlayer.SetDirectAudioVolume(0, 0.2f);
            shuffledClips = videoClips.OrderBy(x => Random.value).ToArray();
            videoPlayer.clip = shuffledClips[0];
            videoPlayer.Play();
        
            videoPlayer.loopPointReached += OnVideoEnd;
        }

        void OnVideoEnd(VideoPlayer vp)
        {
            int nextIndex = (Array.IndexOf(shuffledClips, vp.clip) + 1) % shuffledClips.Length;
            vp.clip = shuffledClips[nextIndex];
            vp.Play();
        }

        void OnDestroy()
        {
            renderTexture?.Release();
        }
    }
}