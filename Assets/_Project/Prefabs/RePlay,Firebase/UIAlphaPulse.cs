using UnityEngine;

public class UIAlphaPulse : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("깜빡임 설정")]
    [SerializeField, Range(0f, 1f)]
    private float minAlpha = 0.5f;

    [SerializeField] private float duration = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        float progress = Mathf.PingPong(Time.unscaledTime / duration, 1f);

        canvasGroup.alpha = Mathf.Lerp(1f, minAlpha, progress);
    }
}
