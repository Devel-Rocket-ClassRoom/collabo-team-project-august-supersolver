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

        public static void Save(StageData stage)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(PathOf(stage.StageId), stage.ToJson());

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
    }
}
