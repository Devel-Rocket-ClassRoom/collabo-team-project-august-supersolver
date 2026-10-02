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

        /// 도형 파일만 모아 두는 하위 폴더 이름.
        public const string ShapeFolderName = "EditorJson";

        public static string PathOf(string stageId) => $"{Folder}/{stageId}.json";

        /// <summary>
        /// 도형 그룹은 에디터만 읽는다. 레벨 파일과
        /// 섞이면 게임·솔버가 쓰지 않는 값이 눈에 걸린다.
        /// </summary>
        public static string ShapePathOf(string stageId)
            => $"{Folder}/{ShapeFolderName}/{stageId}.edit.json";

        /// <param name="stagePath">쓸 레벨 파일 경로. 비우면 기본 폴더.</param>
        public static void Save(StageData stage, MapShapes shapes, string stagePath = null)
        {
            if (string.IsNullOrEmpty(stagePath)) stagePath = PathOf(stage.StageId);

            Directory.CreateDirectory(Path.GetDirectoryName(stagePath) ?? Folder);
            File.WriteAllText(stagePath, stage.ToJson());

            if (shapes != null)
            {
                shapes.StageId = stage.StageId;

                string shapePath = ShapePathFor(stagePath);
                Directory.CreateDirectory(Path.GetDirectoryName(shapePath));
                File.WriteAllText(shapePath, shapes.ToJson());
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

            // 하위 폴더로 옮기기 전에 저장한 파일은 레벨 옆에 있다.
            if (!File.Exists(path)) path = LegacyShapePathFor(stagePath);

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

            return Path.Combine(directory ?? Folder, ShapeFolderName, name + ".edit.json");
        }

        static string LegacyShapePathFor(string stagePath)
        {
            string directory = Path.GetDirectoryName(stagePath);
            string name = Path.GetFileNameWithoutExtension(stagePath);

            return Path.Combine(directory ?? Folder, name + ".edit.json");
        }
    }
}
