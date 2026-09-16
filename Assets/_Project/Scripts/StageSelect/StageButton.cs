using PPS.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageButton : MonoBehaviour
{
    static bool locked = false;
    [Header("Image")]
    [SerializeField] Image Img_Locked;
    [SerializeField] Image Img_Star1;
    [SerializeField] Image Img_Star2;
    [SerializeField] Image Img_Star3;

    [Header("txt")]
    [SerializeField] TextMeshProUGUI stageNumText;

    /// 눌렀을 때 들어갈 자리.
    StageEntry entry;

    /// 잠긴 칸은 눌러도 들어가지 않는다.
    bool isLocked = true;

    public void ApplyView(StageButtonViewModel vm)
    {
        entry = vm.Entry;
        isLocked = vm.IsLocked;

        Img_Locked.sprite = vm.LockedSprite;
        Img_Locked.gameObject.SetActive(vm.IsLocked);
        stageNumText.text = vm.IsLocked ? "" : (vm.Entry.Stage + 1).ToString();

        Img_Star1.sprite = vm.StarSprite;
        Img_Star2.sprite = vm.StarSprite;
        Img_Star3.sprite = vm.StarSprite;

        Img_Star1.gameObject.SetActive(vm.Stars >= 1);
        Img_Star2.gameObject.SetActive(vm.Stars >= 2);
        Img_Star3.gameObject.SetActive(vm.Stars >= 3);
    }

    public async void OnClicked()
    {
        if (isLocked) return;
        if (locked) return;
        locked = true;
        await StageLauncher.Enter(entry);
        locked = false;
    }
}
