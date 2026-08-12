namespace PPS.MapEditor
{
    /// <summary>
    /// 집을 수 있는 것의 종류.
    /// 편집과 표시가 같은 말을 써야 해서 밖에 둔다.
    /// </summary>
    public enum MapHandleKind
    {
        None,
        Start,
        Goal,
        Star,
        Terrain,
        Device,
    }

    /// <summary>
    /// 고른 대상. 별·지형·장치는 개수가 변해서
    /// 종류만으로는 특정할 수 없다.
    /// </summary>
    public readonly struct MapSelection
    {
        public readonly MapHandleKind Kind;
        public readonly int Index;

        public MapSelection(MapHandleKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }

        public static MapSelection None => new MapSelection(MapHandleKind.None, -1);

        public bool Is(MapHandleKind kind, int index) => Kind == kind && Index == index;
    }
}
