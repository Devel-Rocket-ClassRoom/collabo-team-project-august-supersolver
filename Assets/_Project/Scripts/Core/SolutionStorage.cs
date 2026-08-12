using System;
using System.IO;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 그린 그림을 기기 저장소에 남긴다. 판당 최신 1개.
    /// 시뮬레이션은 이 파일을 읽지 않는다 — 메모리의
    /// Solution 을 그대로 쓴다. 여기 쓰는 목적은 솔버에
    /// 먹일 그림을 기기에서 뽑아 내는 것이다.
    /// </summary>
    public static class SolutionStorage
    {
        const string FolderName = "solutions";

        /// 프로젝트가 아니라 기기 저장소다 —
        /// 안드로이드에서 dataPath 는 APK 안이라 못 쓴다.
        public static string FolderPath =>
            Path.Combine(Application.persistentDataPath, FolderName);

        public static string PathOf(string stageId) =>
            Path.Combine(FolderPath, stageId + ".json");

        /// <summary>
        /// 같은 판을 다시 저장하면 덮는다. 이력은 남기지 않는다.
        /// </summary>
        /// <returns>쓰기에 실패하면 false.</returns>
        public static bool Save(string stageId, Solution solution)
        {
            string path = PathOf(stageId);

            try
            {
                Directory.CreateDirectory(FolderPath);
                File.WriteAllText(path, JsonUtility.ToJson(solution, true));
            }
            catch (Exception exception)
            {
                // 저장은 산출물 반출이라 실패해도
                // 판은 계속 돌아가야 한다.
                Debug.LogWarning($"그림을 저장하지 못했다: {path}\n{exception.Message}");
                return false;
            }

            return true;
        }

        /// <returns>파일이 없거나 읽히지 않으면 false.</returns>
        public static bool TryLoad(string stageId, out Solution solution)
        {
            solution = null;
            string path = PathOf(stageId);

            if (!File.Exists(path)) return false;

            try
            {
                solution = JsonUtility.FromJson<Solution>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"그림을 읽지 못했다: {path}\n{exception.Message}");
                return false;
            }

            return solution != null;
        }
    }
}
