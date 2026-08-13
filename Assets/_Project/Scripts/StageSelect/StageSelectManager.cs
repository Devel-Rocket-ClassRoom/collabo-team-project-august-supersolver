using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectManager : MonoBehaviour
{
    const int StagePerTheme = 20;
    const int StagePerRow = StageSelectRow.StagePerRow;
    const int MaxRow = StagePerTheme / StageSelectRow.StagePerRow + 1;

    [SerializeField] Transform parent;
    [SerializeField] ScrollRect scroll;

    [Header("Prefab")]
    [SerializeField] StageSelectRow RowLR;
    [SerializeField] StageSelectRow RowRL;

    List<StageSelectRow> rowPool;

    private void Awake()
    {
        rowPool = new();

        for (int i = 0; i < MaxRow; i++)
        {
            bool isL2R = i % 2 == 0;
            
            var item = isL2R 
                ? Instantiate(RowLR.gameObject, parent) 
                : Instantiate(RowRL.gameObject, parent);

            rowPool.Add(item.GetComponent<StageSelectRow>());
        }

        OnUpdateView();
    }
    private void OnUpdateView()
    {
        for(int i = 0; i < MaxRow; i++)
        {
            rowPool[i].OnUpdate(i * StagePerRow, StagePerTheme);
        }
    }

}
