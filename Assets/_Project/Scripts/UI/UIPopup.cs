using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public abstract class UIPopup : UIPanel
{
    [Header("Dimmed Background")]
    [SerializeField] private Button dimmed;
    [SerializeField] private bool closeOnDimmedClick = true;

    public override void Initialize()
    {
        base.Initialize();

        if (dimmed != null && closeOnDimmedClick)
            dimmed.onClick.AddListener(OnClickBack);
    }

    public void OnClickBack()
    {
        UIManager.Instance.HidePopup().Forget();
    }
}
