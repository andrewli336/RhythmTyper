using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class KeyboardOverlay : MonoBehaviour
{
    public GameObject keyPrefab;
    public RectTransform overlayParent;

    private Dictionary<char, RectTransform> keyPositions = new();
    private Dictionary<char, KeyHitCircle> keyScripts = new();

    void Start()
    {
        string[] rows = {
            "QWERTYUIOP",
            "ASDFGHJKL;",
            "ZXCVBNM,./"
        };

        float rowSpacing = 220f;
        float keySpacing = 170f;
        float startY = 100f;

        float middleRowOffset = keySpacing / 4f;
        float bottomRowOffset = keySpacing / 1.4f;

        float globalXOffset = -80f;

        for (int row = 0; row < rows.Length; row++)
        {
            string letters = rows[row];
            float customOffset = 0f;

            if (row == 1) customOffset = middleRowOffset;
            if (row == 2) customOffset = bottomRowOffset;

            float startX = -((letters.Length - 1) * keySpacing) / 2f + customOffset + globalXOffset;

            for (int i = 0; i < letters.Length; i++)
            {
                char letter = letters[i];
                GameObject key = Instantiate(keyPrefab, overlayParent);
                RectTransform rt = key.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(startX + i * keySpacing, startY - row * rowSpacing);

                key.GetComponentInChildren<TMP_Text>().text = letter.ToString();
                key.name = "Key_" + letter;
                keyPositions[letter] = rt;
                keyScripts[letter] = key.GetComponent<KeyHitCircle>();
            }
        }
    }

    public bool TryGetKey(char c, out RectTransform keyTransform)
    {
        return keyPositions.TryGetValue(c, out keyTransform);
    }

    public bool TryGetHitCircle(char c, out KeyHitCircle script)
    {
        return keyScripts.TryGetValue(c, out script);
    }
}