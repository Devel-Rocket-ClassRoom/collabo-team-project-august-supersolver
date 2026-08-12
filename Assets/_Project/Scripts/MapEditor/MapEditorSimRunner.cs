using PPS.Core;
using PPS.Game;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 중인 맵을 그대로 돌려본다.
    /// 켜고 끄는 일과 소비자를 붙이고 떼는 일만 한다 —
    /// 그리기와 별 세기는 각자의 몫이다.
    /// 편집 데이터는 읽기만 한다. 돌려본 뒤 맵이
    /// 달라지면 만든 것을 믿을 수 없다.
    /// </summary>
    public sealed class MapEditorSimRunner : MonoBehaviour
    {
        [SerializeField] MapEditSession _session;
        [SerializeField] MapEditHandles _handles;
        [SerializeField] GameSimDriver _driver;

        [SerializeField] MapSimView _view;
        [SerializeField] StarStacker _stars;

        bool _running;

        /// 결과를 한 번만 남기려고 둔다.
        bool _reported;

        public bool Running => _running;

        /// <summary>상단바 테스트 버튼이 부른다.</summary>
        public void Toggle()
        {
            if (_running) Stop();
            else Begin();
        }

        void Begin()
        {
            if (_session == null || _driver == null) return;

            // 그린 것 없이 레벨만 돌린다. 도구는 아직 없다.
            _driver.StartSimulation(_session.Current, Solution.Empty);
            if (!_driver.HasWorld) return;

            SimWorld world = _driver.World;

            if (_stars != null) _stars.Attach(world);

            if (_handles != null) _handles.gameObject.SetActive(false);

            _running = true;
            _reported = false;
            Debug.Log($"[맵 에디터] 테스트 시작: {_session.Current.StageId}");
        }

        void Stop()
        {
            if (_stars != null) _stars.Detach();

            _driver.Stop();

            if (_view != null) _view.HideAll();
            if (_handles != null) _handles.gameObject.SetActive(true);

            _running = false;
            Debug.Log("[맵 에디터] 테스트 종료. 편집으로 돌아간다.");
        }

        void Update()
        {
            if (!_running || !_driver.HasWorld) return;

            SimWorld world = _driver.World;

            if (_view != null) _view.OnDraw(world);

            Report(world);
        }

        /// <summary>
        /// 판정이 끝났음을 한 번 알린다.
        /// 별 개수는 스태커가 먹을 때마다 알린다.
        /// </summary>
        void Report(SimWorld world)
        {
            if (_reported || !world.IsTerminal) return;

            _reported = true;
            SimResult result = world.ToResult(0f);
            Debug.Log($"[맵 에디터] 테스트 결과: {result}");
        }

        void OnDestroy()
        {
            if (_driver != null) _driver.Stop();
        }
    }
}
