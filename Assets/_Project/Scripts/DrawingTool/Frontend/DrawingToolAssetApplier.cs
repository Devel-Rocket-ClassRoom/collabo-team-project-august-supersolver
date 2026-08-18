using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 테마 에셋을 드로잉툴 화면에 물린다. 뷰가 저마다
    /// 레포지토리를 찾으면 테마가 바뀌는 시점이 뷰 수만큼
    /// 갈린다 — 넣는 자리를 하나로 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawingToolAssetApplier : MonoBehaviour
    {
        [SerializeField] BackgroundView _background;

        IThemeRepository _repository;

        void OnEnable()
        {
            // 씬을 단독으로 열면 레포지토리가 없다. 조용히
            // 넘기면 배경 없는 화면을 배선 실수로 읽는다.
            if (!ServiceLocator.TryGet<IThemeRepository>(out _repository))
            {
                Debug.LogWarning("테마 레포지토리가 없어 배경을 못 물린다.", this);
                return;
            }

            _repository.OnLoaded += Apply;
            Apply();
        }

        void OnDisable()
        {
            if (_repository != null) _repository.OnLoaded -= Apply;
        }

        void Apply() => _background.SetSprite(_repository.Asset.PlayBackground);
    }
}
