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

        /// 누적 잉크. h = 0 인 동안 이 배치의 f 다.
        public readonly float Ink;

        /// <summary>
        /// 같은 Key 끼리의 순서. 넣은 차례다.
        /// 다시 넣을 때도 처음 번호를 그대로 들고 간다 —
        /// 실행마다 순서가 바뀌면 같은 레벨이 다른 풀이를 낸다.
        /// </summary>
        public readonly int Seq;

        /// <summary>
        /// 열린 목록에서의 우선순위. 노드는 두 가지 일을 차례로 한다.
        /// 굴려 보기 전에는 자기 f 이고, 굴린 뒤에는 다음에 낼 자식의 f 다.
        /// 이 둘을 한 값으로 두어야 "굴려 볼 배치" 와 "낳을 배치" 가
        /// 같은 저울에 오른다.
        /// </summary>
        public float Key;

        /// 이미 굴려 봤는가. 다시 굴리는 것은 궤적 캐시가 빗나간 값이다.
        public bool Rolled;

        /// <summary>
        /// 다음에 낼 자식이 쓸 후보 번호.
        /// 노드 하나가 수만 개의 자식을 내므로 한꺼번에 만들 수 없다 —
        /// 하나씩 떼어 내고 이 값만 올려 다시 넣는다.
        /// </summary>
        public int Cursor;

        public SearchNode(Primitive[] primitives, float ink, int seq)
        {
            Primitives = primitives;
            Ink = ink;
            Seq = seq;
            Key = ink;
        }

        /// 놓은 선의 개수.
        public int Depth => Primitives.Length;
    }
}
