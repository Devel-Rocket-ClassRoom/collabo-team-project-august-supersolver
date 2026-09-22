using UnityEngine;

namespace PPS.MapEditor
{
    [CreateAssetMenu(fileName = "MapEditorVisuals", menuName = "PPS/Map Editor Visuals")]
    public sealed class MapEditorVisuals : ScriptableObject
    {
        [Header("World overlays")]
        public Sprite RotationRing;
        public Sprite ResizeHandle;
        public Sprite VertexHandle;
        public Sprite Eraser;
        public Sprite Line;

        [Header("Panel (9-sliced sprites)")]
        public Sprite Panel;
        public Sprite Button;
        public Sprite SelectedButton;
        public Sprite ValueBackground;
        public Sprite Scrollbar;

        [Header("Tool icons")]
        public Sprite MoveIcon;
        public Sprite RotateIcon;
        public Sprite ResizeIcon;

        [Header("Text")]
        public Color Text = Color.white;
        public Color MutedText = new Color32(176, 189, 206, 255);
        public Color SelectedText = new Color32(28, 34, 45, 255);
        public Color ErrorText = new Color32(255, 161, 147, 255);
    }
}
