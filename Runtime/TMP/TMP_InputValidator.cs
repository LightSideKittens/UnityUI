using UnityEngine;
using System.Collections;


namespace TMPro
{
    [System.Serializable]
    public abstract class TMP_InputValidator : ScriptableObject
    {
        /// <param name="text">The original text</param>
        /// <param name="pos">The position in the string to add the caharcter</param>
        /// <param name="ch">The character to add</param>
        /// <returns>The character added</returns>
        public abstract char Validate(ref string text, ref int pos, char ch);
    }
}
