using System.Collections.Generic;

namespace de.Aargenveldt.BinaryToTextTransformation.Conversion
{
    /// <summary>
    /// Interface for classes describing an encoding alphabet.
    /// </summary>
    public interface IAlphabet
    {
        /// <summary>
        /// Name of the encoding alphabet.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Liste of alternate names of the encoding alphabet.
        /// </summary>
        IReadOnlyList<string> AlternateNames { get; }


        /// <summary>
        /// Length of alphabet (aka number of characters in <see cref="LookupTable"/>).
        /// </summary>
        int Length { get; }

        /// <summary>
        /// 0-based Lookup Table for characters.
        /// </summary>
        char[] LookupTable { get; }

        /// <summary>
        /// Character used for Padding (<see langword="null"/> if no padding is available).
        /// </summary>
        char? PaddingChar { get; }

        /// <summary>
        /// Determine whether a padding character is available (and thus <see cref="PaddingChar"/> is set).
        /// </summary>
        bool HasPaddingChar { get; }

        /// <summary>
        /// Block size for encoding a sequence of bytes into Text - or resulting block size for
        /// decoding a sequence of characters.
        /// </summary>
        int ByteSequenceLength { get; }

        /// <summary>
        /// Block size of encoded sequence of characters - or block size needed for decoding a
        /// sequence of characters into a sequence of bytes.
        /// </summary>
        int CharSequenceLength { get; }

    }
}
