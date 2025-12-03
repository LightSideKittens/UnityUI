using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

static class NewBidi
{
    [StructLayout(LayoutKind.Sequential)]
    struct Run
    {
        public uint offset;
        public uint length;
        public byte level;
    }

    public enum Direction
    {
        Auto = 0,
        LeftToRight = 1,
        RightToLeft = 2
    }

    public readonly struct BidiRun
    {
        public readonly int Offset;
        public readonly int Length;
        public readonly byte Level;

        public bool IsRtl => (Level & 1) != 0;

        public BidiRun(int offset, int length, byte level)
        {
            Offset = offset;
            Length = length;
            Level = level;
        }
    }

    public sealed class Algorithm : IDisposable
    {
        public int[] Codepoints { get; }

        IntPtr handle;
        GCHandle pinnedHandle;
        bool disposed;

        Algorithm(int[] cps, IntPtr handle, GCHandle pinned)
        {
            Codepoints = cps;
            this.handle = handle;
            pinnedHandle = pinned;
        }

        public static Algorithm Create(int[] codepoints)
        {
            if (codepoints == null || codepoints.Length == 0)
                throw new ArgumentException("codepoints is null or empty");

            var pinned = GCHandle.Alloc(codepoints, GCHandleType.Pinned);
            IntPtr algHandle = sheenbidi_unity_create_algorithm_utf32(
                codepoints,
                codepoints.Length);

            if (algHandle == IntPtr.Zero)
            {
                pinned.Free();
                throw new InvalidOperationException("Failed to create SheenBidi algorithm");
            }

            return new Algorithm(codepoints, algHandle, pinned);
        }

