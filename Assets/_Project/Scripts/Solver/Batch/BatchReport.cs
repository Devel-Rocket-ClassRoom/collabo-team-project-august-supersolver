using System;
using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Batch
{
    /// <summary>
    /// 리포트 json 의 모양. JsonUtility 가 읽을 수 있게
    /// 전부 public 필드다. 이름이 곧 json 의 키다.
    /// </summary>
    [Serializable]
    public sealed class BatchReport
    {
        public string generatedAt;
        public string levelFolder;

        /// -stage 로 하나만 굴렸으면 그 이름. 전부 굴렸으면 비어 있다.
        public string stageFilter;

        /// 실측 표의 개수. 0 이면 지렛대 패스가 아무것도 못 굴린다.
        public int presetCount;

        /// 스테이지로 읽히지 않은 파일. 이름과 이유다.
        public List<string> unreadable = new List<string>();

        public List<StageReport> stages = new List<StageReport>();
    }

    [Serializable]
    public sealed class StageReport
    {
        public string stageId;
        public int seed;
        public int deviceCount;

        public bool cleared;
        public int pass;
        public string passName;
        public int tries;

        /// 못 푼 경우에 볼 것이다. 아깝게 못 푼 것과 아예 못 푼 것은 다르다.
        public float bestGoalDist;

        public float seconds;

        /// 풀렸으면 답, 아니면 가장 가까이 갔던 그림.
        public Solution solution;

        /// 굴려 본 순서 그대로. 실패한 것도 들어 있다.
        public List<AttemptRow> attempts = new List<AttemptRow>();
    }

    /// <summary>
    /// 굴려 본 한 판. 뷰어의 '시도' 탭 한 줄과 같은 값이다.
    /// 획은 담지 않는다 — 판이 수천이라 그림까지 넣으면 파일이 감당이 안 된다.
    /// </summary>
    [Serializable]
    public sealed class AttemptRow
    {
        public int index;
        public int pass;
        public string passName;
        public string outcome;
        public float minGoalDist;
        public int endStep;
        public float ink;

        /// 그림이 차지한 사각형.
        public Rect area;
    }
}
