using UnityEngine;
using UnityEngine.UI;


namespace TMPro
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class TMP_SelectionCaret : MaskableGraphic
    {
        /// <param name="clipRect"></param>
        /// <param name="validRect"></param>
        public override void Cull(Rect clipRect, bool validRect)
        {
            if (validRect)
            {
                canvasRenderer.cull = false;
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
                return;
            }

            base.Cull(clipRect, validRect);
        }

        protected override void UpdateGeometry()
        {
        }
    }
}
