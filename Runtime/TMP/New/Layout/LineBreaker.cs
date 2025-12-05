using System;

namespace Tekst
{
    /// <summary>
    /// Line Breaker — разбивает текст на строки.
    /// Реализует UAX #14 (Line Breaking Algorithm) + word wrapping.
    /// Использует IUnicodeDataProvider из Unicode модуля.
    /// </summary>
    public sealed class LineBreaker : ILineBreaker
    {
        private readonly IUnicodeDataProvider dataProvider;
        
        // Буферы
        private TextLine[] lines = new TextLine[16];
        private int lineCount;
        private ShapedRun[] orderedRuns = new ShapedRun[32];
        private int orderedRunCount;
        
        // Temporary: break opportunities
        private BreakOpportunity[] breaks = new BreakOpportunity[256];
        
        public LineBreaker(IUnicodeDataProvider dataProvider)
        {
            this.dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }
        
        public void BreakLines(
            ReadOnlySpan<int> codepoints,
            ReadOnlySpan<ShapedRun> runs,
            ReadOnlySpan<ShapedGlyph> glyphs,
            float maxWidth,
            ILineBreakResult result)
        {
            lineCount = 0;
            orderedRunCount = 0;
            
            if (runs.IsEmpty)
            {
                WriteResult(result);
                return;
            }
            
            // Шаг 1: Определяем break opportunities (UAX #14)
            FindBreakOpportunities(codepoints);
            
            // Шаг 2: Разбиваем на строки по ширине
            WrapLines(codepoints, runs, glyphs, maxWidth);
            
            // Шаг 3: BiDi reorder runs внутри каждой строки
            ReorderRunsPerLine(runs);
            
            WriteResult(result);
        }
        
        /// <summary>
        /// Найти break opportunities по UAX #14
        /// </summary>
        private void FindBreakOpportunities(ReadOnlySpan<int> codepoints)
        {
            EnsureBreakCapacity(codepoints.Length);
            
            for (int i = 0; i < codepoints.Length; i++)
            {
                var breakClass = dataProvider.GetLineBreakClass(codepoints[i]);
                breaks[i] = ClassifyBreakOpportunity(breakClass, i, codepoints);
            }
        }
        
        /// <summary>
        /// Классификация break opportunity на основе Line Break Class
        /// </summary>
        private BreakOpportunity ClassifyBreakOpportunity(LineBreakClass cls, int position, ReadOnlySpan<int> codepoints)
        {
            // Упрощённая логика, полная реализация требует pair table
            switch (cls)
            {
                case LineBreakClass.BK:  // Mandatory Break
                case LineBreakClass.CR:  // Carriage Return
                case LineBreakClass.LF:  // Line Feed
                case LineBreakClass.NL:  // Next Line
                    return BreakOpportunity.Mandatory;
                    
                case LineBreakClass.SP:  // Space
                case LineBreakClass.ZW:  // Zero Width Space
                    return BreakOpportunity.Allowed;
                
                case LineBreakClass.BA:  // Break After
                case LineBreakClass.HY:  // Hyphen
                    return BreakOpportunity.Allowed;
                
                case LineBreakClass.GL:  // Non-breaking (Glue)
                case LineBreakClass.WJ:  // Word Joiner
                    return BreakOpportunity.Prohibited;
                    
                default:
                    // TODO: Полная реализация pair table из UAX #14
                    return BreakOpportunity.Prohibited;
            }
        }
        
        /// <summary>
        /// Word wrapping
        /// </summary>
        private void WrapLines(
            ReadOnlySpan<int> codepoints,
            ReadOnlySpan<ShapedRun> runs,
            ReadOnlySpan<ShapedGlyph> glyphs,
            float maxWidth)
        {
            int lineStart = 0;
            int runStartIndex = 0;
            float currentWidth = 0;
            int lastBreakPoint = -1;
            float widthAtLastBreak = 0;
            int runCountAtLastBreak = 0;
            
            for (int i = 0; i < runs.Length; i++)
            {
                var run = runs[i];
                
                // Накапливаем ширину
                currentWidth += run.width;
                
                // Проверяем break opportunity в конце run
                int runEnd = run.range.End - 1;
                if (runEnd >= 0 && runEnd < breaks.Length && breaks[runEnd] != BreakOpportunity.Prohibited)
                {
                    lastBreakPoint = i;
                    widthAtLastBreak = currentWidth;
                    runCountAtLastBreak = i - runStartIndex + 1;
                }
                
                // Превысили ширину?
                if (currentWidth > maxWidth && lastBreakPoint >= 0)
                {
                    // Создаём строку до последней break opportunity
                    CreateLine(runs, runStartIndex, runCountAtLastBreak, codepoints, glyphs);
                    
                    // Начинаем новую строку
                    runStartIndex = lastBreakPoint + 1;
                    currentWidth = currentWidth - widthAtLastBreak;
                    lastBreakPoint = -1;
                }
                
                // Mandatory break?
                if (runEnd >= 0 && runEnd < breaks.Length && breaks[runEnd] == BreakOpportunity.Mandatory)
                {
                    CreateLine(runs, runStartIndex, i - runStartIndex + 1, codepoints, glyphs);
                    runStartIndex = i + 1;
                    currentWidth = 0;
                    lastBreakPoint = -1;
                }
            }
            
            // Последняя строка
            if (runStartIndex < runs.Length)
            {
                CreateLine(runs, runStartIndex, runs.Length - runStartIndex, codepoints, glyphs);
            }
        }
        
