using UnityEngine;
using System.Collections;


namespace TMPro
{
    public enum CaretPosition { None, Left, Right }

    public struct CaretInfo
    {
        public int index;
        public CaretPosition position;

        public CaretInfo(int index, CaretPosition position)
        {
            this.index = index;
            this.position = position;
        }
    }

    public static class TMP_TextUtilities
    {
        private static Vector3[] m_rectWorldCorners = new Vector3[4];


        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>


        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static int GetCursorIndexFromPosition(TMPText textComponent, Vector3 position, Camera camera)
        {
            int index = FindNearestCharacter(textComponent, position, camera, false);

            RectTransform rectTransform = textComponent.rectTransform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            TMP_CharacterInfo cInfo = textComponent.textInfo.characterInfo[index];

            Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
            Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);

            float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

            if (insertPosition < 0.5f)
                return index;
            else
                return index + 1;

        }


        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="cursor">The position of the cursor insertion position relative to the position.</param>
        /// <returns></returns>


        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="cursor">The position of the cursor insertion position relative to the position.</param>
        /// <returns></returns>
        public static int GetCursorIndexFromPosition(TMPText textComponent, Vector3 position, Camera camera, out CaretPosition cursor)
        {
            int line = FindNearestLine(textComponent, position, camera);

            if (line == -1)
            {
                cursor = CaretPosition.Left;
                return 0;
            }

            int index = FindNearestCharacterOnLine(textComponent, position, line, camera, false);

            if (textComponent.textInfo.lineInfo[line].characterCount == 1)
            {
                cursor = CaretPosition.Left;
                return index;
            }

            RectTransform rectTransform = textComponent.rectTransform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            TMP_CharacterInfo cInfo = textComponent.textInfo.characterInfo[index];

            Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
            Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);

            float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

