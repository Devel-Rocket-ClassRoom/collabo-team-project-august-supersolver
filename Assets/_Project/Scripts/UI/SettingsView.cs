using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>음량 설정과 홈 이동을 제공한다.</summary>
public class SettingsView : UIPopup
{
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] TMP_Text bgmValue;
    [SerializeField] TMP_Text sfxValue;

    void OnEnable()
    {
        var sound = SoundManager.Instance;
        bgmSlider.SetValueWithoutNotify(sound.BgmVolume);
        sfxSlider.SetValueWithoutNotify(sound.SfxVolume);
        UpdateValue(bgmValue, sound.BgmVolume);
        UpdateValue(sfxValue, sound.SfxVolume);
        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    void OnDisable()
    {
        bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        PlayerPrefs.Save();
    }

    void OnBgmChanged(float value)
    {
        SoundManager.Instance.SetBgmVolume(value);
        UpdateValue(bgmValue, value);
    }

    void OnSfxChanged(float value)
    {
        SoundManager.Instance.SetSfxVolume(value);
        UpdateValue(sfxValue, value);
    }

    static void UpdateValue(TMP_Text label, float value)
        => label.text = $"{Mathf.RoundToInt(value * 100f)}%";

    /// 홈은 판을 버리는 길이라 한 번 더 묻는다.
    public void OnClickHome()
        => UIManager.Instance.ShowPopup<HomeConfirmView>().Forget();
}
