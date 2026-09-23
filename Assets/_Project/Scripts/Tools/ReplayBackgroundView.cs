using UnityEngine;

namespace PPS.Tools
{
    // RePlay 카메라가 보는 전체 영역에 테마 배경을 표시한다.
    [DisallowMultipleComponent]
    public sealed class ReplayBackgroundView : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] SpriteRenderer _renderer;
        [SerializeField] ThemeAssetSet _themeAssets;

        void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

            ApplyBackground();
        }

        void LateUpdate()
        {
            FitToCamera();
        }

        void ApplyBackground()
        {
            if (_renderer == null)
            {
                Debug.LogWarning(
                    "ReplayBackgroundView의 SpriteRenderer가 없습니다.",
                    this);

                return;
            }

            if (_themeAssets == null ||
                _themeAssets.playBackground == null)
            {
                Debug.LogWarning(
                    "ReplayBackgroundView의 Theme Assets를 연결하세요.",
                    this);

                return;
            }

            _renderer.sprite = _themeAssets.playBackground;
            _renderer.color = Color.white;

            // 게임 오브젝트보다 뒤에 표시한다.
            _renderer.sortingOrder = -200;
        }

        void FitToCamera()
        {
            if (_camera == null ||
                _renderer == null ||
                _renderer.sprite == null ||
                !_camera.orthographic)
            {
                return;
            }

            float viewHeight =
                _camera.orthographicSize * 2f;

            float viewWidth =
                viewHeight * _camera.aspect;

            Vector2 spriteSize =
                _renderer.sprite.bounds.size;

            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            // 화면보다 작은 쪽이 생기지 않도록 큰 비율을 사용한다.
            float scale = Mathf.Max(
                viewWidth / spriteSize.x,
                viewHeight / spriteSize.y);

            transform.position = new Vector3(
                _camera.transform.position.x,
                _camera.transform.position.y,
                0f);

            transform.localScale =
                Vector3.one * scale;
        }
    }
}
