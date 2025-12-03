namespace UnityEngine.UI
{
    public interface IClipper
    {
        /// <remarks>
        /// Called after layout and before Graphic update of the Canvas update loop.
        /// </remarks>

        void PerformClipping();
    }

    public interface IClippable
    {
        GameObject gameObject { get; }

        void RecalculateClipping();

        RectTransform rectTransform { get; }

        /// <param name="clipRect">The Rectangle in which to clip against.</param>
        /// <param name="validRect">Is the Rect valid. If not then the rect has 0 size.</param>
        void Cull(Rect clipRect, bool validRect);

        /// <param name="value">The Rectangle for the clipping</param>
        /// <param name="validRect">Is the rect valid.</param>
        void SetClipRect(Rect value, bool validRect);

        /// <param name="clipSoftness">The number of pixels to apply the softness to </param>
        void SetClipSoftness(Vector2 clipSoftness);
    }
}
