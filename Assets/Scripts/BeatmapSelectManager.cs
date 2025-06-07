using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BeatmapSelectManager : MonoBehaviour
{
    public Transform scrollContent;
    public GameObject beatmapEntryPrefab;
    public ScrollRect scrollRect;
    public AudioSource audioSource;

    [Header("Map Info UI")]
    public Image mapInfoBackground;
    public TMP_Text mapInfoName;
    public TMP_Text mapInfoArtist;
    public TMP_Text mapInfoMapper;
    public TMP_Text mapInfoTime;
    public TMP_Text mapInfoBPM;
    public TMP_Text mapInfoStars;

    [Header("Mods Panel")]
    public Slider mapSpeedSlider;
    public TMP_Text mapSpeedLabel;
    public Slider approachRateSlider;
    public TMP_Text approachRateLabel;

    private List<FullMap> loadedBeatmaps = new();
    private List<AudioClip> loadedClips = new();
    private List<BeatmapEntryUI> entryUIs = new();
    private int selectedIndex = -1;
    private TextAsset[] jsonFiles;

    [System.Serializable]
    public class MapMetadata
    {
        public string artist;
        public string songName;
        public string difficultyName;
        public string audioFileName;
        public string backgroundFileName;
        public float previewTime;
        public float bpm;
        public float songLength;
        public float difficultyStars;
    }

    [System.Serializable]
    public class FullMap
    {
        public MapMetadata metadata;
        public NoteEditor.NoteData[] notes;
    }

    void Start()
    {
        LoadAllBeatmaps();

        mapSpeedSlider.onValueChanged.AddListener(UpdateMapSpeedLabel);
        approachRateSlider.onValueChanged.AddListener(UpdateApproachRateLabel);

        UpdateMapSpeedLabel(mapSpeedSlider.value);
        UpdateApproachRateLabel(approachRateSlider.value);
    }

    void LoadAllBeatmaps()
    {
        jsonFiles = Resources.LoadAll<TextAsset>("Beatmaps");

        List<(TextAsset json, FullMap map)> sortedMaps = new();

        foreach (TextAsset json in jsonFiles)
        {
            FullMap fullMap = JsonUtility.FromJson<FullMap>(json.text);
            sortedMaps.Add((json, fullMap));
        }

        sortedMaps.Sort((a, b) => a.map.metadata.difficultyStars.CompareTo(b.map.metadata.difficultyStars));

        loadedBeatmaps.Clear();
        loadedClips.Clear();
        entryUIs.Clear();

        for (int index = 0; index < sortedMaps.Count; index++)
        {
            var (json, fullMap) = sortedMaps[index];

            loadedBeatmaps.Add(fullMap);
            jsonFiles[index] = json;

            GameObject entryObj = Instantiate(beatmapEntryPrefab, scrollContent);
            BeatmapEntryUI entryUI = entryObj.GetComponent<BeatmapEntryUI>();
            entryUI.Setup(this, index, fullMap.metadata.songName, fullMap.metadata.artist, fullMap.metadata.difficultyStars);

            if (!string.IsNullOrWhiteSpace(fullMap.metadata.backgroundFileName))
            {
                Texture2D tex = Resources.Load<Texture2D>("Backgrounds/" + fullMap.metadata.backgroundFileName);
                if (tex != null)
                {
                    Sprite bgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    entryUI.background.sprite = bgSprite;
                }
            }

            AudioClip clip = Resources.Load<AudioClip>("Songs/" + fullMap.metadata.audioFileName);
            loadedClips.Add(clip);

            entryUIs.Add(entryUI);
        }
    }

    public void OnBeatmapSelected(int index)
    {
        if (selectedIndex == index)
        {
            string mapJsonName = jsonFiles[index].name;
            PlayerPrefs.SetString("SelectedMapName", mapJsonName);
            PlayerPrefs.SetFloat("SelectedMapSpeed", mapSpeedSlider.value);
            PlayerPrefs.SetFloat("SelectedApproachRate", approachRateSlider.value);
            PlayerPrefs.Save();
            SceneManager.LoadScene("PlayMapScene");
            return;
        }

        selectedIndex = index;
        ScrollTo(index);
        HighlightSelected(index);

        AudioClip clip = loadedClips[index];
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.time = loadedBeatmaps[index].metadata.previewTime;
            audioSource.Play();
        }

        UpdateMapInfo(loadedBeatmaps[index].metadata);
    }

    void ScrollTo(int index)
    {
        float entryHeight = ((RectTransform)entryUIs[index].transform).rect.height;
        float spacing = 20f;
        float totalHeight = (entryHeight + spacing) * entryUIs.Count;
        float viewportHeight = scrollRect.viewport.rect.height;
        float targetY = index * (entryHeight + spacing) + entryHeight / 2f;
        float scrollY = targetY - viewportHeight / 2f;
        float normalized = Mathf.Clamp01(scrollY / Mathf.Max(totalHeight - viewportHeight, 1));
        scrollRect.verticalNormalizedPosition = 1f - normalized;
    }

    void HighlightSelected(int index)
    {
        foreach (var ui in entryUIs)
            ui.highlight.enabled = false;
        entryUIs[index].highlight.enabled = true;
    }

    void UpdateMapInfo(MapMetadata meta)
    {
        mapInfoName.text = meta.songName;
        mapInfoArtist.text = meta.artist;
        mapInfoMapper.text = "Mapped by Andrew";
        mapInfoTime.text = FormatTime(meta.songLength);
        mapInfoBPM.text = "BPM " + Mathf.RoundToInt(meta.bpm).ToString();
        mapInfoStars.text = meta.difficultyStars.ToString("0.0") + " stars";

        Texture2D tex = Resources.Load<Texture2D>("Backgrounds/" + meta.backgroundFileName);
        if (tex != null)
        {
            Sprite bgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            mapInfoBackground.sprite = bgSprite;
        }
    }

    void UpdateMapSpeedLabel(float value)
    {
        if (mapSpeedLabel != null)
            mapSpeedLabel.text = "Speed: " + value.ToString("0.0") + "x";
    }

    void UpdateApproachRateLabel(float value)
    {
        if (approachRateLabel != null)
            approachRateLabel.text = "Approach: " + value.ToString("0.0") + "s";
    }

    string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return mins.ToString("0") + ":" + secs.ToString("00");
    }
}