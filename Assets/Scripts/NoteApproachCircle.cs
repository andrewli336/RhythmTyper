using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NoteApproachCircle : MonoBehaviour
{
    public Image circleImage;
    public float targetTime;
    public float approachTime;
    public float stayTime = 0.2f;
    public float fadeOutDuration = 0.3f;

    private bool fading = false;

    public void Initialize(float targetTime, float approachTime)
    {
        this.targetTime = targetTime;
        this.approachTime = approachTime;
        fading = false;

        // Don't show the circle yet
        SetVisible(false);
    }

    public void UpdateState(float currentTime)
    {
        if (fading) return;

        float timeUntilHit = targetTime - currentTime;
        float timeSinceHit = currentTime - targetTime;

        if (timeUntilHit >= 0f && timeUntilHit <= approachTime)
        {
            float t = 1f - (timeUntilHit / approachTime);
            float scale = Mathf.Lerp(2f, 1f, t);
            float alpha = Mathf.Clamp01(t / 0.2f);
            ApplyVisuals(scale, alpha);
        }
        else if (timeSinceHit >= 0f && timeSinceHit <= stayTime)
        {
            ApplyVisuals(1f, 1f);
        }
        else if (timeSinceHit > stayTime)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            // Not yet in range — hide
            SetVisible(false);
        }
    }

    public void ForceFadeOut()
    {
        if (!fading)
            StartCoroutine(FadeOut());
    }

    private void ApplyVisuals(float scale, float alpha)
    {
        transform.localScale = Vector3.one * scale;
        SetVisible(true);
        if (circleImage != null)
        {
            var col = circleImage.color;
            col.a = alpha;
            circleImage.color = col;
        }
    }

    private void SetVisible(bool visible)
    {
        if (circleImage != null)
        {
            var col = circleImage.color;
            col.a = visible ? col.a : 0f;
            circleImage.color = col;
        }
    }

    private IEnumerator FadeOut()
    {
        fading = true;
        float t = 0f;
        Color start = circleImage.color;
        Color end = new Color(start.r, start.g, start.b, 0f);

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            circleImage.color = Color.Lerp(start, end, t / fadeOutDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}