            if (insertPosition < 0.5f)
            {
                cursor = CaretPosition.Left;
                return index;
            }
            else
            {
                cursor = CaretPosition.Right;
                return index;
            }
        }


        /// <param name="textComponent"></param>
        /// <param name="position"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static int FindNearestLine(TMPText text, Vector3 position, Camera camera)
        {
            RectTransform rectTransform = text.rectTransform;

            float distance = Mathf.Infinity;
            int closest = -1;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.lineCount; i++)
            {
                TMP_LineInfo lineInfo = text.textInfo.lineInfo[i];

                float ascender = rectTransform.TransformPoint(new(0, lineInfo.ascender, 0)).y;
                float descender = rectTransform.TransformPoint(new(0, lineInfo.descender, 0)).y;

                if (ascender > position.y && descender < position.y)
                {
                    return i;
                }

                float d0 = Mathf.Abs(ascender - position.y);
                float d1 = Mathf.Abs(descender - position.y);

                float d = Mathf.Min(d0, d1);
                if (d < distance)
                {
                    distance = d;
                    closest = i;
                }
            }

            return closest;
        }


        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="line"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static int FindNearestCharacterOnLine(TMPText text, Vector3 position, int line, Camera camera, bool visibleOnly)
        {
            RectTransform rectTransform = text.rectTransform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            int firstCharacter = text.textInfo.lineInfo[line].firstCharacterIndex;
            int lastCharacter = text.textInfo.lineInfo[line].lastCharacterIndex;

            float distanceSqr = Mathf.Infinity;
            int closest = lastCharacter;

            for (int i = firstCharacter; i < lastCharacter; i++)
            {
                TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                if (cInfo.character == '\r')
                    continue;

                Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
                Vector3 tl = rectTransform.TransformPoint(new(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
                Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
                Vector3 br = rectTransform.TransformPoint(new(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                {
                    closest = i;
                    break;
                }

                float dbl = DistanceToLine(bl, tl, position);
                float dtl = DistanceToLine(tl, tr, position);
                float dtr = DistanceToLine(tr, br, position);
                float dbr = DistanceToLine(br, bl, position);

                float d = dbl < dtl ? dbl : dtl;
                d = d < dtr ? d : dtr;
                d = d < dbr ? d : dbr;

                if (distanceSqr > d)
                {
                    distanceSqr = d;
                    closest = i;
                }
            }
            return closest;
        }


        /// <param name="rectTransform">A reference to the RectTranform of the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static bool IsIntersectingRectTransform(RectTransform rectTransform, Vector3 position, Camera camera)
        {
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            rectTransform.GetWorldCorners(m_rectWorldCorners);

            if (PointIntersectRectangle(position, m_rectWorldCorners[0], m_rectWorldCorners[1], m_rectWorldCorners[2], m_rectWorldCorners[3]))
            {
                return true;
            }

            return false;
        }


        /// <param name="text">A reference to the TextMeshPro component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which is rendering the text or whichever one might be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>
        public static int FindIntersectingCharacter(TMPText text, Vector3 position, Camera camera, bool visibleOnly)
        {
            RectTransform rectTransform = text.rectTransform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
                Vector3 tl = rectTransform.TransformPoint(new(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
                Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
                Vector3 br = rectTransform.TransformPoint(new(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                    return i;

            }
            return -1;
        }


        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TMP Text component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>
        public static int FindNearestCharacter(TMPText text, Vector3 position, Camera camera, bool visibleOnly)
        {
            RectTransform rectTransform = text.rectTransform;

            float distanceSqr = Mathf.Infinity;
            int closest = 0;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
                Vector3 tl = rectTransform.TransformPoint(new(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
                Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
                Vector3 br = rectTransform.TransformPoint(new(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                    return i;

                float dbl = DistanceToLine(bl, tl, position);
                float dtl = DistanceToLine(tl, tr, position);
                float dtr = DistanceToLine(tr, br, position);
                float dbr = DistanceToLine(br, bl, position);

                float d = dbl < dtl ? dbl : dtl;
                d = d < dtr ? d : dtr;
                d = d < dbr ? d : dbr;

                if (distanceSqr > d)
                {
                    distanceSqr = d;
                    closest = i;
                }
            }

            return closest;
        }


        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TextMeshPro component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TMP_Text component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static int FindIntersectingWord(TMPText text, Vector3 position, Camera camera)
        {
            RectTransform rectTransform = text.rectTransform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.wordCount; i++)
            {
                TMP_WordInfo wInfo = text.textInfo.wordInfo[i];

                bool isBeginRegion = false;

                Vector3 bl = Vector3.zero;
                Vector3 tl = Vector3.zero;
                Vector3 br = Vector3.zero;
                Vector3 tr = Vector3.zero;

                float maxAscender = -Mathf.Infinity;
                float minDescender = Mathf.Infinity;

                for (int j = 0; j < wInfo.characterCount; j++)
                {
                    int characterIndex = wInfo.firstCharacterIndex + j;
                    TMP_CharacterInfo currentCharInfo = text.textInfo.characterInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    bool isCharacterVisible = currentCharInfo.isVisible;

                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    if (!isBeginRegion && isCharacterVisible)
                    {
                        isBeginRegion = true;

                        bl = new(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        tl = new(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        if (wInfo.characterCount == 1)
                        {
                            isBeginRegion = false;

                            br = new(currentCharInfo.topRight.x, currentCharInfo.descender, 0);
                            tr = new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

                            bl = rectTransform.TransformPoint(new(bl.x, minDescender, 0));
                            tl = rectTransform.TransformPoint(new(tl.x, maxAscender, 0));
                            tr = rectTransform.TransformPoint(new(tr.x, maxAscender, 0));
                            br = rectTransform.TransformPoint(new(br.x, minDescender, 0));

                            if (PointIntersectRectangle(position, bl, tl, tr, br))
                                return i;
                        }
                    }

                    if (isBeginRegion && j == wInfo.characterCount - 1)
                    {
                        isBeginRegion = false;

                        br = new(currentCharInfo.topRight.x, currentCharInfo.descender, 0);
                        tr = new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

                        bl = rectTransform.TransformPoint(new(bl.x, minDescender, 0));
                        tl = rectTransform.TransformPoint(new(tl.x, maxAscender, 0));
                        tr = rectTransform.TransformPoint(new(tr.x, maxAscender, 0));
                        br = rectTransform.TransformPoint(new(br.x, minDescender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;
                    }
                    else if (isBeginRegion && currentLine != text.textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        br = new(currentCharInfo.topRight.x, currentCharInfo.descender, 0);
                        tr = new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0);

                        bl = rectTransform.TransformPoint(new(bl.x, minDescender, 0));
                        tl = rectTransform.TransformPoint(new(tl.x, maxAscender, 0));
                        tr = rectTransform.TransformPoint(new(tr.x, maxAscender, 0));
                        br = rectTransform.TransformPoint(new(br.x, minDescender, 0));

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;
                    }
                }
            }

            return -1;
        }


        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TextMeshPro component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TMP_Text component.</param>
        /// <param name="position"></param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static int FindNearestWord(TMPText text, Vector3 position, Camera camera)
        {
            RectTransform rectTransform = text.rectTransform;

            float distanceSqr = Mathf.Infinity;
            int closest = 0;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.wordCount; i++)
            {
                TMP_WordInfo wInfo = text.textInfo.wordInfo[i];

                bool isBeginRegion = false;

                Vector3 bl = Vector3.zero;
                Vector3 tl = Vector3.zero;
                Vector3 br = Vector3.zero;
                Vector3 tr = Vector3.zero;

                for (int j = 0; j < wInfo.characterCount; j++)
                {
                    int characterIndex = wInfo.firstCharacterIndex + j;
                    TMP_CharacterInfo currentCharInfo = text.textInfo.characterInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    bool isCharacterVisible = currentCharInfo.isVisible;

                    if (!isBeginRegion && isCharacterVisible)
                    {
                        isBeginRegion = true;

                        bl = rectTransform.TransformPoint(new(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0));
                        tl = rectTransform.TransformPoint(new(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0));

                        if (wInfo.characterCount == 1)
                        {
                            isBeginRegion = false;

                            br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                            tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                            if (PointIntersectRectangle(position, bl, tl, tr, br))
                                return i;

                            float dbl = DistanceToLine(bl, tl, position);
                            float dtl = DistanceToLine(tl, tr, position);
                            float dtr = DistanceToLine(tr, br, position);
                            float dbr = DistanceToLine(br, bl, position);

                            float d = dbl < dtl ? dbl : dtl;
                            d = d < dtr ? d : dtr;
                            d = d < dbr ? d : dbr;

                            if (distanceSqr > d)
                            {
                                distanceSqr = d;
                                closest = i;
                            }
                        }
                    }

                    if (isBeginRegion && j == wInfo.characterCount - 1)
                    {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        float dbl = DistanceToLine(bl, tl, position);
                        float dtl = DistanceToLine(tl, tr, position);
                        float dtr = DistanceToLine(tr, br, position);
                        float dbr = DistanceToLine(br, bl, position);

                        float d = dbl < dtl ? dbl : dtl;
                        d = d < dtr ? d : dtr;
                        d = d < dbr ? d : dbr;

                        if (distanceSqr > d)
                        {
                            distanceSqr = d;
                            closest = i;
                        }
                    }
                    else if (isBeginRegion && currentLine != text.textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        float dbl = DistanceToLine(bl, tl, position);
                        float dtl = DistanceToLine(tl, tr, position);
                        float dtr = DistanceToLine(tr, br, position);
                        float dbr = DistanceToLine(br, bl, position);

                        float d = dbl < dtl ? dbl : dtl;
                        d = d < dtr ? d : dtr;
                        d = d < dbr ? d : dbr;

                        if (distanceSqr > d)
                        {
                            distanceSqr = d;
                            closest = i;
                        }
                    }
                }
            }

            return closest;
        }

        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position"></param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <returns></returns>


        /// <param name="textComponent"></param>
        /// <param name="position"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static int FindIntersectingLine(TMPText text, Vector3 position, Camera camera)
        {
            RectTransform rectTransform = text.rectTransform;

            int closest = -1;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.lineCount; i++)
            {
                TMP_LineInfo lineInfo = text.textInfo.lineInfo[i];

                float ascender = rectTransform.TransformPoint(new(0, lineInfo.ascender, 0)).y;
                float descender = rectTransform.TransformPoint(new(0, lineInfo.descender, 0)).y;

                if (ascender > position.y && descender < position.y)
                {
                    return i;
                }
            }

            return closest;
        }


        /// <param name="text">A reference to the TMP_Text component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static int FindIntersectingLink(TMPText text, Vector3 position, Camera camera)
        {
            Transform rectTransform = text.transform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.linkCount; i++)
            {
                TMP_LinkInfo linkInfo = text.textInfo.linkInfo[i];

                bool isBeginRegion = false;

                Vector3 bl = Vector3.zero;
                Vector3 tl = Vector3.zero;
                Vector3 br = Vector3.zero;
                Vector3 tr = Vector3.zero;

                for (int j = 0; j < linkInfo.linkTextLength; j++)
                {
                    int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    TMP_CharacterInfo currentCharInfo = text.textInfo.characterInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    if (!isBeginRegion)
                    {
                        isBeginRegion = true;

                        bl = rectTransform.TransformPoint(new(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0));
                        tl = rectTransform.TransformPoint(new(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0));

                        if (linkInfo.linkTextLength == 1)
                        {
                            isBeginRegion = false;

                            br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                            tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                            if (PointIntersectRectangle(position, bl, tl, tr, br))
                                return i;
                        }
                    }

                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;
                    }
                    else if (isBeginRegion && currentLine != text.textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;
                    }
                }
            }

            return -1;
        }

        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TextMeshPro component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TMP_Text component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static int FindNearestLink(TMPText text, Vector3 position, Camera camera)
        {
            RectTransform rectTransform = text.rectTransform;

            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            float distanceSqr = Mathf.Infinity;
            int closest = 0;

            for (int i = 0; i < text.textInfo.linkCount; i++)
            {
                TMP_LinkInfo linkInfo = text.textInfo.linkInfo[i];

                bool isBeginRegion = false;

                Vector3 bl = Vector3.zero;
                Vector3 tl = Vector3.zero;
                Vector3 br = Vector3.zero;
                Vector3 tr = Vector3.zero;

                for (int j = 0; j < linkInfo.linkTextLength; j++)
                {
                    int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    TMP_CharacterInfo currentCharInfo = text.textInfo.characterInfo[characterIndex];
                    int currentLine = currentCharInfo.lineNumber;

                    if (!isBeginRegion)
                    {
                        isBeginRegion = true;

                        bl = rectTransform.TransformPoint(new(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0));
                        tl = rectTransform.TransformPoint(new(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0));

                        if (linkInfo.linkTextLength == 1)
                        {
                            isBeginRegion = false;

                            br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                            tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                            if (PointIntersectRectangle(position, bl, tl, tr, br))
                                return i;

                            float dbl = DistanceToLine(bl, tl, position);
                            float dtl = DistanceToLine(tl, tr, position);
                            float dtr = DistanceToLine(tr, br, position);
                            float dbr = DistanceToLine(br, bl, position);

                            float d = dbl < dtl ? dbl : dtl;
                            d = d < dtr ? d : dtr;
                            d = d < dbr ? d : dbr;

                            if (distanceSqr > d)
                            {
                                distanceSqr = d;
                                closest = i;
                            }

                        }
                    }

                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        float dbl = DistanceToLine(bl, tl, position);
                        float dtl = DistanceToLine(tl, tr, position);
                        float dtr = DistanceToLine(tr, br, position);
                        float dbr = DistanceToLine(br, bl, position);

                        float d = dbl < dtl ? dbl : dtl;
                        d = d < dtr ? d : dtr;
                        d = d < dbr ? d : dbr;

                        if (distanceSqr > d)
                        {
                            distanceSqr = d;
                            closest = i;
                        }

                    }
                    else if (isBeginRegion && currentLine != text.textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new(currentCharInfo.topRight.x, currentCharInfo.ascender, 0));

                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        float dbl = DistanceToLine(bl, tl, position);
                        float dtl = DistanceToLine(tl, tr, position);
                        float dtr = DistanceToLine(tr, br, position);
                        float dbr = DistanceToLine(br, bl, position);

                        float d = dbl < dtl ? dbl : dtl;
                        d = d < dtr ? d : dtr;
                        d = d < dbr ? d : dbr;

                        if (distanceSqr > d)
                        {
                            distanceSqr = d;
                            closest = i;
                        }
                    }
                }
            }

            return closest;
        }


        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>


        /// <param name="text">A reference to the TextMeshPro component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <returns></returns>


        /// <param name="m"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private static bool PointIntersectRectangle(Vector3 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 normal = Vector3.Cross(b - a, d - a);
            if (normal == Vector3.zero)
                return false;

            Vector3 ab = b - a;
            Vector3 am = m - a;
            Vector3 bc = c - b;
            Vector3 bm = m - b;

            float abamDot = Vector3.Dot(ab, am);
            float bcbmDot = Vector3.Dot(bc, bm);

            return 0 <= abamDot && abamDot <= Vector3.Dot(ab, ab) && 0 <= bcbmDot && bcbmDot <= Vector3.Dot(bc, bc);
        }


        /// <param name="transform"></param>
        /// <param name="screenPoint"></param>
        /// <param name="cam"></param>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        public static bool ScreenPointToWorldPointInRectangle(Transform transform, Vector2 screenPoint, Camera cam, out Vector3 worldPoint)
        {
            worldPoint = (Vector3)Vector2.zero;
            Ray ray = RectTransformUtility.ScreenPointToRay(cam, screenPoint);

            if (!new Plane(transform.rotation * Vector3.back, transform.position).Raycast(ray, out var enter))
                return false;

            worldPoint = ray.GetPoint(enter);

            return true;
        }


        private struct LineSegment
        {
            public Vector3 Point1;
            public Vector3 Point2;

            public LineSegment(Vector3 p1, Vector3 p2)
            {
                Point1 = p1;
                Point2 = p2;
            }
        }


        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <param name="normal"></param>
        /// <param name="intersectingPoint"></param>
        /// <returns></returns>
        private static bool IntersectLinePlane(LineSegment line, Vector3 point, Vector3 normal, out Vector3 intersectingPoint)
        {
            intersectingPoint = Vector3.zero;
            Vector3 u = line.Point2 - line.Point1;
            Vector3 w = line.Point1 - point;

            float D = Vector3.Dot(normal, u);
            float N = -Vector3.Dot(normal, w);

            if (Mathf.Abs(D) < Mathf.Epsilon)
            {
                if (N == 0)
                    return true;
                else
                    return false;
            }

            float sI = N / D;

            if (sI < 0 || sI > 1) return false;

            intersectingPoint = line.Point1 + sI * u;

            return true;
        }


        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static float DistanceToLine(Vector3 a, Vector3 b, Vector3 point)
        {
            if (a == b)
            {
                Vector3 diff = point - a;
                return Vector3.Dot(diff, diff);
            }

            Vector3 n = b - a;
            Vector3 pa = a - point;

            float c = Vector3.Dot( n, pa );

            if ( c > 0.0f )
                return Vector3.Dot( pa, pa );

            Vector3 bp = point - b;

            if (Vector3.Dot( n, bp ) > 0.0f )
                return Vector3.Dot( bp, bp );

            Vector3 e = pa - n * (c / Vector3.Dot( n, n ));

            return Vector3.Dot( e, e );
        }


        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <param name="direction">-1 left, 0 in between, 1 right</param>
        /// <returns></returns>


        private const string k_lookupStringL = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

        private const string k_lookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";


        public static char ToLowerFast(char c)
        {
            if (c > k_lookupStringL.Length - 1)
                return c;

            return k_lookupStringL[c];
        }

        public static char ToUpperFast(char c)
        {
            if (c > k_lookupStringU.Length - 1)
                return c;

            return k_lookupStringU[c];
        }

        internal static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_lookupStringU.Length - 1)
                return c;

            return k_lookupStringU[(int)c];
        }

        /// <param name="s"></param>
        /// <returns></returns>
        public static int GetHashCode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ ToUpperFast(s[i]);

            return hashCode;
        }

        /// <returns></returns>
        public static int GetSimpleHashCode(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ s[i];

            return hashCode;
        }

        /// <returns></returns>
        public static uint GetSimpleHashCodeLowercase(string s)
        {
            uint hashCode = 5381;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ ToLowerFast(s[i]);

            return hashCode;
        }

        /// <param name="s">The string from which to compute the hash code.</param>
        /// <returns>The computed hash code.</returns>
        public static uint GetHashCodeCaseInSensitive(string s)
        {
            uint hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ ToUpperFast(s[i]);

            return hashCode;
        }


        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }


        /// <param name="s"></param>
        /// <returns></returns>
        public static int StringHexToInt(string s)
        {
            int value = 0;

            for (int i = 0; i < s.Length; i++)
            {
                value += HexToInt(s[i]) * (int)Mathf.Pow(16, (s.Length - 1) - i);
            }

            return value;
        }
    }
}
