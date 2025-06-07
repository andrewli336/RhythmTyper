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
        keyToLaneMap[KeyCode.Slash] = 29;
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

    double CalculateDifficultyStars(List<NoteData> notes)
    {
        const bool useFingerMoveStrain = true;
        const double decayRate = 0.125;
        const double difficultyMultiplier = 0.018;
        int[] laneToFinger = new int[]{0, 3, 2, 2, 2, 3, 3, 4, 5, 4, 5, 6, 4, 4, 6, 7, 0, 3, 1, 3, 4, 3, 1, 1, 4, 0, 5, 6, 7, 7};
        float[] laneToRow = new float[] { 1, 0.5f, 0, 1, 2, 1, 1.5f, 1.5f, 2, 1, 1, 1, 0, 0.5f, 2, 2, 2, 2, 1, 2.5f, 2, 0, 2, 0, 2.5f, 0, 0, 0, 1, 2.5f};

        notes.Sort((a, b) => a.time.CompareTo(b.time));
        double[] strainAtNote = new double[notes.Count];
        double currentOverallStrain = 0;
        double[] currentIndividualStrain = new double[8];
        float[] previousRow = new float[8];
        int previousFinger = 0;

        for (int i = 1; i < notes.Count; i++)
        {
            int currentFinger = laneToFinger[notes[i].lane];
            float currentRow = laneToRow[notes[i].lane];
            double deltaTime = (notes[i].time - notes[i - 1].time);

            currentOverallStrain *= System.Math.Pow(decayRate, deltaTime);
            currentIndividualStrain[currentFinger] *= System.Math.Pow(decayRate, deltaTime);

            currentOverallStrain += 1;
            currentIndividualStrain[currentFinger] += 2;

            if (useFingerMoveStrain && currentRow != previousRow[currentFinger])
            {
                currentIndividualStrain[currentFinger] += 0.5;
                if ((int)currentRow != (int)previousRow[previousFinger])
                    currentOverallStrain += 0.5;
            }

            previousRow[currentFinger] = currentRow;
            previousFinger = currentFinger;
            strainAtNote[i] = currentOverallStrain + currentIndividualStrain[currentFinger];
        }

        System.Array.Sort(strainAtNote, (a, b) => b.CompareTo(a));

        double sr = 0;
        double weight = 1;
        double weightDecay = 0.9;

        foreach (double strain in strainAtNote)
        {
            sr += strain * weight;
            weight *= weightDecay;
        }

        return sr * difficultyMultiplier;
    }

    public void Publish()
    {
        if (setupImporter == null || timeline == null)
            return;

        var metadata = setupImporter.GetMetadata();
        metadata.bpm = timeline.GetBPM();
        metadata.offsetMs = timeline.GetOffsetMs();
        metadata.difficultyStars = (float)CalculateDifficultyStars(placedNotes);

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
        SceneManager.LoadScene("TitleScene");
    }

    public void RestartAndCountdown()
    {
        if (timeline == null) return;

        placedNotes.Clear();
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