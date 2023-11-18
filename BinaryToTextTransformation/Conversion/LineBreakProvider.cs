using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace de.Aargenveldt.BinaryToTextTransformation.Conversion
{
    /// <summary>
    /// Provider for line break characters - selected by given mode.
    /// </summary>
    public static class LineBreakProvider
    {
        /// <summary>
        /// Map of common line break styles.
        /// </summary>
        private static readonly Dictionary<LineBreakMode, char[]> __commonLineBreaks = new Dictionary<LineBreakMode, char[]>()
        {
            { LineBreakMode.None, Array.Empty<char>() },
            { LineBreakMode.CR, new char[] {'\r' } },
            { LineBreakMode.NL, new char[] {'\n' } },
            { LineBreakMode.CRNL, new char[] {'\r', '\n' } },
            { LineBreakMode.NLCR, new char[] {'\n', '\r' } }
        };

        /// <summary>
        /// List of common line break characters
        /// </summary>
        private static readonly ImmutableArray<char> __commonLineBreakChars = ImmutableArray.Create<char>('\n', '\r');

        /// <summary>
        /// Map of common line break characters
        /// </summary>
        private static readonly ImmutableHashSet<char> __commonLineBreakCharsMap = ImmutableHashSet.Create<char>(__commonLineBreakChars.AsSpan());



        /// <summary>
        /// Collection of characters being part of common line breaks.
        /// </summary>
        public static IList<char> CommonLineBreakChars => __commonLineBreakChars;

        /// <summary>
        /// Map of characters being part of common line breaks.
        /// </summary>
        public static ISet<char> CommonLineBreakCharsMap => __commonLineBreakCharsMap;



        /// <summary>
        /// Provides character array for line breaks selected by mode.
        /// </summary>
        /// <param name="mode">Line break mode</param>
        /// <param name="customLineBreak">
        ///     Optional: Custom line break characters; used if <paramref name="mode"/> is <see cref="LineBreakMode.Custom"/>;
        ///     otherwise ignored; default value is <see langword="null"/> (resulting in an empty line break character array).
        /// </param>
        /// <returns>
        ///     Array of characters containing the line break selected by <paramref name="mode"/>;
        ///     contains content of <paramref name="customLineBreak"/> if <paramref name="mode"/> is <see cref="LineBreakMode.Custom"/>
        ///     (or an empty array of characters if no <c>customLineBreak</c> is given).
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Unknoen/invalid value for <paramref name="mode"/> supplied.
        /// </exception>
        public static char[] GetLineBreakCharacters(LineBreakMode mode, char[] customLineBreak = null)
        {
            switch (mode)
            {
                case LineBreakMode.None:
                case LineBreakMode.CR:
                case LineBreakMode.NL:
                case LineBreakMode.CRNL:
                case LineBreakMode.NLCR:
                    return __commonLineBreaks[mode];
                case LineBreakMode.Environment:
                    return System.Environment.NewLine.ToArray();
                case LineBreakMode.Custom:
                    return customLineBreak ?? Array.Empty<char>();
                default:
                    throw new ArgumentException($"Unknown line break mode [{mode:F}].", nameof(mode));
            }
        }

        /// <summary>
        /// Check whether <paramref name="customLineBreak"/> contain an uncommon line break chracaters
        /// (<paramref name="customLineBreak"/> may contain any number and combination of common line
        /// break chararcters).
        /// </summary>
        /// <param name="customLineBreak">Custom line break characters to be checked</param>
        /// <returns>
        ///     <see langword="true"/>, if <paramref name="customLineBreak"/> consist of common line break
        ///     characters only (or is null oder empty) - otherwise <see langword="false"/>
        /// </returns>
        public static bool CheckForUncommonLineBreakCharacters(char[] customLineBreak)
        {
            bool retval = (customLineBreak != null)
                            && (customLineBreak.Length > 0)
                            && (true == customLineBreak.Any(c => false == __commonLineBreakChars.Contains(c)));
            return retval;
        }
    }
}
