using System;

namespace UnityEngine.UI
{
    public interface IMeshModifier
    {
        void ModifyMesh(VertexHelper verts);
    }
}