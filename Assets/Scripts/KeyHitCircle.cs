using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KeyHitCircle : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject approachCirclePrefab;
    public Transform approachCircleContainer;
    public Image keyGlowOverlay;

    [Header("Settings")]
    public float flashDuration = 0.05f;

    private Coroutine glowRoutine;

    public void ShowApproach(float approachTime)
    {
        if (approachCirclePrefab == null || approachCircleContainer == null) return;

        GameObject obj = Instantiate(approachCirclePrefab, approachCircleContainer);
        Image circle = obj.GetComponent<Image>();
        if (circle == null) return;

        obj.transform.localScale = Vector3.one * 2f;
        circle.color = new Color(1f, 1f, 1f, 0f);
        obj.SetActive(true);

        StartCoroutine(AnimateAndDestroy(obj, circle, approachTime));
    }

    private IEnumerator AnimateAndDestroy(GameObject obj, Image circle, float duration)
    {
        float t = 0f;
        float fadeInTime = 0.2f;
        Vector3 startScale = Vector3.one * 2f;
        Vector3 endScale = Vector3.one;

        obj.transform.localScale = startScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            obj.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            float alpha = Mathf.Clamp01(t / fadeInTime);
            circle.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        obj.transform.localScale = endScale;
        circle.color = new Color(1f, 1f, 1f, 1f);
        yield return new WaitForSeconds(0.2f);

        Destroy(obj);
    }

    public void Flash()
    {
        if (keyGlowOverlay == null) return;

        if (glowRoutine != null)
            StopCoroutine(glowRoutine);

        glowRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        keyGlowOverlay.gameObject.SetActive(true);
        yield return new WaitForSeconds(flashDuration);
        keyGlowOverlay.gameObject.SetActive(false);
    }
}