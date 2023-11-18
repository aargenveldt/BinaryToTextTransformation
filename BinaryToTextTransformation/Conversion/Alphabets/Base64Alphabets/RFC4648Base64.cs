using System;
using System.Collections.Generic;
using System.Linq;

namespace de.Aargenveldt.BinaryToTextTransformation.Conversion.Alphabets.Base64Alphabets
{
    /// <summary>
    ///     <see cref="IAlphabet"/> implementation providing the default RFC4648 Base64 encoding alphabet.
    /// </summary>
    public sealed class RFC4648Base64 : IAlphabet
    {
        /// <summary>
        /// Alphabet
        /// </summary>
        private static readonly char[] __alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToArray();
        /// <summary>
        /// Padding character
        /// </summary>
        private static readonly char __paddingchar = '=';

        /// <summary>
        /// Names array for current alphabet.
        /// </summary>
        private static readonly string[] __names = new string[]
        {
            "RFC4648 Base64 (default)",
            "base64"
        };



        /// <summary>
        /// Default instance of <see cref="RFC4648Base64"/> (providing the default RFC4648 Base64 encoding alphabet).
        /// </summary>
        public static RFC4648Base64 Default => __defaultInstance.Value;
        private static readonly Lazy<RFC4648Base64> __defaultInstance = new Lazy<RFC4648Base64>(() => new RFC4648Base64(), true);


        /// <summary>
        /// Default constructor.
        /// </summary>
        private RFC4648Base64()
        {
            // NOP
        }

        #region IAlphabet



        /// <inheritdoc/>
        public string Name => __names[0];

        /// <inheritdoc/>
        public IReadOnlyList<string> AlternateNames => __names;


        /// <inheritdoc/>
        public int Length => __alphabet.Length;

        /// <inheritdoc/>
        public char[] LookupTable => __alphabet;

        /// <inheritdoc/>
        public char? PaddingChar => __paddingchar;

        /// <inheritdoc/>
        public int ByteSequenceLength => 3;

        /// <inheritdoc/>
        public int CharSequenceLength => 4;

        /// <inheritdoc/>
        public bool HasPaddingChar => true;

        #endregion
    }
}
