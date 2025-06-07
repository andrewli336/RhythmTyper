using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class ScoreListLoader : MonoBehaviour
{
    public GameObject scoreRowPrefab;
    public Transform contentParent;

    [System.Serializable]
    public class ScoreResult
    {
        public string rank;
        public int score;
        public float accuracy;
        public int maxCombo;
        public int greats;
        public int goods;
        public int oks;
        public int misses;
        public string date;
    }

    public void LoadScoresFor(string folderPath)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (string.IsNullOrEmpty(folderPath)) return;

        string scoresFolder = Path.Combine(folderPath, "Scores");
        if (!Directory.Exists(scoresFolder)) return;

        string[] files = Directory.GetFiles(scoresFolder, "score_*.json");
        List<ScoreResult> results = new();

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                ScoreResult result = JsonUtility.FromJson<ScoreResult>(json);
                results.Add(result);
            }
            catch
            {
                Debug.LogWarning($"⚠️ Failed to parse score file: {file}");
            }
        }

        results.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (ScoreResult result in results)
        {
            GameObject row = Instantiate(scoreRowPrefab, contentParent);

            TMP_Text rankText = row.transform.Find("RankText")?.GetComponent<TMP_Text>();
            TMP_Text scoreText = row.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            TMP_Text accuracyText = row.transform.Find("AccuracyText")?.GetComponent<TMP_Text>();
            TMP_Text dateText = row.transform.Find("DateText")?.GetComponent<TMP_Text>();

            if (rankText != null)
            {
                rankText.text = result.rank;

                switch (result.rank)
                {
                    case "SS": rankText.color = new Color32(255, 153, 255, 255); break; // Pink
                    case "S":  rankText.color = new Color32(255, 255, 153, 255); break; // Yellow
                    case "A":  rankText.color = new Color32(102, 255, 255, 255); break; // Cyan
                    case "B":  rankText.color = new Color32(102, 204, 255, 255); break; // Blue
                    case "C":  rankText.color = new Color32(204, 204, 204, 255); break; // Gray
                    case "D":  rankText.color = new Color32(255, 153, 153, 255); break; // Red/pink
                    case "F":  rankText.color = new Color32(160, 160, 160, 255); break; // Dark gray
                    default:   rankText.color = Color.white; break;
                }
            }

            if (scoreText != null) scoreText.text = result.score.ToString();
            if (accuracyText != null) accuracyText.text = $"{result.accuracy:F2}%";
            if (dateText != null) dateText.text = result.date;
        }
    }
}