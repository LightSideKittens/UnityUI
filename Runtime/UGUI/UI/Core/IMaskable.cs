using System;

namespace UnityEngine.UI
{
    public interface IMaskable
    {
        /// <remarks>
        /// Use this to update the internal state (recreate materials etc).
        /// </remarks>
        void RecalculateMasking();
    }
}
