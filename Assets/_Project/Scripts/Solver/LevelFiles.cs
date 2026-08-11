using System;
using System.Collections.Generic;
using System.IO;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// Levels 폴더의 json 을 스테이지로 읽어 온다.
    /// 같은 폴더에 스테이지가 아닌 것도 섞여 있다 — 풀이 파일이 그렇고,
    /// 스테이지 껍데기 없이 레벨만 담은 옛 파일도 있다.
    /// 읽히지 않는 것은 버리지 않고 이유와 함께 남긴다.
    /// </summary>
    public static class LevelFiles
    {
        public const string RelativeFolder = "_Project/Levels";

        public static string Folder => Path.Combine(Application.dataPath, RelativeFolder);

        /// <summary>파일 하나를 읽어 본 결과.</summary>
        public readonly struct Entry
        {
            /// 확장자를 뗀 파일 이름.
            public readonly string Name;

            /// 읽혔으면 스테이지. 아니면 null.
            public readonly StageData Stage;

            /// 못 읽었으면 그 이유. 읽혔으면 null.
            public readonly string Problem;

            public Entry(string name, StageData stage, string problem)
            {
                Name = name;
                Stage = stage;
                Problem = problem;
            }

            public bool Usable => Stage != null;
        }

        /// <summary>
        /// 폴더의 json 전부. 이름 순이다 —
        /// 파일 나열 순서는 OS 가 정해서 볼 때마다 목록이 흔들린다.
        /// </summary>
        public static List<Entry> LoadAll()
        {
            var entries = new List<Entry>();
            if (!Directory.Exists(Folder)) return entries;

            string[] paths = Directory.GetFiles(Folder, "*.json");
            Array.Sort(paths, StringComparer.Ordinal);

            for (int i = 0; i < paths.Length; i++) entries.Add(Read(paths[i]));

            return entries;
        }

        /// <summary>
        /// 스테이지로 먼저 읽고, 안 되면 레벨만 담긴 것으로 본다.
        /// JsonUtility 는 맞지 않는 필드를 조용히 넘겨서 예외로는 못 가른다 —
        /// 읽은 결과가 레벨 구실을 하는지 보고 판단한다.
        /// </summary>
        static Entry Read(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string json;

            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                return new Entry(name, null, $"읽지 못했다 — {e.Message}");
            }

            if (Parse<StageData>(json, out StageData stage, out string broken)
                && IsLevel(stage?.Level))
                return new Entry(name, Renamed(stage, name), null);

            if (broken != null) return new Entry(name, null, $"json 이 아니다 — {broken}");

            if (Parse<LevelData>(json, out LevelData level, out broken) && IsLevel(level))
                return new Entry(
                    name,
                    new StageData { StageId = name, Seed = 0, Level = level },
                    null);

            return new Entry(name, null, "스테이지도 레벨도 아니다 (지형이 없다)");
        }

        static bool Parse<T>(string json, out T value, out string broken)
        {
            broken = null;

            try
            {
                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (Exception e)
            {
                value = default;
                broken = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 레벨 구실을 하는가.
        /// 지형이 하나도 없으면 공이 닿을 것이 없어 판이 성립하지 않는다 —
        /// 풀이 파일처럼 아예 다른 것도 여기서 걸린다.
        /// </summary>
        static bool IsLevel(LevelData level)
            => level != null && level.Terrain != null && level.Terrain.Count > 0;

        /// <summary>
        /// 식별자를 파일 이름으로 바꾼다.
        /// 뷰어가 식별자로 탐색 결과를 캐시하는데, 두 파일이 같은 식별자를
        /// 들고 있으면 서로의 결과를 덮어쓴다.
        /// </summary>
        static StageData Renamed(StageData stage, string name)
        {
            stage.StageId = name;
            return stage;
        }
    }
}
