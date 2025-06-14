using UnityEngine;

public class KeyboardInputVisualizer : MonoBehaviour
{
    public KeyboardOverlay keyboardOverlay;

    void Update()
    {
        for (int i = 0; i < 26; i++)
        {
            KeyCode key = KeyCode.A + i;
            if (Input.GetKeyDown(key))
            {
                char letter = (char)('A' + i);
                FlashKey(letter);
            }
        }

        if (Input.GetKeyDown(KeyCode.Comma)) FlashKey(',');
        if (Input.GetKeyDown(KeyCode.Period)) FlashKey('.');
        if (Input.GetKeyDown(KeyCode.Semicolon)) FlashKey(';');
        if (Input.GetKeyDown(KeyCode.LeftBracket)) FlashKey('[');
    }

    void FlashKey(char letter)
    {
        if (keyboardOverlay.TryGetKey(letter, out var rt))
        {
            var circle = rt.GetComponent<KeyHitCircle>();
            if (circle != null)
                circle.Flash();
        }
    }
}