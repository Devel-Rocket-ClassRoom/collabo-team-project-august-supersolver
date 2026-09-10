using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ThemeAssetCatalog", menuName = "Scriptable Objects/ThemeAssetCatalog")]
public class ThemeAssetCatalog : ScriptableObject
{
    public List<ThemeAssetEntry> Asset;
}
[Serializable]
public struct ThemeAssetEntry
{
    public Sprite Spr_SelectButton;
}