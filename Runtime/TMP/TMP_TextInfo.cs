using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace TMPro
{
    [Serializable]
    public class TMP_TextInfo
    {
        internal static Vector2 k_InfinityVectorPositive = new(32767, 32767);
        internal static Vector2 k_InfinityVectorNegative = new(-32767, -32767);

        public TMP_Text textComponent;

        public int characterCount;
        public int spriteCount;
        public int spaceCount;
        public int wordCount;
        public int linkCount;
        public int lineCount;

        public int materialCount;

        public TMP_CharacterInfo[] characterInfo;
        public TMP_WordInfo[] wordInfo;
        public TMP_LinkInfo[] linkInfo;
        public TMP_LineInfo[] lineInfo;
        public TMP_MeshInfo[] meshInfo;

        private TMP_MeshInfo[] m_CachedMeshInfo;

        public TMP_TextInfo()
        {
            characterInfo = new TMP_CharacterInfo[8];
            wordInfo = new TMP_WordInfo[16];
            linkInfo = Array.Empty<TMP_LinkInfo>();
            lineInfo = new TMP_LineInfo[2];

            meshInfo = new TMP_MeshInfo[1];
        }

        internal TMP_TextInfo(int characterCount)
        {
            characterInfo = new TMP_CharacterInfo[characterCount];
            wordInfo = new TMP_WordInfo[16];
            linkInfo = Array.Empty<TMP_LinkInfo>();
            lineInfo = new TMP_LineInfo[2];

            meshInfo = new TMP_MeshInfo[1];
        }

        public TMP_TextInfo(TMP_Text textComponent)
        {
            this.textComponent = textComponent;

            characterInfo = new TMP_CharacterInfo[8];

            wordInfo = new TMP_WordInfo[4];
            linkInfo = Array.Empty<TMP_LinkInfo>();

            lineInfo = new TMP_LineInfo[2];

            meshInfo = new TMP_MeshInfo[1];
            meshInfo[0].mesh = textComponent.mesh;
            materialCount = 1;
        }


        internal void Clear()
        {
            characterCount = 0;
            spaceCount = 0;
            wordCount = 0;
            linkCount = 0;
            lineCount = 0;
            spriteCount = 0;

            for (int i = 0; i < meshInfo.Length; i++)
            {
                meshInfo[i].vertexCount = 0;
            }
        }


        internal void ClearAllData()
        {
            characterCount = 0;
            spaceCount = 0;
            wordCount = 0;
            linkCount = 0;
            lineCount = 0;
            spriteCount = 0;

            characterInfo = new TMP_CharacterInfo[4];
            wordInfo = new TMP_WordInfo[1];
            lineInfo = new TMP_LineInfo[1];
            linkInfo = Array.Empty<TMP_LinkInfo>();

            materialCount = 0;

            meshInfo = new TMP_MeshInfo[1];
        }


        public void ClearMeshInfo(bool updateMesh)
        {
            for (int i = 0; i < meshInfo.Length; i++)
                meshInfo[i].Clear(updateMesh);
        }


        public void ClearAllMeshInfo()
        {
            for (int i = 0; i < meshInfo.Length; i++)
                meshInfo[i].Clear(true);
        }


        public void ResetVertexLayout(bool isVolumetric)
        {
            for (int i = 0; i < meshInfo.Length; i++)
                meshInfo[i].ResizeMeshInfo(0, isVolumetric);
        }


        /// <param name="materials"></param>
        public void ClearUnusedVertices(MaterialReference[] materials)
        {
            for (int i = 0; i < meshInfo.Length; i++)
            {
                int start = 0;
                meshInfo[i].ClearUnusedVertices(start);
            }
        }


        internal void ClearLineInfo()
        {
            if (lineInfo == null)
                lineInfo = new TMP_LineInfo[1];

            int length = lineInfo.Length;

            for (int i = 0; i < length; i++)
            {
                lineInfo[i].characterCount = 0;
                lineInfo[i].spaceCount = 0;
                lineInfo[i].wordCount = 0;
                lineInfo[i].controlCharacterCount = 0;

                lineInfo[i].visibleCharacterCount = 0;
                lineInfo[i].visibleSpaceCount = 0;

                lineInfo[i].ascender = k_InfinityVectorNegative.x;
                lineInfo[i].baseline = 0;
                lineInfo[i].descender = k_InfinityVectorPositive.x;
                lineInfo[i].maxAdvance = 0;

                lineInfo[i].marginLeft = 0;
                lineInfo[i].marginRight = 0;

                lineInfo[i].lineExtents.min = k_InfinityVectorPositive;
                lineInfo[i].lineExtents.max = k_InfinityVectorNegative;
                lineInfo[i].width = 0;
            }
        }

        /// <returns>A copy of the MeshInfo[]</returns>
        public TMP_MeshInfo[] CopyMeshInfoVertexData()
        {
            if (m_CachedMeshInfo == null || m_CachedMeshInfo.Length != meshInfo.Length)
            {
                m_CachedMeshInfo = new TMP_MeshInfo[meshInfo.Length];

                for (int i = 0; i < m_CachedMeshInfo.Length; i++)
                {
                    int length = meshInfo[i].vertices.Length;

                    m_CachedMeshInfo[i].vertices = new Vector3[length];
                    m_CachedMeshInfo[i].uvs0 = new Vector4[length];
                    m_CachedMeshInfo[i].uvs2 = new Vector2[length];
                    m_CachedMeshInfo[i].colors32 = new Color32[length];
                }
            }

            for (int i = 0; i < m_CachedMeshInfo.Length; i++)
            {
                int length = meshInfo[i].vertices.Length;

                if (m_CachedMeshInfo[i].vertices.Length != length)
                {
                    m_CachedMeshInfo[i].vertices = new Vector3[length];
                    m_CachedMeshInfo[i].uvs0 = new Vector4[length];
                    m_CachedMeshInfo[i].uvs2 = new Vector2[length];
                    m_CachedMeshInfo[i].colors32 = new Color32[length];
                }


                Array.Copy(meshInfo[i].vertices, m_CachedMeshInfo[i].vertices, length);
                Array.Copy(meshInfo[i].uvs0, m_CachedMeshInfo[i].uvs0, length);
                Array.Copy(meshInfo[i].uvs2, m_CachedMeshInfo[i].uvs2, length);
                Array.Copy(meshInfo[i].colors32, m_CachedMeshInfo[i].colors32, length);
            }

            return m_CachedMeshInfo;
        }



        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        public static void Resize<T> (ref T[] array, int size)
        {
            int newSize = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size);

            Array.Resize(ref array, newSize);
        }


        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <param name="isFixedSize"></param>
        public static void Resize<T>(ref T[] array, int size, bool isBlockAllocated)
        {
            if (isBlockAllocated) size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size);

            if (size == array.Length) return;

            Array.Resize(ref array, size);
        }

    }
}
