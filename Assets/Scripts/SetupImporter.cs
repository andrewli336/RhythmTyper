using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetupImporter : MonoBehaviour
{
    [Header("Scene References")]
    public AudioSource audioSource;

    [Header("Input Fields")]
    public TMP_InputField artistInput;
    public TMP_InputField songNameInput;
    public TMP_InputField difficultyNameInput;
    public TMP_InputField audioNameInput;
    public TMP_InputField backgroundNameInput;
    public TMP_InputField previewTimeInput;

    private RawImage backgroundRaw;
    private TimelineController timeline;

    void Start()
    {
        backgroundRaw = GameObject.Find("BackgroundRaw").GetComponent<RawImage>();
        timeline = FindObjectOfType<TimelineController>();
    }

    public void TryLoadResources()
    {
        string audioName = audioNameInput.text.Trim();
        if (!string.IsNullOrEmpty(audioName))
        {
            AudioClip clip = Resources.Load<AudioClip>("Songs/" + audioName);
            if (clip != null)
            {
                audioSource.clip = clip;
            }
        }

        string bgName = backgroundNameInput.text.Trim();
        if (!string.IsNullOrEmpty(bgName))
        {
            Texture2D tex = Resources.Load<Texture2D>("Backgrounds/" + bgName);
            if (tex != null)
            {
                backgroundRaw.texture = tex;
            }
        }
    }

    public MapMetadata GetMetadata()
    {
        float.TryParse(previewTimeInput.text, out float previewTime);

        float bpm = timeline != null ? timeline.GetBPM() : 120f;
        float songLength = audioSource.clip != null ? audioSource.clip.length : 0f;

        return new MapMetadata
        {
            artist = artistInput.text,
            songName = songNameInput.text,
            difficultyName = difficultyNameInput.text,
            audioFileName = audioNameInput.text,
            backgroundFileName = backgroundNameInput.text,
            previewTime = previewTime,
            bpm = bpm,
            songLength = songLength
        };
    }

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
        public float offsetMs;
        public float difficultyStars;
    }

    public void SetAudioFromClip(AudioClip clip)
    {
        audioSource.clip = clip;
    }

    public void SetBackgroundFromTexture(Texture2D texture)
    {
        backgroundRaw.texture = texture;
    }

    public void ApplySettings()
    {
        TryLoadResources();
        FindObjectOfType<TimelineController>()?.ApplySetupChanges();
    }
}