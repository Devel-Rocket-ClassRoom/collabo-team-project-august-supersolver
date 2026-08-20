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

        /// <summary>
        /// 편집의 원본. 지형 선분은 여기서 구워 낸 결과다.
        /// </summary>
        MapShapes _shapes = new MapShapes();

        /// 마지막으로 저장·불러온 파일. 없으면 빈 문자열.
        string _path = "";

        public StageData Current => _stage;

        public MapShapes Shapes => _shapes;

        /// <summary>
        /// 첫 굽기. 한 번도 손대지 않은 맵을 그대로
        /// 테스트해도 킬라인이 맞아야 한다.
        /// </summary>
        void Awake() => Bake();

        /// <summary>
        /// 실행 취소가 예전 상태로 되돌릴 때 쓴다.
        /// 통째로 갈아끼워야 고른 번호도 함께 풀린다.
        /// </summary>
        public void Replace(StageData stage, MapShapes shapes)
        {
            _stage = stage;
            _shapes = shapes;
            Bake();
        }

        /// <summary>
        /// 도형을 고친 뒤 부른다.
        /// 지형은 파생 데이터라 원본이 바뀌면 다시 굽는다.
        /// </summary>
        public void Bake()
        {
            ShapeBaker.Bake(_shapes, _stage.Level);

            // 킬라인은 플레이 영역의 아래 변이다. 저장 때만
            // 잡으면 테스트 플레이가 옛 자리에서 죽는다.
            _stage.Level.KillY = LevelDataArea.Calculate(_stage.Level).yMin;
        }

        /// <summary>빈 맵으로 새로 시작한다.</summary>
        public void NewMap()
        {
            _stage = NewStage();
            _shapes = new MapShapes { StageId = _stage.StageId };
            _path = "";
            Bake();

            Debug.Log($"[맵 에디터] 새 맵: {_stage.StageId}");
        }

        public void Save()
        {
            Bake();

            if (_path.Length == 0) _path = MapFile.PathOf(_stage.StageId);
            MapFile.Save(_stage, _shapes, _path);

            Debug.Log($"[맵 에디터] 저장: {_path}");
        }

        /// <summary>
        /// 이름을 새로 받아 저장한다.
        /// 판 이름은 파일 이름을 따라간다 — 둘이 어긋나면
        /// 도형 파일이 짝을 못 찾는다.
        /// </summary>
        public void SaveAs()
        {
#if UNITY_EDITOR
            string picked = UnityEditor.EditorUtility.SaveFilePanel(
                "다른 이름으로 저장", MapFile.Folder, _stage.StageId, "json");

            if (string.IsNullOrEmpty(picked)) return;

            _stage.StageId = System.IO.Path.GetFileNameWithoutExtension(picked);

            Bake();
            MapFile.Save(_stage, _shapes, picked);
            _path = picked;

            Debug.Log($"[맵 에디터] 다른 이름으로 저장: {_path}");
#else
            Debug.LogWarning("[맵 에디터] 다른 이름으로 저장은 에디터에서만 된다.");
#endif
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
            _shapes = MapFile.LoadShapes(picked, loaded);
            _path = picked;
            Bake();

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
                _shapes = MapFile.LoadShapes(_path, saved);
                Bake();

                Debug.Log("[맵 에디터] 초기화: 마지막 저장 상태로 되돌림");
                return;
            }

            string stageId = _stage.StageId;
            _stage = NewStage();
            _stage.StageId = stageId;
            _shapes = new MapShapes { StageId = stageId };
            Bake();

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
                },
            };
        }
    }
}
