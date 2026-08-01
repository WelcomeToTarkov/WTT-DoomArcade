using UnityEngine;
using UnityEngine.Audio;

public class ArcadeAudioBus : MonoBehaviour
{
    public static ArcadeAudioBus Instance;

    [Header("Mixer (optional)")]
    [SerializeField] private AudioMixerGroup arcadeGroup;

    [Header("Audio Settings")]
    [SerializeField] private float volume = 1f;
    [SerializeField] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;

    private AudioSource speaker;
    private AudioSource loopSpeaker;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        speaker = CreateSource("ArcadeSpeaker_OneShot", false);
        loopSpeaker = CreateSource("ArcadeSpeaker_Loop", true);
    }

    private AudioSource CreateSource(string name, bool loop)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.zero;

        var src = obj.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = spatialBlend;
        src.volume = volume;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;

        if (arcadeGroup != null)
            src.outputAudioMixerGroup = arcadeGroup;

        return src;
    }

    public void PlayAtCabinet(AudioClip clip, float volumeMul = 1f)
    {
        if (!clip || speaker == null) return;
        speaker.PlayOneShot(clip, volumeMul);
    }

    public void StartLoopAtCabinet(AudioClip clip, float volumeMul = 1f)
    {
        if (!clip || loopSpeaker == null) return;

        if (loopSpeaker.isPlaying && loopSpeaker.clip == clip)
            return;

        loopSpeaker.Stop();
        loopSpeaker.clip = clip;
        loopSpeaker.volume = volume * volumeMul;
        loopSpeaker.loop = true;
        loopSpeaker.Play();
    }

    public void StopLoopAtCabinet()
    {
        if (loopSpeaker == null) return;

        loopSpeaker.Stop();
        loopSpeaker.clip = null;
    }
}