using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

public class NoteEditor : MonoBehaviour
{
    [Header("Dependencies")]
    public TimelineController timeline;
    public SetupImporter setupImporter;
    public TabManager tabManager;

    private List<NoteData> placedNotes = new();
    private Dictionary<KeyCode, int> keyToLaneMap = new();

    void Start()
    {
        for (int i = 0; i < 26; i++)
            keyToLaneMap[(KeyCode)((int)KeyCode.A + i)] = i;

        keyToLaneMap[KeyCode.Comma] = 26;
        keyToLaneMap[KeyCode.Period] = 27;
        keyToLaneMap[KeyCode.Semicolon] = 28;
        keyToLaneMap[KeyCode.LeftBracket] = 29;
    }

    void Update()
    {
        if (timeline == null || !timeline.IsPlaying()) return;
        if (tabManager == null || !tabManager.IsObjectsTabActive()) return;

        foreach (var pair in keyToLaneMap)
        {
            if (Input.GetKeyDown(pair.Key))
            {
                AddNote(pair.Value);
            }
        }
    }

    void AddNote(int lane)
    {
        float rawTime = timeline.GetRawAudioTime();
        float snappedTime = SnapToBeat(rawTime);

        placedNotes.Add(new NoteData { lane = lane, time = snappedTime });
    }

    float SnapToBeat(float time)
    {
        float beatDuration = 60f / timeline.GetBPM();
        float sixteenth = beatDuration / 4f;
        int nearest = Mathf.RoundToInt(time / sixteenth);
        return nearest * sixteenth;
    }

    float CalculateDifficultyStars()
    {
        if (placedNotes.Count == 0 || timeline == null) return 0f;

        float songLength = timeline.songLengthInSeconds;
        HashSet<float> uniqueTimestamps = new();

        foreach (var note in placedNotes)
        {
            float rounded = Mathf.Round(note.time * 100f) / 100f;
            uniqueTimestamps.Add(rounded);
        }

        float notePerSecond = uniqueTimestamps.Count / songLength;
        return Mathf.Clamp(notePerSecond, 0f, 10f);
    }

    public void Publish()
    {
        if (setupImporter == null || timeline == null)
        {
            Debug.LogWarning("⚠️ Cannot publish: missing setupImporter or timeline.");
            return;
        }

        var metadata = setupImporter.GetMetadata();
        metadata.bpm = timeline.GetBPM();
        metadata.offsetMs = timeline.GetOffsetMs();
        metadata.difficultyStars = CalculateDifficultyStars();

        FullMap map = new FullMap
        {
            metadata = metadata,
            notes = placedNotes.ToArray()
        };

        string pathInResources = Path.Combine(Application.dataPath, "Resources/Beatmaps");
        Directory.CreateDirectory(pathInResources);

        string mapName = metadata.songName.Replace(" ", "_") + "_" + metadata.difficultyName.Replace(" ", "_");
        string jsonPath = Path.Combine(pathInResources, mapName + ".json");

        File.WriteAllText(jsonPath, JsonUtility.ToJson(map, true));

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        SceneManager.LoadScene("TitleScene");
    }

    public void DiscardAndReturnToTitle()
    {
        Debug.Log("↩️ Discarded map and returning to TitleScene.");
        SceneManager.LoadScene("TitleScene");
    }

    public void RestartAndCountdown()
    {
        if (timeline == null)
        {
            Debug.LogWarning("❌ Cannot restart: missing timeline.");
            return;
        }

        placedNotes.Clear();
        Debug.Log("🧹 Notes cleared!");

        StartCoroutine(RestartWithMetronomeIntro());
    }

    IEnumerator RestartWithMetronomeIntro()
    {
        timeline.StopImmediately();

        timeline.ScrubTo(0f);

        yield return new WaitForEndOfFrame();

        timeline.PlayFromOffset();
    }

    [System.Serializable]
    public class NoteData
    {
        public int lane;
        public float time;
    }

    [System.Serializable]
    public class FullMap
    {
        public SetupImporter.MapMetadata metadata;
        public NoteData[] notes;
    }
}