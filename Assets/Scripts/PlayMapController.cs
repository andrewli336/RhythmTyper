using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayMapController : MonoBehaviour
{
    public RawImage backgroundRawImage;

    [Header("Score UI")]
    public TMP_Text comboText;
    public TMP_Text scoreText;
    public TMP_Text accuracyText;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TMP_Text rankText, finalScoreText, finalAccuracyText;
    public TMP_Text greatText, goodText, okText, missText;
    public GameObject quitButton;

    private int combo = 0;
    private int maxCombo = 0;
    private int score = 0;
    private int totalHits = 0;
    private float totalAccuracyPoints = 0f;

    private int greats = 0, goods = 0, oks = 0, misses = 0;
    private bool mapEnded = false;

    [Header("References")]
    public AudioSource audioSource;
    public KeyboardOverlay keyboardOverlay;
    public GameObject approachCirclePrefab;

    [Header("Judgment UI")]
    public GameObject judgmentDisplayPrefab;
    public Transform judgmentParent;

    [Header("Settings")]
    public float approachTime = 0.6f;
    public float startDelay = 1f;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioSource sfxSource;

    private Coroutine fadeCoroutine = null;
    private Coroutine bounceCoroutine = null;
    private List<Note> notes = new();
    private Dictionary<Note, NoteApproachCircle> noteToVisualMap = new();
    private GameObject currentJudgmentInstance = null;

    public enum NoteState { Pending, Shown, Hit, Missed }

    [System.Serializable]
    public class Note
    {
        public int lane;
        public float time;
        [System.NonSerialized] public NoteState state = NoteState.Pending;
    }

    [System.Serializable]
    public class MapMetadata
    {
        public string artist, songName, difficultyName, audioFileName, backgroundFileName;
        public float previewTime, bpm, songLength, offsetMs;
    }

    [System.Serializable]
    public class FullMap
    {
        public MapMetadata metadata;
        public Note[] notes;
    }

    private float playbackSpeed = 1f;

    void Start()
    {
        playbackSpeed = PlayerPrefs.GetFloat("SelectedMapSpeed", 1f);
        approachTime = PlayerPrefs.GetFloat("SelectedApproachRate", 0.6f);
        audioSource.pitch = playbackSpeed;

        string mapJsonName = PlayerPrefs.GetString("SelectedMapName", "");
        if (string.IsNullOrEmpty(mapJsonName)) return;

        TextAsset jsonAsset = Resources.Load<TextAsset>("Beatmaps/" + mapJsonName);
        if (jsonAsset == null) return;

        FullMap map = JsonUtility.FromJson<FullMap>(jsonAsset.text);

        Texture2D tex = Resources.Load<Texture2D>("Backgrounds/" + map.metadata.backgroundFileName);
        if (tex != null)
        {
            backgroundRawImage.texture = tex;
            backgroundRawImage.color = new Color(1f, 1f, 1f, 0.3f);
            backgroundRawImage.gameObject.SetActive(true);
        }

        notes.AddRange(map.notes);

        foreach (var note in notes)
        {
            if (keyboardOverlay.TryGetKey(GetLetterForLane(note.lane), out var rt))
            {
                var obj = Instantiate(approachCirclePrefab, rt);
                var visual = obj.GetComponent<NoteApproachCircle>();
                visual.Initialize(note.time, approachTime);
                noteToVisualMap[note] = visual;
            }
        }

        AudioClip clip = Resources.Load<AudioClip>("Songs/" + map.metadata.audioFileName);
        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.time = 0f;
        StartCoroutine(BeginAfterDelay());
        UpdateScoreUI();
    }

    void Update()
    {
        if (!audioSource.isPlaying) return;
        float songTime = audioSource.time;

        foreach (var note in notes)
        {
            if (note.state == NoteState.Pending && note.time - songTime <= approachTime)
                note.state = NoteState.Shown;

            if ((note.state == NoteState.Shown || note.state == NoteState.Hit) && noteToVisualMap.TryGetValue(note, out var visual))
            {
                visual.UpdateState(songTime);
            }
        }

        HandleKeyPresses(songTime);
        CheckForMisses(songTime);

        if (!mapEnded)
        {
            float lastNoteTime = notes.Count > 0 ? notes[^1].time : 0f;
            if (songTime >= lastNoteTime + 1f) EndMap();
        }
    }

    void HandleKeyPresses(float songTime)
    {
        for (int i = 0; i < 26; i++)
            if (Input.GetKeyDown(KeyCode.A + i)) JudgeKey((char)('A' + i), i, songTime);

        if (Input.GetKeyDown(KeyCode.Comma)) JudgeKey(',', 26, songTime);
        if (Input.GetKeyDown(KeyCode.Period)) JudgeKey('.', 27, songTime);
        if (Input.GetKeyDown(KeyCode.Semicolon)) JudgeKey(';', 28, songTime);
        if (Input.GetKeyDown(KeyCode.Slash)) JudgeKey('/', 29, songTime);
    }

    void JudgeKey(char letter, int lane, float songTime)
    {
        foreach (var note in notes)
        {
            if (note.state != NoteState.Shown || note.lane != lane) continue;

            float diff = songTime - note.time;
            if (Mathf.Abs(diff) <= 0.3f)
            {
                string judgment;
                Color color;

                if (Mathf.Abs(diff) <= 0.10f)
                {
                    judgment = "Great!"; color = Color.blue; totalAccuracyPoints += 1f; greats++;
                }
                else if (Mathf.Abs(diff) <= 0.2f)
                {
                    judgment = "Good"; color = Color.green; totalAccuracyPoints += 0.3333f; goods++;
                }
                else
                {
                    judgment = "OK"; color = Color.yellow; totalAccuracyPoints += 0.1667f; oks++;
                }

                if (noteToVisualMap.TryGetValue(note, out var visual))
                    visual.ForceFadeOut();

                combo++;
                maxCombo = Mathf.Max(combo, maxCombo);
                score += Mathf.RoundToInt(300f * (1f + combo / 10f));
                totalHits++;
                UpdateScoreUI();
                ShowJudgment(judgment, color);
                if (hitSound != null && sfxSource != null)
                    sfxSource.PlayOneShot(hitSound);
                note.state = NoteState.Hit;
                return;
            }
        }
    }

    void CheckForMisses(float songTime)
    {
        foreach (var note in notes)
        {
            if (note.state == NoteState.Shown && songTime - note.time > 0.15f)
            {
                ShowJudgment("Miss", Color.red);
                if (noteToVisualMap.TryGetValue(note, out var visual))
                    visual.ForceFadeOut();
                if (missSound != null && sfxSource != null)
                    sfxSource.PlayOneShot(missSound);
                note.state = NoteState.Missed;
                misses++;
                combo = 0;
                totalHits++;
                UpdateScoreUI();
            }
        }
    }

    void EndMap()
    {
        mapEnded = true;
        float acc = totalHits > 0 ? (totalAccuracyPoints / totalHits) * 100f : 100f;
        string rank = acc == 100f ? "SS" : acc >= 90f && misses == 0 ? "S" : acc >= 90f ? "A" : acc >= 80f ? "B" : acc >= 70f ? "C" : acc >= 60f ? "D" : "F";
        ShowResultScreen(rank, acc);
        if (quitButton != null) quitButton.SetActive(false);
    }

    void ShowResultScreen(string rank, float acc)
    {
        resultPanel.SetActive(true);
        rankText.text = $"Rank: {rank}";
        finalScoreText.text = $"Score: {score}";
        finalAccuracyText.text = $"Accuracy: {acc:F2}%";
        greatText.text = $"Great: {greats}";
        goodText.text = $"Good: {goods}";
        okText.text = $"OK: {oks}";
        missText.text = $"Miss: {misses}";
    }

    void UpdateScoreUI()
    {
        comboText.text = $"Combo: {combo}";
        scoreText.text = $"Score: {score}";
        float accuracy = totalHits > 0 ? (totalAccuracyPoints / totalHits) * 100f : 100f;
        accuracyText.text = $"Accuracy: {accuracy:F2}%";
    }

    void ShowJudgment(string text, Color color)
    {
        if (judgmentDisplayPrefab == null || judgmentParent == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
        if (currentJudgmentInstance != null) Destroy(currentJudgmentInstance);

        GameObject display = Instantiate(judgmentDisplayPrefab, judgmentParent);
        currentJudgmentInstance = display;

        var rect = display.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        bounceCoroutine = StartCoroutine(BounceEffect(rect));

        var tmp = display.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            if (tmp.fontMaterial.HasProperty("_FaceColor"))
                tmp.fontMaterial.SetColor("_FaceColor", color);
            else
                tmp.color = color;
            fadeCoroutine = StartCoroutine(FadeOutAndDestroy(tmp, 1f));
        }
    }

    IEnumerator BounceEffect(RectTransform rect)
    {
        float duration = 0.15f;
        float scale = 1.3f;
        float time = 0f;
        Vector3 start = Vector3.one * scale;
        Vector3 end = Vector3.one;
        rect.localScale = start;

        while (time < duration)
        {
            rect.localScale = Vector3.Lerp(start, end, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        rect.localScale = end;
    }

    IEnumerator FadeOutAndDestroy(TMP_Text tmp, float delay)
    {
        yield return new WaitForSeconds(delay);
        float fadeDuration = 0.3f;
        float t = 0f;
        Color startColor = tmp.color;
        Color targetColor = startColor; targetColor.a = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            Color lerped = Color.Lerp(startColor, targetColor, t / fadeDuration);
            if (tmp.fontMaterial.HasProperty("_FaceColor"))
                tmp.fontMaterial.SetColor("_FaceColor", lerped);
            else
                tmp.color = lerped;
            yield return null;
        }

        Destroy(tmp.transform.parent.gameObject);
    }

    IEnumerator BeginAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        audioSource.Play();
    }

    public void OnQuitButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("BeatmapSelectScene");
    }

    public void OnRetryButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayMapScene");
    }

    char GetLetterForLane(int lane)
    {
        return lane switch
        {
            < 26 => (char)('A' + lane),
            26 => ',',
            27 => '.',
            28 => ';',
            29 => '/',
            _ => '?'
        };
    }
}