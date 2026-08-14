using UnityEngine;
using UnityEngine.UI;

public class StageSelectPaperScroller : MonoBehaviour
{
    [SerializeField] ScrollRect scroll;
    RectTransform rect;
    float y;
    float h;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        y = rect.position.y;
        h = scroll.GetComponent<RectTransform>().rect.height;
    }
    private void OnEnable()
    {
        scroll.onValueChanged.AddListener(OnScolled);
    }
    private void OnDisable()
    {
        scroll.onValueChanged.RemoveListener(OnScolled);
    }
    void OnScolled(Vector2 v)
    {
        Vector3 pos = rect.position;
        rect.position = new Vector3(pos.x, y + h * (1-v.y), pos.z);
    }
}
