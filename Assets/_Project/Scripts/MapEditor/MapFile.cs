using System.IO;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 맵 파일 읽기·쓰기.
    /// 에디터 툴이라 결과물이 저장소에 남아야 한다 —
    /// 기기 저장소가 아니라 프로젝트 안에 쓴다.
    /// </summary>
    public static class MapFile
    {
        public const string Folder = "Assets/_Project/Levels";

        public static string PathOf(string stageId) => $"{Folder}/{stageId}.json";

        /// <summary>
        /// 도형 그룹은 에디터만 읽는다. 레벨 파일에
        /// 섞으면 게임·솔버가 쓰지 않는 값을 지고 간다.
        /// </summary>
        public static string ShapePathOf(string stageId) => $"{Folder}/{stageId}.edit.json";

        public static void Save(StageData stage, MapShapes shapes)
        {
            Directory.CreateDirectory(Folder);

            File.WriteAllText(PathOf(stage.StageId), stage.ToJson());

            if (shapes != null)
            {
                shapes.StageId = stage.StageId;
                File.WriteAllText(ShapePathOf(stage.StageId), shapes.ToJson());
            }

#if UNITY_EDITOR
            // 새로 쓴 파일을 프로젝트 창이 알아채게 한다.
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        /// <returns>파일이 없거나 형식이 깨졌으면 false.</returns>
        public static bool TryLoad(string path, out StageData stage)
        {
            stage = null;
            if (!File.Exists(path)) return false;

            stage = StageData.FromJson(File.ReadAllText(path));
            return stage != null && stage.Level != null;
        }

        /// <summary>
        /// 도형 파일을 읽는다. 없으면 지형 선분을
        /// 하나씩 도형으로 본다 — 예전 레벨도 열린다.
        /// </summary>
        public static MapShapes LoadShapes(string stagePath, StageData stage)
        {
            string path = ShapePathFor(stagePath);

            if (File.Exists(path))
            {
                var shapes = MapShapes.FromJson(File.ReadAllText(path));

                // 짝이 어긋난 파일을 물면 엉뚱한 도형이 뜬다.
                if (shapes != null && shapes.StageId == stage.StageId) return shapes;

                Debug.LogWarning($"[맵 에디터] 도형 파일의 판 이름이 다르다: {path}");
            }

            return ShapeBaker.FromTerrain(stage.Level, stage.StageId);
        }

        static string ShapePathFor(string stagePath)
        {
            string directory = Path.GetDirectoryName(stagePath);
            string name = Path.GetFileNameWithoutExtension(stagePath);

            return Path.Combine(directory ?? Folder, name + ".edit.json");
        }
    }
}
