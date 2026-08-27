using System;
using PPS.Core;
using PPS.Game;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 판의 상태를 튜토리얼이 볼 수 있는 창구로 내보낸다.
    /// 드라이버와 세션을 아는 쪽은 여기까지고, 안내 층은
    /// ITutorialSignals 만 본다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialSignals : MonoBehaviour, ITutorialSignals
    {
        [SerializeField] DrawingSession _session;
        [SerializeField] GameSimDriver _driver;

        public event Action DrawingChanged;

        /// 월드가 아직 없으면 판정도 없다. 재시도 직후가 그렇다.
        public bool SimDecided => _driver.HasWorld && !_driver.IsSimulating;

        void Awake() => ServiceLocator.Register<ITutorialSignals>(this);

        void OnEnable() => _session.Changed += Raise;

        void OnDisable() => _session.Changed -= Raise;

        // 판이 사라진 뒤에도 등록이 남으면 파괴된 것을 넘긴다.
        void OnDestroy() => ServiceLocator.Unregister<ITutorialSignals>();

        void Raise() => DrawingChanged?.Invoke();
    }
}
