using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Bound")]
    [SerializeField] private RectTransform targetVisual;

    [Header("Bound Targets (Auto)")]
    [SerializeField] private List<Image> images = new List<Image>();
    [SerializeField] private List<TMP_Text> tmpTexts = new List<TMP_Text>();

    [Space(6)]
    [SerializeField] private List<Image> excludeImages = new List<Image>();
    [SerializeField] private List<TMP_Text> excludeTmpTexts = new List<TMP_Text>();

    [Header("Pointer Down")]
    [SerializeField] private float scaleAmount = 0.95f;
    [SerializeField] private float duration = 0.1f;

    [Header("Pointer Up")]
    [SerializeField] private float punchStrength = 0.08f;
    [SerializeField] private float punchDuration = 0.15f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float pressedColorMultiplier = 0.85f;

    private RectTransform actualTarget;
    private Vector3 originalScale;

    private Coroutine runtimeRoutine;

    private readonly Dictionary<Image, Color> originalImageColors = new Dictionary<Image, Color>();
    private readonly Dictionary<TMP_Text, Color> originalTmpColors = new Dictionary<TMP_Text, Color>();

    void Awake()
    {
        actualTarget = targetVisual != null ? targetVisual : GetComponent<RectTransform>();
        if (actualTarget != null) originalScale = actualTarget.localScale;

        RebindTargets();
        CacheOriginalColors();
    }

    void OnDisable()
    {
        StopRuntimeRoutineIfNeeded();

        if (actualTarget != null)
            actualTarget.localScale = originalScale;

        RestoreOriginalColors();
    }

    [ContextMenu("Rebind Targets Now")]
    public void RebindTargets()
    {
        images.Clear();
        tmpTexts.Clear();

        images.AddRange(GetComponentsInChildren<Image>(true));
        tmpTexts.AddRange(GetComponentsInChildren<TMP_Text>(true));

        RemoveNullAndDuplicates(images);
        RemoveNullAndDuplicates(tmpTexts);

        RemoveNullAndDuplicates(excludeImages);
        RemoveNullAndDuplicates(excludeTmpTexts);

        ApplyExcludesToBoundLists();

        CacheOriginalColors();
    }

    private void ApplyExcludesToBoundLists()
    {
        if (excludeImages != null && excludeImages.Count > 0)
        {
            var excludeSet = new HashSet<Image>(excludeImages);
            for (int i = images.Count - 1; i >= 0; i--)
            {
                var img = images[i];
                if (img == null || excludeSet.Contains(img))
                    images.RemoveAt(i);
            }
        }

        if (excludeTmpTexts != null && excludeTmpTexts.Count > 0)
        {
            var excludeSet = new HashSet<TMP_Text>(excludeTmpTexts);
            for (int i = tmpTexts.Count - 1; i >= 0; i--)
            {
                var t = tmpTexts[i];
                if (t == null || excludeSet.Contains(t))
                    tmpTexts.RemoveAt(i);
            }
        }
    }

    private void CacheOriginalColors()
    {
        originalImageColors.Clear();
        originalTmpColors.Clear();

        for (int i = 0; i < images.Count; i++)
        {
            var img = images[i];
            if (img == null) continue;
            if (!originalImageColors.ContainsKey(img))
                originalImageColors.Add(img, img.color);
        }

        for (int i = 0; i < tmpTexts.Count; i++)
        {
            var t = tmpTexts[i];
            if (t == null) continue;
            if (!originalTmpColors.ContainsKey(t))
                originalTmpColors.Add(t, t.color);
        }
    }

    private void RestoreOriginalColors()
    {
        foreach (var kv in originalImageColors)
        {
            if (kv.Key != null) kv.Key.color = kv.Value;
        }

        foreach (var kv in originalTmpColors)
        {
            if (kv.Key != null) kv.Key.color = kv.Value;
        }
    }

    private void StopRuntimeRoutineIfNeeded()
    {
        if (runtimeRoutine != null)
        {
            StopCoroutine(runtimeRoutine);
            runtimeRoutine = null;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || actualTarget == null) return;

        StopRuntimeRoutineIfNeeded();
        runtimeRoutine = StartCoroutine(AnimateDown());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || actualTarget == null) return;

        StopRuntimeRoutineIfNeeded();
        runtimeRoutine = StartCoroutine(PunchUpAndRestore());
    }

    private IEnumerator AnimateDown()
    {
        float elapsed = 0f;

        Vector3 startScale = actualTarget.localScale;

        var startImageColors = SnapshotCurrentImageColors();
        var startTmpColors = SnapshotCurrentTmpColors();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutSin01(elapsed / duration);

            actualTarget.localScale = Vector3.Lerp(startScale, originalScale * scaleAmount, t);
            LerpAllColorsToPressed(startImageColors, startTmpColors, t);

            yield return null;
        }

        actualTarget.localScale = originalScale * scaleAmount;
        SetAllColorsPressed();
    }

    private IEnumerator PunchUpAndRestore()
    {
        float elapsed = 0f;

        var startImageColors = SnapshotCurrentImageColors();
        var startTmpColors = SnapshotCurrentTmpColors();

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / punchDuration);

            float punch = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * punchStrength;
            actualTarget.localScale = originalScale * (1f + punch);

            LerpAllColorsToOriginal(startImageColors, startTmpColors, t);

            yield return null;
        }

        actualTarget.localScale = originalScale;
        RestoreOriginalColors();
    }

    private float EaseOutSin01(float t)
    {
        t = Mathf.Clamp01(t);
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }

    private Dictionary<Image, Color> SnapshotCurrentImageColors()
    {
        var dict = new Dictionary<Image, Color>(images.Count);
        for (int i = 0; i < images.Count; i++)
        {
            var img = images[i];
            if (img == null) continue;
            dict[img] = img.color;
        }
        return dict;
    }

    private Dictionary<TMP_Text, Color> SnapshotCurrentTmpColors()
    {
        var dict = new Dictionary<TMP_Text, Color>(tmpTexts.Count);
        for (int i = 0; i < tmpTexts.Count; i++)
        {
            var t = tmpTexts[i];
            if (t == null) continue;
            dict[t] = t.color;
        }
        return dict;
    }

    private Color MultiplyRGB(Color c, float mul)
    {
        return new Color(c.r * mul, c.g * mul, c.b * mul, c.a);
    }

    private void LerpAllColorsToPressed(Dictionary<Image, Color> startImageColors,
                                        Dictionary<TMP_Text, Color> startTmpColors,
                                        float t)
    {
        foreach (var kv in startImageColors)
        {
            var img = kv.Key;
            if (img == null) continue;

            Color original = originalImageColors.TryGetValue(img, out var o) ? o : kv.Value;
            Color target = MultiplyRGB(original, pressedColorMultiplier);
            img.color = Color.Lerp(kv.Value, target, t);
        }

        foreach (var kv in startTmpColors)
        {
            var txt = kv.Key;
            if (txt == null) continue;

            Color original = originalTmpColors.TryGetValue(txt, out var o) ? o : kv.Value;
            Color target = MultiplyRGB(original, pressedColorMultiplier);
            txt.color = Color.Lerp(kv.Value, target, t);
        }
    }

    private void LerpAllColorsToOriginal(Dictionary<Image, Color> startImageColors,
                                         Dictionary<TMP_Text, Color> startTmpColors,
                                         float t)
    {
        foreach (var kv in startImageColors)
        {
            var img = kv.Key;
            if (img == null) continue;

            if (originalImageColors.TryGetValue(img, out var original))
                img.color = Color.Lerp(kv.Value, original, t);
        }

        foreach (var kv in startTmpColors)
        {
            var txt = kv.Key;
            if (txt == null) continue;

            if (originalTmpColors.TryGetValue(txt, out var original))
                txt.color = Color.Lerp(kv.Value, original, t);
        }
    }

    private void SetAllColorsPressed()
    {
        for (int i = 0; i < images.Count; i++)
        {
            var img = images[i];
            if (img == null) continue;

            if (originalImageColors.TryGetValue(img, out var original))
                img.color = MultiplyRGB(original, pressedColorMultiplier);
        }

        for (int i = 0; i < tmpTexts.Count; i++)
        {
            var txt = tmpTexts[i];
            if (txt == null) continue;

            if (originalTmpColors.TryGetValue(txt, out var original))
                txt.color = MultiplyRGB(original, pressedColorMultiplier);
        }
    }

    private static void RemoveNullAndDuplicates<T>(List<T> list) where T : Object
    {
        if (list == null) return;

        var seen = new HashSet<T>();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var item = list[i];
            if (item == null || !seen.Add(item))
                list.RemoveAt(i);
        }
    }
}