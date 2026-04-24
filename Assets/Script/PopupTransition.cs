using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PopupTransition : MonoBehaviour
{
    [Header("Dimmed Settings")]
    [SerializeField] private Image dimmed;
    [SerializeField] private float dimmedDuration = 0.2f;

    [Header("Scale Settings")]
    [SerializeField] private Transform frame;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField]
    private AnimationCurve scaleEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private float _dimmedTargetAlpha;
    private Vector3 _frameTargetScale;
    private Coroutine _playing;

    private void Reset()
    {
        AutoBindIfNull();
    }

    private void Awake()
    {
        AutoBindIfNull();
        CacheTargets();
    }

    private void AutoBindIfNull()
    {
        // Dimmed가 바인딩 안 되어 있으면 하위 자식에서 "Dimmed" 이름으로 탐색
        if (dimmed == null)
        {
            dimmed = FindInChildrenByName<Image>("Dimmed");
        }

        // Frame이 바인딩 안 되어 있으면 하위 자식에서 "Frame" 이름으로 탐색
        if (frame == null)
        {
            Transform found = FindInChildrenByName<Transform>("Frame");
            if (found != null)
            {
                frame = found;
            }
        }
    }

    private T FindInChildrenByName<T>(string targetName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.name == targetName)
            {
                return components[i];
            }
        }
        return null;
    }

    private void CacheTargets()
    {
        if (dimmed != null)
        {
            _dimmedTargetAlpha = dimmed.color.a;
        }

        if (frame != null)
        {
            _frameTargetScale = frame.localScale;
        }
    }

    private void OnEnable()
    {
        PlayOpen();
    }

    public void PlayOpen()
    {
        if (_playing != null)
        {
            StopCoroutine(_playing);
        }
        _playing = StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        // 초기 상태 세팅
        if (dimmed != null)
        {
            Color c = dimmed.color;
            c.a = 0f;
            dimmed.color = c;
        }

        if (frame != null)
        {
            frame.localScale = Vector3.zero;
        }

        float maxDuration = Mathf.Max(dimmedDuration, scaleDuration);
        float elapsed = 0f;

        while (elapsed < maxDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Dimmed Alpha 애니메이션 (선형)
            if (dimmed != null && dimmedDuration > 0f)
            {
                float t = Mathf.Clamp01(elapsed / dimmedDuration);
                Color c = dimmed.color;
                c.a = Mathf.Lerp(0f, _dimmedTargetAlpha, t);
                dimmed.color = c;
            }

            // Frame Scale 애니메이션 (커브 이징)
            if (frame != null && scaleDuration > 0f)
            {
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                float eased = scaleEase.Evaluate(t);
                frame.localScale = Vector3.LerpUnclamped(Vector3.zero, _frameTargetScale, eased);
            }

            yield return null;
        }

        // 최종값 보정
        if (dimmed != null)
        {
            Color c = dimmed.color;
            c.a = _dimmedTargetAlpha;
            dimmed.color = c;
        }

        if (frame != null)
        {
            frame.localScale = _frameTargetScale;
        }

        _playing = null;
    }
}