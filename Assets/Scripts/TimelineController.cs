using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TimelineController : MonoBehaviour
{
    [Header("Timeline")]
    public RectTransform tickContainer;
    public GameObject tickPrefab;
    public RectTransform scrollContainer;
    public float fixedTimelineWidth = 6400;

    [Header("Bottom Timeline")]
    public RectTransform globalTimelineArea;
    public RectTransform movingPlayhead;

    [Header("Audio + Playback")]
    public AudioSource audioSource;
    public float bpm = 120f;
    public float songLengthInSeconds = 60f;
    public float offsetMs = 0f;
    public SetupImporter setupImporter;

    [Header("UI")]
    public TMP_Text timeText;
    public TMP_InputField bpmInput;
    public TMP_InputField offsetInput;
    public TMP_InputField speedInput;
    public float pixelsPerSecond = 100f;

    [Header("Metronome")]
    public AudioClip metronomeClip;
    public AudioSource metronomeSource;
    public bool metronomeEnabled = true;

    private Coroutine metronomeRoutine;
    private bool isPlaying = false;
    private bool songEnded = false;
    private float songPosition = 0f;
    private float songStartTime = 0f;
    private float playbackSpeed = 1f;
    private int metronomeLastPlayedIndex = -1;

    void Start()
    {
        ApplyBPMFromInput();
        ApplyOffsetFromInput();
        ApplySpeedFromInput();

        songPosition = 0f;
        bpmInput.text = bpm.ToString("F0");
        offsetInput.text = offsetMs.ToString("F0");
        speedInput.text = playbackSpeed.ToString("0.0");

        GenerateTickMarks();
    }

    void Update()
    {
        HandlePlayPauseInput();
        HandleGlobalScrubbing();

        if (isPlaying && !songEnded)
        {
            songPosition = (Time.time - songStartTime) * playbackSpeed;

            if (songPosition >= songLengthInSeconds)
            {
                songPosition = songLengthInSeconds;
                HandleSongEnd();
            }
        }

        UpdateTimeText(songPosition);
        UpdateBottomPlayhead(songPosition);
        if (!songEnded)
            ScrollTopTimeline(songPosition);
    }

    void HandlePlayPauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPlaying = !isPlaying;

            if (isPlaying)
            {
                if (songEnded || songPosition >= songLengthInSeconds - 0.01f)
                {
                    songPosition = 0f;
                    songEnded = false;
                }

                ApplySpeedFromInput();
                audioSource.pitch = playbackSpeed;
                audioSource.time = songPosition;
                audioSource.Play();

                songStartTime = Time.time - songPosition / playbackSpeed;
                metronomeLastPlayedIndex = -1;

                if (metronomeEnabled && metronomeRoutine == null)
                    metronomeRoutine = StartCoroutine(MetronomeTickLoop());
            }
            else
            {
                audioSource.Pause();
                StopMetronome();
            }
        }
    }

    void HandleGlobalScrubbing()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject()) return;

            PointerEventData pointerData = new(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject == globalTimelineArea.gameObject)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(globalTimelineArea, Input.mousePosition, null, out Vector2 localPoint);
                    float width = globalTimelineArea.rect.width;
                    float normalizedX = Mathf.Clamp01((localPoint.x + width / 2f) / width);
                    float newTime = normalizedX * songLengthInSeconds;

                    songPosition = newTime;
                    audioSource.time = newTime;
                    songStartTime = Time.time - newTime / playbackSpeed;

                    RestartMetronomeIfNeeded();
                    break;
                }
            }
        }
    }

    void HandleSongEnd()
    {
        songEnded = true;
        isPlaying = false;
        audioSource.Pause();
        StopMetronome();
    }

    void UpdateBottomPlayhead(float time)
    {
        float normalizedTime = time / songLengthInSeconds;
        float width = globalTimelineArea.rect.width;
        movingPlayhead.anchoredPosition = new Vector2(normalizedTime * width, 0);
    }

    void UpdateTimeText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
        timeText.text = $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }

    void ScrollTopTimeline(float time)
    {
        float normalizedTime = time / songLengthInSeconds;
        float scrollX = normalizedTime * fixedTimelineWidth;
        scrollContainer.anchoredPosition = new Vector2(-scrollX, 0);
    }

    void GenerateTickMarks()
    {
        foreach (Transform child in tickContainer)
            Destroy(child.gameObject);

        float secondsPerBeat = 60f / bpm;
        int beatCount = Mathf.CeilToInt(songLengthInSeconds / secondsPerBeat);

        for (int i = 0; i < beatCount; i++)
        {
            float beatTime = i * secondsPerBeat;
            float normalizedTime = beatTime / songLengthInSeconds;
            float posX = normalizedTime * fixedTimelineWidth;

            GameObject tick = Instantiate(tickPrefab, tickContainer);
            RectTransform rt = tick.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, 0);
        }
    }

    public void ChangeBPM(float delta)
    {
        bpm = Mathf.Max(1f, bpm + delta);
        bpmInput.text = bpm.ToString("F0");
        GenerateTickMarks();
        RestartMetronomeIfNeeded();
    }

    public void ChangeOffset(float delta)
    {
        offsetMs += delta;
        offsetInput.text = offsetMs.ToString("F0");
        GenerateTickMarks();
    }

    public void ApplyBPMFromInput()
    {
        if (float.TryParse(bpmInput.text, out float parsed))
        {
            bpm = Mathf.Max(1f, parsed);
            GenerateTickMarks();
        }
    }

    public void ApplyOffsetFromInput()
    {
        if (float.TryParse(offsetInput.text, out float parsed))
        {
            offsetMs = parsed;
            GenerateTickMarks();
        }
    }

    public void ApplySpeedFromInput()
    {
        if (speedInput != null && float.TryParse(speedInput.text, out float parsed))
        {
            playbackSpeed = Mathf.Clamp(parsed, 0.1f, 5f);
            audioSource.pitch = playbackSpeed;
        }
    }

    void RestartMetronomeIfNeeded()
    {
        StopMetronome();
        if (metronomeEnabled && isPlaying)
        {
            metronomeLastPlayedIndex = -1;
            metronomeRoutine = StartCoroutine(MetronomeTickLoop());
        }
    }

    void StopMetronome()
    {
        if (metronomeRoutine != null)
        {
            StopCoroutine(metronomeRoutine);
            metronomeRoutine = null;
        }
    }

    IEnumerator MetronomeTickLoop()
    {
        while (metronomeEnabled)
        {
            float songTime = audioSource.time + offsetMs / 1000f;

            float secondsPerBeat = 60f / bpm;
            int currentIndex = Mathf.FloorToInt(songTime / secondsPerBeat);

            if (currentIndex > metronomeLastPlayedIndex && songTime >= 0f)
            {
                metronomeLastPlayedIndex = currentIndex;
                metronomeSource.PlayOneShot(metronomeClip);
            }

            yield return null;
        }
    }

    public void ApplySetupChanges()
    {
        if (audioSource.clip != null)
        {
            songLengthInSeconds = audioSource.clip.length;
            songPosition = 0f;
            audioSource.time = 0f;
            songStartTime = Time.time;

            fixedTimelineWidth = songLengthInSeconds * pixelsPerSecond;
            GenerateTickMarks();
        }
    }

    public float GetOffsetSeconds() => offsetMs / 1000f;
    public float GetBPM() => bpm;
    public float GetOffsetMs() => offsetMs;
    public float GetSongPosition() => songPosition;
    public bool IsPlaying() => isPlaying;

    public float GetRawAudioTime()
    {
        return audioSource != null ? audioSource.time : 0f;
    }

    public void StopImmediately()
    {
        isPlaying = false;
        songEnded = false;
        songPosition = 0f;
        audioSource.Stop();
        StopMetronome();
        UpdateBottomPlayhead(0f);
        ScrollTopTimeline(0f);
        UpdateTimeText(0f);
    }

    public void PlayFromOffset()
    {
        songEnded = false;
        isPlaying = true;

        ApplySpeedFromInput();
        float offsetSec = GetAdjustedOffsetSeconds();
        songPosition = offsetSec;
        audioSource.time = offsetSec;
        audioSource.pitch = playbackSpeed;
        audioSource.Play();
        songStartTime = Time.time - offsetSec;

        metronomeLastPlayedIndex = -1;
        if (metronomeEnabled)
            metronomeRoutine = StartCoroutine(MetronomeTickLoop());
    }

    public void SetSongPosition(float newTime)
    {
        songPosition = newTime;
        audioSource.time = newTime;
        songStartTime = Time.time - newTime / playbackSpeed;

        UpdateBottomPlayhead(newTime);
        ScrollTopTimeline(newTime);
        UpdateTimeText(newTime);
    }

    public void ScrubTo(float newTime)
    {
        songPosition = newTime;
        audioSource.time = newTime;
        audioSource.Stop();
        songStartTime = Time.time - newTime / playbackSpeed;

        UpdateBottomPlayhead(newTime);
        ScrollTopTimeline(newTime);
        UpdateTimeText(newTime);
        StopMetronome();
    }

    public float GetAdjustedOffsetSeconds()
    {
        return (offsetMs / 1000f) / playbackSpeed;
    }
}