using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 중인 스테이지 한 판을 들고 있다.
    /// 더보기 메뉴가 부르는 네 동작이 여기 있다.
    /// </summary>
    public sealed class MapEditSession : MonoBehaviour
    {
        StageData _stage = NewStage();

        /// 마지막으로 저장·불러온 파일. 없으면 빈 문자열.
        string _path = "";

        public StageData Current => _stage;

        /// <summary>
        /// 실행 취소가 예전 상태로 되돌릴 때 쓴다.
        /// 통째로 갈아끼워야 고른 번호도 함께 풀린다.
        /// </summary>
        public void Replace(StageData stage) => _stage = stage;

        /// <summary>빈 맵으로 새로 시작한다.</summary>
        public void NewMap()
        {
            _stage = NewStage();
            _path = "";
            Debug.Log($"[맵 에디터] 새 맵: {_stage.StageId}");
        }

        public void Save()
        {
            MapFile.Save(_stage);
            _path = MapFile.PathOf(_stage.StageId);
            Debug.Log($"[맵 에디터] 저장: {_path}");
        }

        public void Load()
        {
#if UNITY_EDITOR
            string picked = UnityEditor.EditorUtility.OpenFilePanel(
                "맵 불러오기", MapFile.Folder, "json");

            if (string.IsNullOrEmpty(picked)) return;

            if (!MapFile.TryLoad(picked, out var loaded))
            {
                Debug.LogWarning($"[맵 에디터] 열 수 없는 파일: {picked}");
                return;
            }

            _stage = loaded;
            _path = picked;
            Debug.Log($"[맵 에디터] 불러오기: {_stage.StageId}");
#else
            Debug.LogWarning("[맵 에디터] 불러오기는 에디터에서만 된다.");
#endif
        }

        /// <summary>
        /// 마지막 저장 상태로 되돌린다.
        /// 저장한 적이 없으면 빈 맵이 된다.
        /// </summary>
        public void ResetMap()
        {
            if (_path.Length > 0 && MapFile.TryLoad(_path, out var saved))
            {
                _stage = saved;
                Debug.Log("[맵 에디터] 초기화: 마지막 저장 상태로 되돌림");
                return;
            }

            string stageId = _stage.StageId;
            _stage = NewStage();
            _stage.StageId = stageId;
            Debug.Log("[맵 에디터] 초기화: 빈 맵 (저장본 없음)");
        }

        /// <summary>
        /// 시드는 0 으로 둔다. 스테이지의 성질이라
        /// 만들 때마다 달라지면 같은 맵이 아니게 된다.
        /// </summary>
        static StageData NewStage()
        {
            return new StageData
            {
                StageId = "M" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                Seed = 0,
                Level = new LevelData
                {
                    BallStart = new Vector2(-3f, 2f),
                    GoalPosition = new Vector2(3f, -1f),
                    KillY = -8f,
                },
            };
        }
    }
}
