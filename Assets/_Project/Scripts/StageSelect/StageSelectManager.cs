using System.Collections.Generic;
using UnityEngine;
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
            "M20260810_164657"
        };
    }
}
public class StageSelectManager : MonoBehaviour
{
    const int MaxShownRow = 4;
    const int StageNumInARow = StageSelectRow.StageNumInARow;

    [SerializeField] Transform parent;

    [Header("Prefab")]
    [SerializeField] StageSelectRow RowLR;
    [SerializeField] StageSelectRow RowRL;

    LinkedList<StageSelectRow> rowPool;
    
    IStageRegistry stages;

    // 화면에 보여야하는 스테이지의 시작 인덱스. 화면 가장 왼쪽 위의 스테이지 인덱스와 같음.
    int curShownStageIdxFrom;

    private void Awake()
    {
        rowPool = new();
        for (int i = 0; i < MaxShownRow; i++)
        {
            bool isL2R = i % 2 == 0;
            
            var item = isL2R 
                ? Instantiate(RowLR.gameObject, parent) 
                : Instantiate(RowRL.gameObject, parent);
            item.SetActive(false);
            rowPool.AddLast(item.GetComponent<StageSelectRow>());
        }

        ServiceLocator.Register<IStageRegistry>(new TestStageRegistry());

        OnUpdateView();
    }
    public void ScrollUp()
    {
        if (curShownStageIdxFrom - StageNumInARow < 0) return;
        curShownStageIdxFrom -= StageNumInARow;
        AddFirst(); OnUpdateView();
    }
    public void ScrollDown()
    {
        curShownStageIdxFrom += StageNumInARow;
        AddLast(); OnUpdateView();
    }
    private void OnUpdateView()
    {
        if(stages != null || ServiceLocator.TryGet<IStageRegistry>(out stages))
        {
            int offset = 0;
            foreach (var item in rowPool)
            {
                int startidx = curShownStageIdxFrom + offset;
                int maxidx = stages.Num;

                if (startidx < 0 || startidx >= maxidx)
                    item.gameObject.SetActive(false);
                else
                {
                    item.gameObject.SetActive(true);
                    item.OnUpdate(startidx, maxidx);
                }
                offset += StageNumInARow;
            }
        }
    }

    private StageSelectRow AddLast()
    {
        var item = rowPool.First;
        rowPool.RemoveFirst();
        item.Value.transform.SetAsLastSibling();
        rowPool.AddLast(item);

        return item.Value;
    }
    private StageSelectRow AddFirst()
    {
        var item = rowPool.Last;
        rowPool.RemoveLast();
        item.Value.transform.SetAsFirstSibling();
        rowPool.AddFirst(item);

        return item.Value;
    }
}