        private void CreateLine(
            ReadOnlySpan<ShapedRun> runs,
            int startRun,
            int runCount,
            ReadOnlySpan<int> codepoints,
            ReadOnlySpan<ShapedGlyph> glyphs)
        {
            if (runCount <= 0) return;
            
            var firstRun = runs[startRun];
            var lastRun = runs[startRun + runCount - 1];
            
            float width = 0;
            for (int i = 0; i < runCount; i++)
            {
                width += runs[startRun + i].width;
            }
            
            EnsureLineCapacity(lineCount + 1);
            lines[lineCount++] = new TextLine
            {
                range = new TextRange(firstRun.range.start, lastRun.range.End - firstRun.range.start),
                runStart = orderedRunCount,
                runCount = runCount,
                width = width,
                height = 0, // TODO: вычислить из метрик шрифта
                baseline = 0
            };
            
            // Копируем runs (пока без reorder)
            for (int i = 0; i < runCount; i++)
            {
                EnsureOrderedRunCapacity(orderedRunCount + 1);
                orderedRuns[orderedRunCount++] = runs[startRun + i];
            }
        }
        
        /// <summary>
        /// BiDi reorder runs внутри строки
        /// </summary>
        private void ReorderRunsPerLine(ReadOnlySpan<ShapedRun> originalRuns)
        {
            for (int i = 0; i < lineCount; i++)
            {
                var line = lines[i];
                ReorderRunsInLine(line.runStart, line.runCount);
            }
        }
        
        /// <summary>
        /// Reorder runs по BiDi levels (правило L2)
        /// </summary>
        private void ReorderRunsInLine(int start, int count)
        {
            if (count <= 1) return;
            
            // Находим max level
            byte maxLevel = 0;
            for (int i = 0; i < count; i++)
            {
                var level = GetRunLevel(orderedRuns[start + i]);
                if (level > maxLevel) maxLevel = level;
            }
            
            // Reverse subsequences с level >= currentLevel, от maxLevel до 1
            for (byte level = maxLevel; level >= 1; level--)
            {
                int runStart = -1;
                
                for (int i = 0; i <= count; i++)
                {
                    bool inSequence = i < count && GetRunLevel(orderedRuns[start + i]) >= level;
                    
                    if (inSequence && runStart < 0)
                    {
                        runStart = i;
                    }
                    else if (!inSequence && runStart >= 0)
                    {
                        // Reverse [runStart, i)
                        ReverseRuns(start + runStart, i - runStart);
                        runStart = -1;
                    }
                }
            }
        }
        
        private byte GetRunLevel(ShapedRun run)
        {
            return run.direction == TextDirection.RightToLeft ? (byte)1 : (byte)0;
        }
        
        private void ReverseRuns(int start, int count)
        {
            int end = start + count - 1;
            while (start < end)
            {
                var temp = orderedRuns[start];
                orderedRuns[start] = orderedRuns[end];
                orderedRuns[end] = temp;
                start++;
                end--;
            }
        }
        
        private void WriteResult(ILineBreakResult result)
        {
            if (result is LineBreakResultBuffer buffer)
            {
                buffer.Set(lines.AsSpan(0, lineCount), orderedRuns.AsSpan(0, orderedRunCount));
            }
        }
        
        private void EnsureBreakCapacity(int required)
        {
            if (breaks.Length >= required) return;
            Array.Resize(ref breaks, Math.Max(required, breaks.Length * 2));
        }
        
        private void EnsureLineCapacity(int required)
        {
            if (lines.Length >= required) return;
            Array.Resize(ref lines, Math.Max(required, lines.Length * 2));
        }
        
        private void EnsureOrderedRunCapacity(int required)
        {
            if (orderedRuns.Length >= required) return;
            Array.Resize(ref orderedRuns, Math.Max(required, orderedRuns.Length * 2));
        }
    }
    
    /// <summary>
    /// Break opportunity type
    /// </summary>
    public enum BreakOpportunity : byte
    {
        Prohibited,     // Нельзя разрывать
        Allowed,        // Можно разрывать
        Mandatory       // Обязательный разрыв
    }
    
    // IUnicodeDataProvider из Unicode модуля предоставляет GetLineBreakClass()
    
    /// <summary>
    /// Буфер результатов Line Breaker
    /// </summary>
    public sealed class LineBreakResultBuffer : ILineBreakResult
    {
        private TextLine[] lines = new TextLine[16];
        private int lineCount;
        private ShapedRun[] orderedRuns = new ShapedRun[32];
        private int runCount;
        
        public ReadOnlySpan<TextLine> Lines => lines.AsSpan(0, lineCount);
        public ReadOnlySpan<ShapedRun> OrderedRuns => orderedRuns.AsSpan(0, runCount);
        
        internal void Set(ReadOnlySpan<TextLine> sourceLines, ReadOnlySpan<ShapedRun> sourceRuns)
        {
            if (lines.Length < sourceLines.Length)
                lines = new TextLine[sourceLines.Length];
            sourceLines.CopyTo(lines);
            lineCount = sourceLines.Length;
            
            if (orderedRuns.Length < sourceRuns.Length)
                orderedRuns = new ShapedRun[sourceRuns.Length];
            sourceRuns.CopyTo(orderedRuns);
            runCount = sourceRuns.Length;
        }
        
        public void Clear()
        {
            lineCount = 0;
            runCount = 0;
        }
    }
}