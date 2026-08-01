using UnityEngine;
using UnityEngine.Audio;

namespace DoomArcade.Scripts.Arcade
{
    public class ArcadeMenuMusic : MonoBehaviour
    {
        public static ArcadeMenuMusic Instance;

        public AudioClip menuLoop;
        public AudioMixerGroup outputGroup;
        private AudioSource _source;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.volume = 0.5f;
            _source.spatialBlend = 1f;
            if (outputGroup != null)
                _source.outputAudioMixerGroup = outputGroup;
        }

        public void PlayMenuMusic()
        {
            if (menuLoop == null)
            {
                Debug.LogWarning("[ArcadeMenuMusic] menuLoop is null");
                return;
            }

            if (_source.isPlaying && _source.clip == menuLoop)
                return;

            _source.clip = menuLoop;
            _source.Play();
        }

        public void StopMenuMusic()
        {
            if (_source.isPlaying)
                _source.Stop();
        }
    }
}