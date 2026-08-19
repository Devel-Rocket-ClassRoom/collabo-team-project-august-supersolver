using UnityEngine;
using UnityEngine.UI;

public class StageSceneLoaderOnClick : MonoBehaviour
{
    bool locked;
    public async void OnClickedHome()
    {
        if (locked) return;
        locked = true;
        await UIManager.Instance.ShowScene<StageSelectView>();
        await UIManager.Instance.HidePopup(true);
        locked = false;
    }
}
