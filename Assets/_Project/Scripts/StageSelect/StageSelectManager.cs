using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public interface IStageRegistry 
{
    public string[] StageIDs { get; }
    public int Num { get => StageIDs.Length; }
}
public class TestStageRegistry : IStageRegistry
{
    public string[] StageIDs { get; }

    public TestStageRegistry()
    {
        StageIDs = new string[]
        {
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
            "M20260810_164657",
        };
    }
}
public class StageSelectManager : MonoBehaviour
{
    const int StagePerRow = StageSelectRow.StagePerRow;

    [SerializeField] Transform parent;
    [SerializeField] ScrollRect scroll;

    [Header("Prefab")]
    [SerializeField] StageSelectRow RowLR;
    [SerializeField] StageSelectRow RowRL;

    LinkedList<StageSelectRow> rowPool;
    
    IStageRegistry stages;
    int MaxRow => stages.Num;

    private void Awake()
    {
        rowPool = new();
        ServiceLocator.Register<IStageRegistry>(new TestStageRegistry());
        ServiceLocator.TryGet<IStageRegistry>(out stages);

        for (int i = 0; i < MaxRow; i++)
        {
            bool isL2R = i % 2 == 0;
            
            var item = isL2R 
                ? Instantiate(RowLR.gameObject, parent) 
                : Instantiate(RowRL.gameObject, parent);
            item.SetActive(false);

            rowPool.AddLast(item.GetComponent<StageSelectRow>());
        }

        OnUpdateView();
    }
    private void OnUpdateView()
    {
        int rowidx = 0;
        foreach (var item in rowPool)
        {
            int maxidx = stages.Num;

            if (rowidx < 0 || rowidx >= maxidx)
                item.gameObject.SetActive(false);
            else
            {
                item.gameObject.SetActive(true);
                item.OnUpdate(rowidx, maxidx);
            }
            rowidx += StagePerRow;
        }
    }

}
