using PPS.Core;
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
    public ThemeLabel label;
    public Sprite Spr_SelectButton;
}