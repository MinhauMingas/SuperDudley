using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- Struct to hold track info ---
[System.Serializable]
public class AudioTrack
{
    [Tooltip("A descriptive name for this track (used for selection).")]
    public string name = "New Track";
    [Tooltip("The audio file for this track.")]
    public AudioClip clip;
    [Tooltip("The target volume for this specific track.")]
    [Range(0f, 1f)]
    public float volume = 0.8f;
    [Tooltip("Does this specific track loop? (Mainly for Music/Ambience)")]
    public bool loop = true;
}

public class AudioManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static AudioManager Instance { get; private set; }

    // --- Inspector Settings ---
    [Header("Music Settings")]
    public AudioTrack[] musicTracks;
    public float musicCrossfadeDuration = 1.5f;

    [Header("Ambience Settings")]
    public AudioTrack[] ambienceTracks;
    public float ambienceCrossfadeDuration = 2.0f;

    [Header("SFX Settings")]
    public AudioTrack[] sfxTracks;
    [Range(0f, 1f)]
    public float sfxMasterVolume = 0.9f;
    [Range(0f, 1f)]
    public float sfxAmbienceDuckAmount = 0.5f;
    public float sfxAmbienceDuckDuration = 0.5f;

    // --- Audio Sources ---
    private AudioSource musicSource1;
    private AudioSource musicSource2;
    private AudioSource ambienceSource1;
    private AudioSource ambienceSource2;
    private AudioSource sfxSource;

    // --- Active Source Tracking ---
    private AudioSource activeMusicSource;
    private AudioSource activeAmbienceSource;

    // --- Internal State ---
    private Coroutine musicFadeCoroutine;
    private Coroutine ambienceFadeCoroutine;
    private Coroutine ambienceDuckCoroutine;
    private AudioTrack currentMusicTrack;
    private AudioTrack currentAmbienceTrack; // Correct variable name

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Setup Audio Sources
            musicSource1 = InitializeAudioSource("Music Source 1", true, 0f);
            musicSource2 = InitializeAudioSource("Music Source 2", true, 0f);
            ambienceSource1 = InitializeAudioSource("Ambience Source 1", true, 0f);
            ambienceSource2 = InitializeAudioSource("Ambience Source 2", true, 0f);
            sfxSource = InitializeAudioSource("SFX Source", false, sfxMasterVolume);
            // Initial state
            activeMusicSource = musicSource1;
            activeAmbienceSource = ambienceSource1;
            currentMusicTrack = null;
            currentAmbienceTrack = null;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private AudioSource InitializeAudioSource(string name, bool defaultLoop, float initialVolume)
    {
        GameObject sourceGO = new GameObject(name);
        sourceGO.transform.SetParent(this.transform);
        AudioSource source = sourceGO.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = defaultLoop;
        source.volume = initialVolume;
        return source;
    }

    // --- Music Control Methods ---
    public int FindMusicTrackIndex(string trackName) { if (string.IsNullOrEmpty(trackName)) return -1; for (int i = 0; i < musicTracks.Length; i++) { if (musicTracks[i].name == trackName) { if (musicTracks[i].clip == null) { Debug.LogWarning($"AudioManager: Music track named '{trackName}' found at index {i}, but its AudioClip is null.", this); return -1; } return i; } } Debug.LogWarning($"AudioManager: Music track named '{trackName}' not found.", this); return -1; }
    public void PlayMusic(int trackIndex) { PlayMusicInternal(trackIndex, musicCrossfadeDuration); }
    public void PlayMusic(int trackIndex, float fadeDuration) { PlayMusicInternal(trackIndex, fadeDuration); }
    public void PlayMusic(string trackName) { int index = FindMusicTrackIndex(trackName); if (index != -1) PlayMusicInternal(index, musicCrossfadeDuration); }
    public void PlayMusic(string trackName, float fadeDuration) { int index = FindMusicTrackIndex(trackName); if (index != -1) PlayMusicInternal(index, fadeDuration); }
    private void PlayMusicInternal(int trackIndex, float fadeDurationOverride) { if (trackIndex < 0 || trackIndex >= musicTracks.Length) { Debug.LogWarning($"AudioManager: Invalid music track index: {trackIndex}"); return; } AudioTrack trackToPlay = musicTracks[trackIndex]; if (trackToPlay.clip == null) { Debug.LogWarning($"AudioManager: Music track '{trackToPlay.name}' (Index: {trackIndex}) has a null AudioClip."); return; } if (currentMusicTrack == trackToPlay && activeMusicSource.isPlaying && activeMusicSource.clip == trackToPlay.clip && musicFadeCoroutine == null) { return; } if (currentMusicTrack == trackToPlay && activeMusicSource.clip == trackToPlay.clip) { return; } float duration = (fadeDurationOverride >= 0) ? fadeDurationOverride : musicCrossfadeDuration; float targetVolume = trackToPlay.volume; AudioSource newActiveSource = (activeMusicSource == musicSource1) ? musicSource2 : musicSource1; AudioSource oldActiveSource = activeMusicSource; newActiveSource.clip = trackToPlay.clip; newActiveSource.loop = trackToPlay.loop; newActiveSource.volume = 0f; newActiveSource.Play(); if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine); musicFadeCoroutine = StartCoroutine(FadeAudioSource(oldActiveSource, newActiveSource, targetVolume, duration, true)); activeMusicSource = newActiveSource; currentMusicTrack = trackToPlay; Debug.Log($"AudioManager: Crossfading Music to '{trackToPlay.name}' over {duration}s"); }
    public void FadeOutMusic(float duration) { if (currentMusicTrack == null && (musicSource1 == null || !musicSource1.isPlaying) && (musicSource2 == null || !musicSource2.isPlaying)) { Debug.Log("AudioManager: No music playing to fade out."); return; } Debug.Log($"AudioManager: Fading out Music over {duration}s"); if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); musicFadeCoroutine = null; } AudioSource sourceToFade = activeMusicSource; AudioSource otherSource = (activeMusicSource == musicSource1) ? musicSource2 : musicSource1; if (otherSource != null && otherSource.isPlaying) { otherSource.Stop(); otherSource.clip = null; } if (sourceToFade != null && sourceToFade.isPlaying) { musicFadeCoroutine = StartCoroutine(FadeOutSingleSourceCoroutine(sourceToFade, duration, true)); } else { if (sourceToFade != null) sourceToFade.Stop(); } currentMusicTrack = null; }
    public void StopMusic() { if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); musicFadeCoroutine = null; } if (musicSource1 != null && musicSource1.isPlaying) musicSource1.Stop(); if (musicSource2 != null && musicSource2.isPlaying) musicSource2.Stop(); currentMusicTrack = null; Debug.Log("AudioManager: All Music Stopped (Immediately)"); }
    public void PauseMusic() { bool p = false; if (activeMusicSource != null && activeMusicSource.isPlaying) { activeMusicSource.Pause(); p = true; } AudioSource iS = (activeMusicSource == musicSource1) ? musicSource2 : musicSource1; if (musicFadeCoroutine != null && iS != null && iS.isPlaying) { iS.Pause(); p = true; } if (p) Debug.Log("AudioManager: Music Paused"); }
    public void ResumeMusic() { bool r = false; float targetVolume = (currentMusicTrack != null) ? currentMusicTrack.volume : 0f; if (activeMusicSource != null && !activeMusicSource.isPlaying && activeMusicSource.time > 0) { if (musicFadeCoroutine == null) activeMusicSource.volume = targetVolume; activeMusicSource.UnPause(); r = true; } AudioSource iS = (activeMusicSource == musicSource1) ? musicSource2 : musicSource1; if (musicFadeCoroutine != null && iS != null && !iS.isPlaying && iS.time > 0) { iS.UnPause(); r = true; } if (r) Debug.Log("AudioManager: Music Resumed"); }
    public void ToggleMusic(bool isOn) { if (isOn) ResumeMusic(); else PauseMusic(); }


    // --- Ambience Control Methods ---
    public int FindAmbienceTrackIndex(string trackName) { if (string.IsNullOrEmpty(trackName)) return -1; for (int i = 0; i < ambienceTracks.Length; i++) { if (ambienceTracks[i].name == trackName) { if (ambienceTracks[i].clip == null) { Debug.LogWarning($"AudioManager: Ambience track named '{trackName}' found at index {i}, but its AudioClip is null.", this); return -1; } return i; } } Debug.LogWarning($"AudioManager: Ambience track named '{trackName}' not found.", this); return -1; }
    public void PlayAmbience(int trackIndex) { PlayAmbienceInternal(trackIndex, ambienceCrossfadeDuration); }
    public void PlayAmbience(int trackIndex, float fadeDuration) { PlayAmbienceInternal(trackIndex, fadeDuration); }
    public void PlayAmbience(string trackName) { int index = FindAmbienceTrackIndex(trackName); if (index != -1) PlayAmbienceInternal(index, ambienceCrossfadeDuration); }
    public void PlayAmbience(string trackName, float fadeDuration) { int index = FindAmbienceTrackIndex(trackName); if (index != -1) PlayAmbienceInternal(index, fadeDuration); }
    private void PlayAmbienceInternal(int trackIndex, float fadeDurationOverride) { if (trackIndex < 0 || trackIndex >= ambienceTracks.Length) { return; } AudioTrack trackToPlay = ambienceTracks[trackIndex]; if (trackToPlay.clip == null) { return; } if (currentAmbienceTrack == trackToPlay && activeAmbienceSource != null && activeAmbienceSource.isPlaying && activeAmbienceSource.clip == trackToPlay.clip && ambienceFadeCoroutine == null && ambienceDuckCoroutine == null) { return; } if (currentAmbienceTrack == trackToPlay && activeAmbienceSource != null && activeAmbienceSource.clip == trackToPlay.clip) { return; } float duration = (fadeDurationOverride >= 0) ? fadeDurationOverride : ambienceCrossfadeDuration; float targetVolume = trackToPlay.volume; AudioSource newActiveSource = (activeAmbienceSource == ambienceSource1) ? ambienceSource2 : ambienceSource1; AudioSource oldActiveSource = activeAmbienceSource; if (newActiveSource == null) { Debug.LogError("Failed to get new active ambience source"); return; } newActiveSource.clip = trackToPlay.clip; newActiveSource.loop = trackToPlay.loop; newActiveSource.volume = 0f; newActiveSource.Play(); if (ambienceFadeCoroutine != null) StopCoroutine(ambienceFadeCoroutine); if (ambienceDuckCoroutine != null) { StopCoroutine(ambienceDuckCoroutine); ambienceDuckCoroutine = null; } ambienceFadeCoroutine = StartCoroutine(FadeAudioSource(oldActiveSource, newActiveSource, targetVolume, duration, false)); activeAmbienceSource = newActiveSource; currentAmbienceTrack = trackToPlay; Debug.Log($"AudioManager: Crossfading Ambience to '{trackToPlay.name}' over {duration}s"); }
    public void FadeOutAmbience(float duration) { if (currentAmbienceTrack == null && (ambienceSource1 == null || !ambienceSource1.isPlaying) && (ambienceSource2 == null || !ambienceSource2.isPlaying)) { Debug.Log("AudioManager: No ambience playing to fade out."); return; } Debug.Log($"AudioManager: Fading out Ambience over {duration}s"); if (ambienceFadeCoroutine != null) { StopCoroutine(ambienceFadeCoroutine); ambienceFadeCoroutine = null; } if (ambienceDuckCoroutine != null) { StopCoroutine(ambienceDuckCoroutine); ambienceDuckCoroutine = null; } AudioSource sourceToFade = activeAmbienceSource; AudioSource otherSource = (activeAmbienceSource == ambienceSource1) ? ambienceSource2 : ambienceSource1; if (otherSource != null && otherSource.isPlaying) { otherSource.Stop(); otherSource.clip = null; } if (sourceToFade != null && sourceToFade.isPlaying) { ambienceFadeCoroutine = StartCoroutine(FadeOutSingleSourceCoroutine(sourceToFade, duration, false)); } else { if (sourceToFade != null) sourceToFade.Stop(); } currentAmbienceTrack = null; }
    public void StopAmbience() { if (ambienceFadeCoroutine != null) { StopCoroutine(ambienceFadeCoroutine); ambienceFadeCoroutine = null; } if (ambienceDuckCoroutine != null) { StopCoroutine(ambienceDuckCoroutine); ambienceDuckCoroutine = null; } if (ambienceSource1 != null && ambienceSource1.isPlaying) ambienceSource1.Stop(); if (ambienceSource2 != null && ambienceSource2.isPlaying) ambienceSource2.Stop(); currentAmbienceTrack = null; Debug.Log("AudioManager: All Ambience Stopped (Immediately)"); }
    public void PauseAmbience() { bool p = false; if (activeAmbienceSource != null && activeAmbienceSource.isPlaying) { activeAmbienceSource.Pause(); p = true; } AudioSource iS = (activeAmbienceSource == ambienceSource1) ? ambienceSource2 : ambienceSource1; if (ambienceFadeCoroutine != null && iS != null && iS.isPlaying) { iS.Pause(); p = true; } if (p) Debug.Log("AudioManager: Ambience Paused"); }
    public void ResumeAmbience() { bool r = false; float targetVolume = (currentAmbienceTrack != null) ? currentAmbienceTrack.volume : 0f; if (activeAmbienceSource != null && !activeAmbienceSource.isPlaying && activeAmbienceSource.time > 0) { if (ambienceFadeCoroutine == null && ambienceDuckCoroutine == null) { activeAmbienceSource.volume = targetVolume; } activeAmbienceSource.UnPause(); r = true; } AudioSource iS = (activeAmbienceSource == ambienceSource1) ? ambienceSource2 : ambienceSource1; if (ambienceFadeCoroutine != null && iS != null && !iS.isPlaying && iS.time > 0) { iS.UnPause(); r = true; } if (r) Debug.Log("AudioManager: Ambience Resumed"); }
    public void ToggleAmbience(bool isOn) { if (isOn) ResumeAmbience(); else PauseAmbience(); }


    // --- SFX Control Methods ---
    public int FindSfxTrackIndex(string trackName) { if (string.IsNullOrEmpty(trackName)) return -1; for (int i = 0; i < sfxTracks.Length; i++) { if (sfxTracks[i].name == trackName) { if (sfxTracks[i].clip == null) { Debug.LogWarning($"AudioManager: SFX track named '{trackName}' found at index {i}, but its AudioClip is null.", this); return -1; } return i; } } Debug.LogWarning($"AudioManager: SFX track named '{trackName}' not found.", this); return -1; }
    public void PlaySFX(string sfxName) { int index = FindSfxTrackIndex(sfxName); if (index != -1) PlaySFXInternal(index); }
    private void PlaySFXInternal(int trackIndex) { if (trackIndex < 0 || trackIndex >= sfxTracks.Length) { return; } AudioTrack sT = sfxTracks[trackIndex]; if (sT == null || sT.clip == null) { Debug.LogWarning($"AudioManager: SFX track at index {trackIndex} or its clip is null."); return; } if (sfxSource == null) { Debug.LogError("SFX AudioSource is not initialized!"); return; } float fV = sfxMasterVolume * sT.volume; sfxSource.PlayOneShot(sT.clip, fV); Debug.Log($"AudioManager: Playing SFX '{sT.name}' (Volume: {fV})"); if (activeAmbienceSource != null && activeAmbienceSource.isPlaying && currentAmbienceTrack != null && sfxAmbienceDuckAmount < 1.0f && sfxAmbienceDuckDuration > 0f && ambienceFadeCoroutine == null) { if (ambienceDuckCoroutine != null) { StopCoroutine(ambienceDuckCoroutine); } ambienceDuckCoroutine = StartCoroutine(DuckAmbienceCoroutine()); } }
    private IEnumerator DuckAmbienceCoroutine() { if (currentAmbienceTrack == null || activeAmbienceSource == null || !activeAmbienceSource.isPlaying) { ambienceDuckCoroutine = null; yield break; } float originalVolume = currentAmbienceTrack.volume; float duckedVolume = originalVolume * sfxAmbienceDuckAmount; float startVolume = activeAmbienceSource.volume; if (sfxAmbienceDuckDuration <= 0 || sfxAmbienceDuckAmount >= 1.0f) { ambienceDuckCoroutine = null; yield break; } float fadeDownDuration = sfxAmbienceDuckDuration * 0.2f; float holdDuration = sfxAmbienceDuckDuration * 0.6f; float fadeUpDuration = sfxAmbienceDuckDuration * 0.2f; fadeDownDuration = Mathf.Max(fadeDownDuration, 0.01f); fadeUpDuration = Mathf.Max(fadeUpDuration, 0.01f); holdDuration = Mathf.Max(holdDuration, 0f); float elapsedTime = 0f; while (elapsedTime < fadeDownDuration) { if (ambienceFadeCoroutine != null || activeAmbienceSource == null || !activeAmbienceSource.isPlaying || currentAmbienceTrack == null || activeAmbienceSource.clip != currentAmbienceTrack.clip) { if (ambienceFadeCoroutine == null && activeAmbienceSource != null && activeAmbienceSource.isPlaying && currentAmbienceTrack != null) activeAmbienceSource.volume = currentAmbienceTrack.volume; ambienceDuckCoroutine = null; yield break; } elapsedTime += Time.deltaTime; float progress = Mathf.Clamp01(elapsedTime / fadeDownDuration); activeAmbienceSource.volume = Mathf.Lerp(startVolume, duckedVolume, progress); yield return null; } if (activeAmbienceSource != null) activeAmbienceSource.volume = duckedVolume; elapsedTime = 0f; while (elapsedTime < holdDuration) { if (ambienceFadeCoroutine != null || activeAmbienceSource == null || !activeAmbienceSource.isPlaying || currentAmbienceTrack == null || activeAmbienceSource.clip != currentAmbienceTrack.clip) { if (ambienceFadeCoroutine == null && activeAmbienceSource != null && activeAmbienceSource.isPlaying && currentAmbienceTrack != null) activeAmbienceSource.volume = currentAmbienceTrack.volume; ambienceDuckCoroutine = null; yield break; } elapsedTime += Time.deltaTime; yield return null; } elapsedTime = 0f; startVolume = activeAmbienceSource != null ? activeAmbienceSource.volume : duckedVolume; if (currentAmbienceTrack != null) originalVolume = currentAmbienceTrack.volume; else { originalVolume = 0f; } /* Added safety */ while (elapsedTime < fadeUpDuration) { if (ambienceFadeCoroutine != null || activeAmbienceSource == null || !activeAmbienceSource.isPlaying || currentAmbienceTrack == null || activeAmbienceSource.clip != currentAmbienceTrack.clip) { if (ambienceFadeCoroutine == null && activeAmbienceSource != null && activeAmbienceSource.isPlaying && currentAmbienceTrack != null) activeAmbienceSource.volume = currentAmbienceTrack.volume; ambienceDuckCoroutine = null; yield break; } elapsedTime += Time.deltaTime; float progress = Mathf.Clamp01(elapsedTime / fadeUpDuration); if (activeAmbienceSource != null) activeAmbienceSource.volume = Mathf.Lerp(startVolume, originalVolume, progress); yield return null; } if (ambienceFadeCoroutine == null && activeAmbienceSource != null && activeAmbienceSource.isPlaying && currentAmbienceTrack != null && activeAmbienceSource.clip == currentAmbienceTrack.clip) { activeAmbienceSource.volume = originalVolume; } ambienceDuckCoroutine = null; }


    // --- Coroutines ---
    private IEnumerator FadeAudioSource(AudioSource fadeOutSource, AudioSource fadeInSource, float targetVolume, float duration, bool isMusic) { if (duration <= 0) { if (fadeInSource != null) fadeInSource.volume = targetVolume; if (fadeOutSource != null) { fadeOutSource.volume = 0f; fadeOutSource.Stop(); fadeOutSource.clip = null; } if (isMusic && musicFadeCoroutine != null && ReferenceEquals(musicFadeCoroutine, this)) musicFadeCoroutine = null; if (!isMusic && ambienceFadeCoroutine != null && ReferenceEquals(ambienceFadeCoroutine, this)) ambienceFadeCoroutine = null; yield break; } float sVOut = (fadeOutSource != null && fadeOutSource.isPlaying) ? fadeOutSource.volume : 0f; float eT = 0f; if (fadeInSource != null && !fadeInSource.isPlaying) { fadeInSource.Play(); } while (eT < duration) { if (fadeInSource == null && fadeOutSource == null) yield break; eT += Time.deltaTime; float p = Mathf.Clamp01(eT / duration); float sP = Mathf.SmoothStep(0.0f, 1.0f, p); if (fadeInSource != null) fadeInSource.volume = Mathf.Lerp(0f, targetVolume, sP); if (fadeOutSource != null) { if (sVOut > 0) fadeOutSource.volume = Mathf.Lerp(sVOut, 0f, sP); else fadeOutSource.volume = 0f; } yield return null; } if (fadeInSource != null) fadeInSource.volume = targetVolume; if (fadeOutSource != null) { fadeOutSource.volume = 0f; if (fadeOutSource.isPlaying) fadeOutSource.Stop(); fadeOutSource.clip = null; } if (isMusic) { if (musicFadeCoroutine != null && ReferenceEquals(musicFadeCoroutine, this)) { musicFadeCoroutine = null; } } else { if (ambienceFadeCoroutine != null && ReferenceEquals(ambienceFadeCoroutine, this)) { ambienceFadeCoroutine = null; } } }
    private IEnumerator FadeOutSingleSourceCoroutine(AudioSource sourceToFade, float duration, bool isMusic) { if (sourceToFade == null || !sourceToFade.isPlaying) { if (isMusic && musicFadeCoroutine != null && ReferenceEquals(musicFadeCoroutine, this)) musicFadeCoroutine = null; if (!isMusic && ambienceFadeCoroutine != null && ReferenceEquals(ambienceFadeCoroutine, this)) ambienceFadeCoroutine = null; yield break; } float startVolume = sourceToFade.volume; float elapsedTime = 0f; if (duration <= 0) { sourceToFade.volume = 0f; sourceToFade.Stop(); sourceToFade.clip = null; if (isMusic && musicFadeCoroutine != null && ReferenceEquals(musicFadeCoroutine, this)) musicFadeCoroutine = null; if (!isMusic && ambienceFadeCoroutine != null && ReferenceEquals(ambienceFadeCoroutine, this)) ambienceFadeCoroutine = null; yield break; } while (elapsedTime < duration) { if (sourceToFade == null) yield break; elapsedTime += Time.deltaTime; float progress = Mathf.Clamp01(elapsedTime / duration); sourceToFade.volume = Mathf.Lerp(startVolume, 0f, progress); yield return null; } if (sourceToFade != null) { sourceToFade.volume = 0f; sourceToFade.Stop(); sourceToFade.clip = null; } if (isMusic && musicFadeCoroutine != null && ReferenceEquals(musicFadeCoroutine, this)) { musicFadeCoroutine = null; } else if (!isMusic && ambienceFadeCoroutine != null && ReferenceEquals(ambienceFadeCoroutine, this)) { ambienceFadeCoroutine = null; } }

} // End of AudioManager class