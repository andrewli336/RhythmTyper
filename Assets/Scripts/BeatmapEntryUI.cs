using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BeatmapEntryUI : MonoBehaviour
{
    public TMP_Text songName;
    public TMP_Text songArtist;
    public TMP_Text difficultyStarsText;
    public Image background;
    public Image highlight;
    public Button button;

    [HideInInspector] public BeatmapSelectManager manager;
    [HideInInspector] public int index;

    public void Setup(BeatmapSelectManager mgr, int idx, string name, string artist, float stars)
    {
        manager = mgr;
        index = idx;
        songName.text = name;
        songArtist.text = artist;
        difficultyStarsText.text = stars.ToString("0.0") + " stars";
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        manager.OnBeatmapSelected(index);
    }
}