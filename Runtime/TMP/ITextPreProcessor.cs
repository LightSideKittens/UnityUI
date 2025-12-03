

namespace TMPro
{
    public interface ITextPreprocessor
    {
        /// <param name="text">Source text to be processed</param>
        /// <returns>Processed text</returns>
        string PreprocessText(string text);
    }
}
