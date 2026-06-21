using UnityEngine;
using UnityEngine.UI;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// Flat, pre-sized arrays of the widgets a scenario mutates each frame. Built once by
    /// <see cref="UISceneBuilder"/>; drivers walk these by integer index so the per-frame
    /// hot path never calls GetComponent or allocates.
    /// </summary>
    public sealed class WidgetRegistry
    {
        /// <summary>Root canvas the whole rig lives under.</summary>
        public Canvas RootCanvas;

        /// <summary>Generated root object holding every section; destroy it to tear the rig down.</summary>
        public GameObject Root;

        /// <summary>Every image in the card grid plus free layer. Targets for color / vertex dirtying.</summary>
        public Image[] AllImages;

        /// <summary>Subset of images using <see cref="Image.Type.Filled"/>, driven via fillAmount.</summary>
        public Image[] FilledImages;

        /// <summary>Subset of card images whose material is swapped by the MaterialToggle scenario.</summary>
        public Image[] MaterialTargets;

        /// <summary>Leaf layout elements at the bottom of the deep-layout columns, driven by LayoutThrash.</summary>
        public LayoutElement[] LayoutLeaves;

        /// <summary>Absolutely positioned rects in the free layer, moved by TransformMove.</summary>
        public RectTransform[] FreeRects;

        /// <summary>Empty parent under which the Churn scenario instantiates and destroys widgets.</summary>
        public RectTransform ChurnParent;

        /// <summary>Sprite handed to churn-created images.</summary>
        public Sprite ChurnSprite;

        /// <summary>Total graphic count across the rig, for HUD/report context.</summary>
        public int TotalGraphics;
    }
}
