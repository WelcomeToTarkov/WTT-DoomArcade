using System.Collections;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager instance;

        [Header("Clips")]

        public AudioClip[] mainMenuThemes;
        public AudioClip[] levelThemes;

        public AudioClip[] victoryIntros;
        public AudioClip[] victoryLoops; 

        private AudioClip _lastMenuClip;
        private AudioClip _lastLevelClip;
        private AudioClip _lastVictoryIntro;
        private AudioClip _lastVictoryLoop;
        private AudioSource source;
        private Coroutine _sequenceRoutine;
        private AudioClip GetRandomClip(AudioClip[] pool, ref AudioClip last)
        {
            if (pool == null || pool.Length == 0) return null;
            if (pool.Length == 1) {
                last = pool[0];
                return last;
            }

            AudioClip chosen;
            int safety = 0;
            do {
                chosen = pool[Random.Range(0, pool.Length)];
                safety++;
            } while (chosen == last && safety < 10);

            last = chosen;
            return chosen;
        }
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.5f;
            source.loop = false;
        }

        void PlayClip(AudioClip clip, bool loop)
        {
            if (clip == null)
            {
                return;
            }

            // Cancel any sequence that might be waiting
            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }

            if (source.clip == clip && source.isPlaying && source.loop == loop)
                return;


            source.loop = loop;
            source.clip = clip;
            source.Play();
        }

        IEnumerator PlaySequence(AudioClip first, AudioClip second, bool loopSecond)
        {
            if (first != null)
            {
                PlayClip(first, false);
                yield return new WaitForSeconds(first.length);
            }

            if (second != null)
            {
                PlayClip(second, loopSecond);
            }

            _sequenceRoutine = null;
        }

        public void PlayMenuMusic()
        {
            var clip = GetRandomClip(mainMenuThemes, ref _lastMenuClip);
            PlayClip(clip, true);
        }

        public void PlayLevelMusic()
        {
            var clip = GetRandomClip(levelThemes, ref _lastLevelClip);
            PlayClip(clip, true);
        }

        public void PlayVictorySequence()
        {
            if (_sequenceRoutine != null)
                StopCoroutine(_sequenceRoutine);

            var intro = GetRandomClip(victoryIntros, ref _lastVictoryIntro);
            var loop  = GetRandomClip(victoryLoops,  ref _lastVictoryLoop);

            _sequenceRoutine = StartCoroutine(PlaySequence(intro, loop, true));
        }

        public void StopMusic()
        {
            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }

            source.Stop();
            source.clip = null;
        }
    }
}
