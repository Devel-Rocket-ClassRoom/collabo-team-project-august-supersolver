using UnityEngine;
using UnityEngine.UI;

public class StageSceneLoaderOnClick : MonoBehaviour
{
    bool locked;
    public async void OnClicked()
    {
        if (locked) return;
        locked = true;
        await UIManager.Instance.ShowScene<StageSelectView>();
        locked = false;
    }
}
