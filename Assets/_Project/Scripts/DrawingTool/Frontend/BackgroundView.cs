using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 테마 배경 한 장을 판 뒤에 깐다. 레벨이 아니라
    /// 카메라가 보는 만큼을 덮는다 — 판 바깥 여백까지
    /// 채워야 밴드 뒤가 빈 색으로 남지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _renderer;

        CanvasCameraFitter _fitter;

        void Awake()
        {
            _fitter = CanvasCameraFitter.Instance;

            // z 가 전부 같아 이 값이 없으면 판과의 앞뒤가
            // 정해지지 않는다.
            _renderer.sortingOrder = RenderOrder.Background;
        }

        /// <summary>테마가 바뀌면 그림도 바뀐다.</summary>
        public void SetSprite(Sprite sprite) => _renderer.sprite = sprite;

        /// <summary>
        /// 화면 크기와 카메라 프레임에는 콜백이 없다.
        /// 한 장뿐이라 매 프레임 다시 재는 편이 싸다.
        /// </summary>
        void LateUpdate()
        {
            Sprite sprite = _renderer.sprite;
            if (sprite == null || !_fitter.IsReady) return;

            var screen = new Vector2(Screen.width, Screen.height);
            Vector2 view = screen / _fitter.PixelsPerUnit;
            Vector2 art = sprite.bounds.size;

            // 긴 쪽에 맞추면 짧은 쪽에 빈틈이 남는다.
            // 넘치는 만큼은 화면 밖으로 잘린다.
            float scale = Mathf.Max(view.x / art.x, view.y / art.y);

            _renderer.transform.position = _fitter.ScreenToWorld(screen * 0.5f);
            _renderer.transform.localScale = Vector3.one * scale;
        }
    }
}
