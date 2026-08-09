namespace PPS.Solver
{
    /// <summary>
    /// 탐색의 한 후보 배치. 프리미티브 집합과 그 값이다.
    /// 궤적은 들지 않는다 — 열린 목록이 수십만 개가 되므로
    /// 노드당 궤적을 들면 그것만으로 메모리가 찬다.
    /// 필요할 때 다시 굴려 얻는다.
    /// </summary>
    public sealed class SearchNode
    {
        /// 지금까지 놓은 선들. 순서가 곧 월드 구축 순서다.
        public readonly Primitive[] Primitives;

        /// 누적 잉크.
        public readonly float Ink;

        /// <summary>
        /// 같은 순위끼리의 순서. 넣은 차례다.
        /// 다시 넣을 때도 처음 번호를 그대로 들고 간다 —
        /// 실행마다 순서가 바뀌면 같은 레벨이 다른 풀이를 낸다.
        /// </summary>
        public readonly int Seq;

        /// <summary>
        /// 우선순위 1축. 이 풀이가 최종적으로 쓸 선 개수 추정치 = g + h.
        /// 둘 다 개수라 더할 수 있다 — 잉크(미터)를 섞으면 뜻이 없어진다.
        /// 노드는 두 가지 일을 차례로 한다. 굴려 보기 전에는 자기 추정치이고,
        /// 굴린 뒤에는 다음에 낼 자식의 추정치다. 그래야
        /// "굴려 볼 배치" 와 "낳을 배치" 가 같은 저울에 오른다.
        /// </summary>
        public int KeyLines;

        /// <summary>
        /// 우선순위 2축. 누적 잉크로 동점을 가른다.
        /// h 가 정수라 1축은 대량으로 겹치는데, 그때 같은 선 개수면
        /// 싼 쪽을 고르게 한다. 1축과 더하지 않으므로 단위가 안 섞인다.
        /// </summary>
        public float KeyInk;

        /// 이미 굴려 봤는가. 다시 굴리는 것은 궤적 캐시가 빗나간 값이다.
        public bool Rolled;

        /// <summary>
        /// 다음에 낼 자식이 쓸 후보 번호.
        /// 노드 하나가 수만 개의 자식을 내므로 한꺼번에 만들 수 없다 —
        /// 하나씩 떼어 내고 이 값만 올려 다시 넣는다.
        /// </summary>
        public int Cursor;

        /// <param name="estimate">부모가 물려준 g + h. 굴려 보기 전의 순위다.
        /// 자기 궤적이 없어 h 를 직접 못 구하므로, 부모가 선을 놓은
        /// 자리의 h 로 대신한다.</param>
        public SearchNode(Primitive[] primitives, float ink, int seq, int estimate)
        {
            Primitives = primitives;
            Ink = ink;
            Seq = seq;
            KeyLines = estimate;
            KeyInk = ink;
        }

        /// 놓은 선의 개수. 곧 g 다.
        public int Depth => Primitives.Length;
    }
}
