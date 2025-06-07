using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class KeyHitCircle : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject approachCirclePrefab;
    public Transform approachCircleContainer;
    public Image keyGlowOverlay;

    [Header("Settings")]
    public float flashDuration = 0.05f;
    public float fadeOutDuration = 0.3f;
    public float stayTimeAfterHit = 0.2f;

    private Coroutine glowRoutine;

    private class ApproachInstance
    {
        public GameObject obj;
        public Image img;
        public Coroutine fadeCoroutine;
    }

    private List<ApproachInstance> activeCircles = new();

    public void AddApproachCircle(float scale, float alpha)
    {
        if (approachCirclePrefab == null || approachCircleContainer == null) return;

        GameObject obj = Instantiate(approachCirclePrefab, approachCircleContainer);
        Image img = obj.GetComponent<Image>();
        if (img == null) return;

        obj.transform.localScale = Vector3.one * scale;
        img.color = new Color(1f, 1f, 1f, alpha);
        obj.SetActive(true);

        ApproachInstance inst = new()
        {
            obj = obj,
            img = img,
            fadeCoroutine = null
        };
        activeCircles.Add(inst);
    }

    public void UpdateApproachCircleScale(int index, float scale, float alpha)
    {
        if (index < 0 || index >= activeCircles.Count) return;

        var inst = activeCircles[index];
        inst.obj.transform.localScale = Vector3.one * scale;
        var color = inst.img.color;
        color.a = alpha;
        inst.img.color = color;
    }

    public void FadeOutApproachCircle(int index)
    {
        if (index < 0 || index >= activeCircles.Count) return;

        var inst = activeCircles[index];
        if (inst.fadeCoroutine != null) StopCoroutine(inst.fadeCoroutine);
        inst.fadeCoroutine = StartCoroutine(FadeOutRoutine(inst));
    }

    public void ClearAll()
    {
        foreach (var inst in activeCircles)
        {
            if (inst.obj != null) Destroy(inst.obj);
        }
        activeCircles.Clear();
    }

    private IEnumerator FadeOutRoutine(ApproachInstance inst)
    {
        yield return new WaitForSeconds(stayTimeAfterHit);

        float t = 0f;
        Color start = inst.img.color;
        Color end = new Color(start.r, start.g, start.b, 0f);

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            inst.img.color = Color.Lerp(start, end, t / fadeOutDuration);
            yield return null;
        }

        if (inst.obj != null) Destroy(inst.obj);
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

    public int GetCircleCount()
    {
        return activeCircles.Count;
    }
}