        public Paragraph CreateParagraph(int start, int length, Direction dir)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Algorithm));

            if (dir == Direction.Auto)
                throw new ArgumentException("Direction.Auto must be resolved before CreateParagraph", nameof(dir));

            int baseDirCode = dir switch
            {
                Direction.RightToLeft => 2,
                _ => 1
            };

            IntPtr paraHandle = sheenbidi_unity_create_paragraph(
                handle,
                start,
                length,
                baseDirCode);

            if (paraHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create SheenBidi paragraph");

            return new Paragraph(this, paraHandle, start, length, dir);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (handle != IntPtr.Zero)
            {
                sheenbidi_unity_release_algorithm(handle);
                handle = IntPtr.Zero;
            }

            if (pinnedHandle.IsAllocated)
                pinnedHandle.Free();

            disposed = true;
        }

        internal IntPtr Handle
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(Algorithm));
                return handle;
            }
        }
    }

    public sealed class Paragraph : IDisposable
    {
        public Algorithm Algorithm { get; }

        public int Start { get; }
        public int Length { get; }
        public Direction BaseDirection { get; }

        IntPtr handle;
        bool disposed;

        internal Paragraph(
            Algorithm algorithm,
            IntPtr handle,
            int start,
            int length,
            Direction dir)
        {
            Algorithm = algorithm;
            this.handle = handle;
            Start = start;
            Length = length;
            BaseDirection = dir;
        }

        public Line CreateLine(int lineOffsetInParagraph, int lineLength)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Paragraph));

            IntPtr lineHandle = sheenbidi_unity_create_line(
                handle,
                lineOffsetInParagraph,
                lineLength);

            if (lineHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create SheenBidi line");

            return new Line(this, lineHandle, lineOffsetInParagraph, lineLength);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (handle != IntPtr.Zero)
            {
                sheenbidi_unity_release_paragraph(handle);
                handle = IntPtr.Zero;
            }

            disposed = true;
        }

        internal IntPtr Handle
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(Paragraph));
                return handle;
            }
        }
    }

    public sealed class Line : IDisposable
    {
        public Paragraph Paragraph { get; }

        public int OffsetInParagraph { get; }
        public int Length { get; }

        IntPtr handle;
        bool disposed;

        internal Line(
            Paragraph paragraph,
            IntPtr handle,
            int offsetInParagraph,
            int length)
        {
            Paragraph = paragraph;
            this.handle = handle;
            OffsetInParagraph = offsetInParagraph;
            Length = length;
        }

        public BidiRun[] GetRuns()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Line));

            int runCount = sheenbidi_unity_line_get_run_count(handle);
            if (runCount <= 0)
                return Array.Empty<BidiRun>();

            IntPtr runsPtr = sheenbidi_unity_line_get_runs(handle);
            if (runsPtr == IntPtr.Zero)
                return Array.Empty<BidiRun>();

            var result = new BidiRun[runCount];
            int structSize = Marshal.SizeOf<Run>();

            for (int i = 0; i < runCount; i++)
            {
                IntPtr runPtr = runsPtr + i * structSize;
                var nativeRun = Marshal.PtrToStructure<Run>(runPtr);
                result[i] = new BidiRun(
                    (int)nativeRun.offset,
                    (int)nativeRun.length,
                    nativeRun.level);
            }

            return result;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (handle != IntPtr.Zero)
            {
                sheenbidi_unity_release_line(handle);
                handle = IntPtr.Zero;
            }

            disposed = true;
        }

        internal IntPtr Handle
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(Line));
                return handle;
            }
        }
    }

    public static void ReorderLine(
        int[] paragraphCodepoints,
        int paragraphOffset,
        int lineLength,
        Direction baseDirection,
        out int[] visualCodepoints,
        out int[] logicalToVisual)
    {
        if (paragraphCodepoints == null)
            throw new ArgumentNullException(nameof(paragraphCodepoints));
        if (paragraphOffset < 0 || lineLength <= 0 || paragraphOffset + lineLength > paragraphCodepoints.Length)
            throw new ArgumentOutOfRangeException();
        if (baseDirection == Direction.Auto)
            throw new ArgumentException("Direction.Auto must be resolved before calling ReorderLine", nameof(baseDirection));

        visualCodepoints = new int[lineLength];
        logicalToVisual = new int[lineLength];

        for (int i = 0; i < lineLength; i++)
            logicalToVisual[i] = -1;

        using var algorithm = Algorithm.Create(paragraphCodepoints);
        using var paragraph = algorithm.CreateParagraph(0, paragraphCodepoints.Length, baseDirection);
        using var line = paragraph.CreateLine(paragraphOffset, lineLength);

        var runs = line.GetRuns();

        int visualIndex = 0;

        for (int r = 0; r < runs.Length; r++)
        {
            var run = runs[r];

            int runOffsetInPara = run.Offset;
            int runLength = run.Length;

            int runStartInLine = runOffsetInPara - paragraphOffset;
            if (runStartInLine < 0)
            {
                int delta = -runStartInLine;
                runLength -= delta;
                runStartInLine = 0;
            }

            if (runStartInLine + runLength > lineLength)
                runLength = lineLength - runStartInLine;

            if (runLength <= 0)
                continue;

            if (!run.IsRtl)
            {
                for (int i = 0; i < runLength; i++)
                {
                    int logicalIndex = runStartInLine + i;
                    logicalToVisual[logicalIndex] = visualIndex;
                    int paragraphIndex = paragraphOffset + logicalIndex;
                    visualCodepoints[visualIndex] = paragraphCodepoints[paragraphIndex];
                    visualIndex++;
                }
            }
            else
            {
                for (int i = 0; i < runLength; i++)
                {
                    int logicalIndex = runStartInLine + (runLength - 1 - i);
                    logicalToVisual[logicalIndex] = visualIndex;
                    int paragraphIndex = paragraphOffset + logicalIndex;
                    visualCodepoints[visualIndex] = paragraphCodepoints[paragraphIndex];
                    visualIndex++;
                }
            }
        }

        for (int i = 0; i < lineLength; i++)
        {
            if (logicalToVisual[i] < 0 || logicalToVisual[i] >= lineLength)
            {
                logicalToVisual[i] = i;
                visualCodepoints[i] = paragraphCodepoints[paragraphOffset + i];
            }
        }
    }
    
    
    public static Direction DetectDirection(int[] codepoints, Direction fallback = Direction.LeftToRight)
    {
        if (codepoints == null || codepoints.Length == 0)
            return fallback;

        int res = sheenbidi_unity_detect_base_direction_utf32(codepoints, codepoints.Length);
        return res == 1 ? Direction.RightToLeft : Direction.LeftToRight;
    }
    
    public static int[] StringToCodepoints(string s)
    {
        if (string.IsNullOrEmpty(s))
            return Array.Empty<int>();

        var list = new List<int>(s.Length);
        for (int i = 0; i < s.Length;)
        {
            int cp = char.ConvertToUtf32(s, i);
            list.Add(cp);
            i += char.IsSurrogatePair(s, i) ? 2 : 1;
        }

        return list.ToArray();
    }

    public static string CodepointsToString(int[] cps)
    {
        if (cps == null || cps.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(cps.Length);
        for (int i = 0; i < cps.Length; i++)
        {
            sb.Append(char.ConvertFromUtf32(cps[i]));
        }

        return sb.ToString();
    }
    
#if UNITY_IOS && !UNITY_EDITOR
    const string DllName = "__Internal";
#else
    const string DllName = "AddPlugin";
#endif

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sheenbidi_unity_create_algorithm_utf32([In] int[] logicalUtf32, int length);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void sheenbidi_unity_release_algorithm(IntPtr alg);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sheenbidi_unity_create_paragraph(IntPtr alg, int start, int length, int baseDirCode);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void sheenbidi_unity_release_paragraph(IntPtr para);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sheenbidi_unity_create_line(IntPtr para, int start, int length);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void sheenbidi_unity_release_line(IntPtr line);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sheenbidi_unity_line_get_run_count(IntPtr line);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sheenbidi_unity_line_get_runs(IntPtr line);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sheenbidi_unity_detect_base_direction_utf32([In] int[] logicalUtf32, int length);
}
