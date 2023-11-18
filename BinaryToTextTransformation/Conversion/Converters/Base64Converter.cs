using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;

namespace de.Aargenveldt.BinaryToTextTransformation.Conversion.Converters
{
    /// <summary>
    /// Base64 converter for encoding byte arrays into sequences of Base64 encoded characters
    /// or decoding Base64 encoded sequences of characters back into byte arrays.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On Base64 encoding three (3) input bytes are encoded into four (4) characters.
    ///         The encoding is described in RFC4648.
    ///     </para>
    ///     <para>
    ///         Various alphabets may be used as lookup tables in order to encode bytes into
    ///         characters. A suitable alphabet must contain at least 64 members (aka characters
    ///         in its lookup table).
    ///     </para>
    ///     <para>
    ///         You need to apply the same alphabet for encoding and decoding of data.
    ///         Otherwise you get errors on lookups during decoding (best case) or
    ///         data corruption (worst case if the alphabet used for decoding just
    ///         shuffles around the same characters which are contained in the alphabet
    ///         used for encoding).
    ///     </para>
    ///     <para>
    ///         Several canonical alphabets exist (eg. described by RFC4648 - available as
    ///         <see cref="de.Aargenveldt.BinaryToTextTransformation.Conversion.Alphabets.Base64Alphabets.RFC4648Base64"/>).
    ///     </para>
    ///     <para>
    ///         Encoding will fail if padding is required - but the used alphabet does not
    ///         define a padding character.
    ///     </para>
    ///     <para>
    ///         If a converter instance is created, the lookup table (and padding character)
    ///         is copied from the supplied alphabet. So if the alphabet (dynamically) changes
    ///         during runtime the created converter instance always use the original alphabet
    ///         which was in effect on creating the instance.
    ///     </para>
    /// </remarks>
    public class Base64Converter
    {

        private static readonly string[] __mames = new[]
        {
            "base64",
            "Base64 (RFC4648, Default)"
        };



        private IAlphabet _alphabet;
        private readonly char[] _lookupTable;
        private readonly Dictionary<char, uint> _decodingMap;
        private readonly char? _paddingChar;
        private readonly char[] _lineBreakChars;
        private readonly bool _uncommonLineBreakChars;    // true if _lineBreakChars contains any uncommon line break characters




        /// <summary>
        /// The alphabet being used (the lookup table is extracted on initialization)
        /// </summary>
        public IAlphabet Alphabet => this._alphabet;
        /// <summary>
        /// Mode for inserting line break into encoded data.
        /// </summary>
        public LineBreakMode LineBreakMode { get; private set; }

        /// <summary>
        /// The actual line break character(s) being used on inserting line breaks into encoded data.
        /// </summary>
        public char[] LineBreakChars => this._lineBreakChars.ToArray();

        /// <summary>
        /// Line break position (aka length of a line in number of characters);
        /// if value is &lt;=0 no line breaks will be inserted.
        /// </summary>
        public int LineBreakPosition { get; private set; }
        /// <summary>
        /// Indicates whether fill characters are used on encoding output (<see langword="true"/>)
        /// or not (<see langword="false"/>).
        /// </summary>
        public bool Padding { get; private set; }

        /// <summary>
        /// Indicates if whitespaces should be ignored on decoding. (Line breaks will always be
        /// ignored). If set to <see langword="false"/> and encoded input contains any whitespace
        /// (other than line breaks) a <see cref="FormatException"/> is thrown.
        /// </summary>
        public bool IgnoreWhitespaces { get; set; }


        /// <summary>
        /// Name of encoding.
        /// </summary>
        public string Name => __mames[0];

        /// <summary>
        /// Alternate names of encoding.
        /// </summary>
        public IReadOnlyList<string> AlternateNames => __mames;



        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="alphabet">Alphabet for character lookups</param>
        /// <param name="lineBreakMode">Mode for inserting line breaks in encoded text output</param>
        /// <param name="lineBreakPosition">
        ///     Line break position (aka length of a single line); if &lt;=0 insertion
        ///     of line breaks is disabled.
        /// </param>
        /// <param name="padding">
        ///     Enables (<see langword="true"/>) or disables (<see langword="false"/> canonical
        ///     padding of the text output.
        /// </param>
        /// <param name="ignoreWhitespaces">
        ///     Optional: Ignore all whitespace on decoding (<see langword="true"/>) - or throw
        ///     a <see cref="FormatException"/> if any whitespace (other than line breaks) are
        ///     encountered while decoding enocded data.
        /// </param>
        /// <param name="customLineBreak">
        ///     Optional: Custom line break characters; only used if <see cref="LineBreakMode.Custom"/>
        ///     is given for <paramref name="lineBreakMode"/>; otherwise ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="alphabet"/> is <see langword="null"/></description></item>
        ///     </list>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <list type="bullet">
        ///         <item>Padding is enabled but <description><paramref name="alphabet"/> does not define a padding character</description></item>
        ///         <item><description><paramref name="alphabet"/> does not define a suitable alphabet size (at least 64
        ///         alphabet members are expected)</description></item>
        ///     </list>
        /// </exception>
        public Base64Converter(
            IAlphabet alphabet,
            LineBreakMode lineBreakMode,
            int lineBreakPosition,
            bool padding,
            bool ignoreWhitespaces = true,
            char[] customLineBreak = null)
        {
            this._alphabet = alphabet ?? throw new ArgumentNullException(nameof(alphabet));
            if (alphabet.Length < 64)
            {
                throw new ArgumentException($"Unsuitable alphabet (expecting at least 64 characters, actual={alphabet.Length})", nameof(alphabet));
            }

            this._lookupTable = alphabet.LookupTable.Take(64).ToArray();    // we need only the first 64 entries (thus base64)
            this._decodingMap = this._lookupTable
                                    .Select((symbol, index) => (symbol, index))
                                    .ToDictionary(item => item.symbol, item => (uint)item.index);

            if ((true == padding) && (false == alphabet.HasPaddingChar))
            {
                throw new ArgumentException(
                    $"Padding is required but supplied alphabet [{alphabet.GetType().Name}] does not define a padding character",
                    $"{nameof(alphabet)}.{nameof(IAlphabet.PaddingChar)}");
            }
            this._paddingChar = alphabet.PaddingChar;

            this.LineBreakMode = lineBreakMode;
            this.LineBreakPosition = Math.Max(lineBreakPosition, 0);
            this.Padding = padding;
            this.IgnoreWhitespaces = ignoreWhitespaces;

            this._lineBreakChars = LineBreakProvider.GetLineBreakCharacters(lineBreakMode, customLineBreak: customLineBreak);
            this._uncommonLineBreakChars = LineBreakProvider.CheckForUncommonLineBreakCharacters(this._lineBreakChars);
            return;
        }


        /// <summary>
        /// Converting a byte array into a Base64 encoded sequence of characters.
        /// </summary>
        /// <param name="data">Byte array to encode</param>
        /// <param name="offset">0-based offset in <paramref name="data"/></param>
        /// <param name="length">Number of bytes to encode (starting at <paramref name="offset"/>)</param>
        /// <param name="finalBlock">
        ///     Optional: Denotes if a <paramref name="data"/> is the final (or single) block of a sequence of
        ///     blocks to encode. If set to <see langword="false"/> all bytes at the end of <paramref name="data"/>
        ///     which are not part of a full triplet of bytes are returned in the <c>remaining</c> member of
        ///     the result tuple (and thus are not encoded). Otherwise (if set to <see langword="true"/>) all
        ///     bytes from <paramref name="data"/> are encoded - and the output is padded as necessary/wanted.
        ///     Default value is <see langword="true"/>.
        /// </param>
        /// <returns>
        ///     A tuple consisting of two members:
        ///     <list type="table">
        ///         <item>
        ///             <term>encodedData</term>
        ///             <description>An array of characters containing the encoded data; the array is empty,
        ///             if <paramref name="length"/> is <c>==0</c> - or if <paramref name="length"/> is
        ///             not a multiple of 3 <paramref name="finalBlock"/> is set to <see langword="false"/>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>remaining</term>
        ///             <description>An array of bytes containing the remaining, not encoded bytes from
        ///             <paramref name="data"/>. The array is empty if <paramref name="length"/> is
        ///             a multiple of 3 (aka only full triplets of bytes) or is zero (0) or
        ///             <paramref name="finalBlock"/> is set to <see langword="true"/>. In the latter
        ///             case all denoted bytes from <paramref name="data"/> are encoded (and the output 
        ///             is padded as necessary/wanted). 
        ///             The array contain the last one or two byte(s) from the denoted bytes in <paramref name="data"/>
        ///             if <paramref name="length"/> is not a multiple of 3 bytes - and <paramref name="finalBlock"/> 
        ///             is set to <see langword="false"/>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="data"/> is null (and <paramref name="length"/> is not zero)</description></item>
        ///     </list>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="data"/> is empty (and <paramref name="length"/> is not zero)</description></item>
        ///     </list>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="offset"/> is &lt;0 or &gt;=length of <paramref name="data"/></description></item>
        ///         <item><description>The denoted range given by <paramref name="offset"/> and <paramref name="length"/>
        ///         exceeds the boundaries of <paramref name="data"/></description></item>
        ///     </list>
        /// </exception>
        [SecurityCritical]
        public unsafe (char[] encodedData, byte[] remaining) Encode(byte[] data, int offset, int length, bool finalBlock = true)
        {
            char[] encodedData;
            byte[] remaining;

            // checking the arguments
            int dataLength = data?.Length ?? 0;

            // special case: length is 0 --> tolerating null in buffer but offset have to be valid or 0 (other arguments are ignored)
            if ((length == 0)
                && ((offset == 0) || ((offset >= 0) && (offset < dataLength))))
            {
                encodedData = Array.Empty<char>();
                remaining = Array.Empty<byte>();
            }
            // otherwise: No in data?
            else if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            else if (data.Length <= 0)
            {
                throw new ArgumentException("In buffer is empty", nameof(data));
            }
            // otherwise: If offset is out of range, it is an argument error
            else if ((offset < 0) || (offset >= dataLength))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Start offset is out of range: Expected=[0..{dataLength - 1}], Actual={offset}.");
            }
            else if ((length < 0) || (length + offset > dataLength))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Length is out of range: Expected=[0..{dataLength - offset}], Actual={length}.");
            }
            else
            {
                // ok, some valid arguments
                // calculating out buffer size

                //// having a specialized internal Encoding method
                //(encodedData, remaining) = this.DoEncode(data, offset, length, finalBlock);


                int outBufferLength = this.ComputeEncodeBufferSize(length, finalBlock, this.Padding, this.LineBreakPosition, this._lineBreakChars.Length);
                encodedData = new char[outBufferLength];

                fixed (char* outChars = encodedData)
                fixed (byte* inData = data)
                {
                    (int lastInIndex, int outCharactersCount) = this.Encode(outChars, inData, offset, length, this.Padding, finalBlock);

                    if (lastInIndex < offset + length)
                    {
                        // remainings
                        remaining = new byte[offset + length - lastInIndex];
                        Array.Copy(data, lastInIndex, remaining, 0, remaining.Length);
                    }
                    else
                    {
                        remaining = Array.Empty<byte>();
                    }
                }


            }

            var retval = (encodedData: encodedData, remaining: remaining);
            return retval;
        }


        /// <summary>
        /// Converting a byte array into a Base64 encoded string.
        /// </summary>
        /// <param name="data">Byte array to encode</param>
        /// <param name="offset">0-based offset in <paramref name="data"/></param>
        /// <param name="length">Number of bytes to encode (starting at <paramref name="offset"/>)</param>
        /// <param name="finalBlock">
        ///     Optional: Denotes if a <paramref name="data"/> is the final (or single) block of a sequence of
        ///     blocks to encode. If set to <see langword="false"/> all bytes at the end of <paramref name="data"/>
        ///     which are not part of a full triplet of bytes are returned in the <c>remaining</c> member of
        ///     the result tuple (and thus are not encoded). Otherwise (if set to <see langword="true"/>) all
        ///     bytes from <paramref name="data"/> are encoded - and the output is padded as necessary/wanted.
        ///     Default value is <see langword="true"/>.
        /// </param>
        /// <returns>
        ///     A tuple consisting of two members:
        ///     <list type="table">
        ///         <item>
        ///             <term>encodedString</term>
        ///             <description>A string containing the encoded data; the string is empty,
        ///             if <paramref name="length"/> is <c>==0</c> - or if <paramref name="length"/> is
        ///             not a multiple of 3 <paramref name="finalBlock"/> is set to <see langword="false"/>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>remaining</term>
        ///             <description>An array of bytes containing the remaining, not encoded bytes from
        ///             <paramref name="data"/>. The array is empty if <paramref name="length"/> is
        ///             a multiple of 3 (aka only full triplets of bytes) or is zero (0) or
        ///             <paramref name="finalBlock"/> is set to <see langword="true"/>. In the latter
        ///             case all denoted bytes from <paramref name="data"/> are encoded (and the output 
        ///             is padded as necessary/wanted). 
        ///             The array contain the last one or two byte(s) from the denoted bytes in <paramref name="data"/>
        ///             if <paramref name="length"/> is not a multiple of 3 bytes - and <paramref name="finalBlock"/> 
        ///             is set to <see langword="false"/>
        ///             </description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="data"/> is null (and <paramref name="length"/> is not zero)</description></item>
        ///     </list>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="data"/> is empty (and <paramref name="length"/> is not zero)</description></item>
        ///     </list>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="offset"/> is &lt;0 or &gt;=length of <paramref name="data"/></description></item>
        ///         <item><description>The denoted range given by <paramref name="offset"/> and <paramref name="length"/>
        ///         exceeds the boundaries of <paramref name="data"/></description></item>
        ///     </list>
        /// </exception>
        public (string encodedString, byte[] remaining) EncodeToString(byte[] data, int offset, int length, bool finalBlock = true)
        {
            // Creating a string from char array is somewhat less than optimal... Content of char array
            // will be copied to initialize the string...
            // Will perhaps return later to improve - using something like String.FastAllocateString() or String.Create()
            // available in later version of .NET ...

            (char[] encodedData, byte[] remaining) = this.Encode(data, offset, length, finalBlock: finalBlock);
            string encodedString = new string(encodedData);

            var retval = (encodedString: encodedString, remaining: remaining);
            return retval;
        }




        /// <summary>
        /// Internal method encoding the input bufer into its Base64 representation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Unsafe pointers are used to suppress unnecessary bound checks and optimize
        ///         the indexed access. The calle is responsible to give only valid values for
        ///         the arguments (especially <paramref name="offset"/> and <paramref name="length"/>).
        ///     </para>
        ///     <para>
        ///         The supplied <paramref name="outChars"/> must be large enough to receive ALL
        ///         encoded characters (including line breaks and paddings).
        ///     </para>
        /// </remarks>
        /// <param name="outChars"></param>
        /// <param name="inData"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="padding"></param>
        /// <param name="finalBlock"></param>
        /// <returns>
        ///     Tuple with two members:
        ///     <list type="table">
        ///         <item>
        ///             <term>inIndex</term>
        ///             <description>0-based index of the next byte in <paramref name="inData"/> which
        ///             needs to be processed; this values is beyond <paramref name="offset"/> + <paramref name="length"/>
        ///             if <paramref name="finalBlock"/> is set to <see langword="true"/> - or <paramref name="length"/>
        ///             is a multiple of 3; in that cases all denoted bytes from <paramref name="inData"/> are already
        ///             encoded (and the output is padded as necessary/wanted). -
        ///             If <paramref name="finalBlock"/> is set to <see langword="false"/> and <paramref name="length"/>
        ///             is not a multiple of 3 <c>inIndex</c> is pointing to the first byte of the remainings.</description>
        ///         </item>
        ///         <item>
        ///             <term>outCharactersCount</term>
        ///             <description>Number of characters set in <paramref name="outChars"/> (including
        ///             line breaks and paddings)</description>
        ///         </item>
        ///     </list>
        /// </returns>
        [SecurityCritical]
        private unsafe (int inIndex, int outCharactersCount) Encode(char* outChars, byte* inData, int offset, int length, bool padding, bool finalBlock)
        {
            int inIndex;
            int outCharactersCount = 0;
            int numCharsInLine = 0;

            int lengthMod3 = length % 3;
            int completeByteSequenceOffset = offset + length - lengthMod3;

            int lineBreakLength = this._lineBreakChars.Length;

            bool matchingLineLength = (this.LineBreakPosition % 4) == 0;
            bool insertLineBreaks = (this.LineBreakMode != LineBreakMode.None)
                                    && (this.LineBreakPosition > 0)
                                    && (lineBreakLength > 0);

            fixed (char* linebreak = this._lineBreakChars)
            fixed (char* base64 = this._lookupTable)
            {
                // we do not want to waste time for unnecessary comparisons to place line breaks...

                // looping the in buffer - consuming all the full blocks á 3 bytes
                // case 1) a single line without line breaks
                if (false == insertLineBreaks)
                {
                    for (inIndex = offset; inIndex < completeByteSequenceOffset; inIndex += 3)
                    {
                        outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                        outChars[outCharactersCount + 1] = base64[((inData[inIndex] & 0x03) << 4) | ((inData[inIndex + 1] & 0xf0) >> 4)];
                        outChars[outCharactersCount + 2] = base64[((inData[inIndex + 1] & 0x0f) << 2) | ((inData[inIndex + 2] & 0xc0) >> 6)];
                        outChars[outCharactersCount + 3] = base64[(inData[inIndex + 2] & 0x3f)];
                        outCharactersCount += 4;
                    }
                }

                // case 2) line length is a multiple of the output block size
                else if (true == matchingLineLength)
                {
                    // if the line length is a multiple of the output block size we only need to check once per loop step
                    for (inIndex = offset; inIndex < completeByteSequenceOffset; inIndex += 3)
                    {
                        // placing a line break once per loop step
                        if (numCharsInLine == this.LineBreakPosition)
                        {
                            for (int k = 0; k < lineBreakLength; k++)
                            {
                                outChars[outCharactersCount++] = linebreak[k];
                            }
                            numCharsInLine = 0;
                        }
                        outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                        outChars[outCharactersCount + 1] = base64[((inData[inIndex] & 0x03) << 4) | ((inData[inIndex + 1] & 0xf0) >> 4)];
                        outChars[outCharactersCount + 2] = base64[((inData[inIndex + 1] & 0x0f) << 2) | ((inData[inIndex + 2] & 0xc0) >> 6)];
                        outChars[outCharactersCount + 3] = base64[(inData[inIndex + 2] & 0x3f)];

                        outCharactersCount += 4;
                        numCharsInLine += 4;
                    }
                }

                // case 3) line length is not a multiple of the output block size - need to check on every placed character
                else
                {


                    for (inIndex = offset; inIndex < completeByteSequenceOffset; inIndex += 3)
                    {
                        void CheckAndPlaceLineBreak(char* lineBreakChars)
                        {
                            if (numCharsInLine == this.LineBreakPosition)
                            {
                                for (int k = 0; k < lineBreakLength; k++)
                                {
                                    outChars[outCharactersCount++] = lineBreakChars[k];
                                }
                                numCharsInLine = 0;
                            }
                        }

                        CheckAndPlaceLineBreak(linebreak);
                        outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                        ++numCharsInLine;

                        CheckAndPlaceLineBreak(linebreak);
                        outChars[outCharactersCount + 1] = base64[((inData[inIndex] & 0x03) << 4) | ((inData[inIndex + 1] & 0xf0) >> 4)];
                        ++numCharsInLine;

                        CheckAndPlaceLineBreak(linebreak);
                        outChars[outCharactersCount + 2] = base64[((inData[inIndex + 1] & 0x0f) << 2) | ((inData[inIndex + 2] & 0xc0) >> 6)];
                        ++numCharsInLine;

                        CheckAndPlaceLineBreak(linebreak);
                        outChars[outCharactersCount + 3] = base64[(inData[inIndex + 2] & 0x3f)];
                        ++numCharsInLine;

                        outCharactersCount += 4;
                    }
                }


                // if this is a final block we need to handle the remaning bytes from the end of the in buffer
                if (true == finalBlock)
                {
                    // no line breaks - or matching line length
                    if ((false == insertLineBreaks) || (true == matchingLineLength))
                    {
                        // ~~~ kann weg
                        //if ((false == insertLineBreaks) && (numCharsInLine == this.LineBreakPosition))
                        //{
                        //    for (int k = 0; k < lineBreakLength; k++)
                        //    {
                        //        outChars[outCharactersCount++] = linebreak[k];
                        //    }
                        //    numCharsInLine = 0;
                        //}

                        switch (lengthMod3)
                        {
                            case 2: //One character padding needed
                                outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                                outChars[outCharactersCount + 1] = base64[((inData[inIndex] & 0x03) << 4) | ((inData[inIndex + 1] & 0xf0) >> 4)];
                                outChars[outCharactersCount + 2] = base64[(inData[inIndex + 1] & 0x0f) << 2];

                                // padding?
                                if (true == this.Padding)
                                {
                                    outChars[outCharactersCount + 3] = this._paddingChar.Value; //Pad
                                    outCharactersCount += 4;
                                }
                                else
                                {
                                    outCharactersCount += 3;
                                }
                                inIndex += 2;
                                break;
                            case 1: // Two character padding needed
                                outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                                outChars[outCharactersCount + 1] = base64[(inData[inIndex] & 0x03) << 4];

                                if (true == this.Padding)
                                {
                                    outChars[outCharactersCount + 2] = this._paddingChar.Value; //Pad
                                    outChars[outCharactersCount + 3] = this._paddingChar.Value; //Pad
                                    outCharactersCount += 4;
                                }
                                else
                                {
                                    outCharactersCount += 2;
                                }
                                ++inIndex;
                                break;
                        }
                    }

                    // line breaks anywhere?
                    else
                    {
                        void CheckAndPlaceLineBreak(char* lineBreakChars)
                        {
                            if (numCharsInLine == this.LineBreakPosition)
                            {
                                for (int k = 0; k < lineBreakLength; k++)
                                {
                                    outChars[outCharactersCount++] = lineBreakChars[k];
                                }
                                numCharsInLine = 0;
                            }
                        }

                        switch (lengthMod3)
                        {
                            case 2: //One character padding needed
                                CheckAndPlaceLineBreak(linebreak);
                                outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                                ++numCharsInLine;

                                CheckAndPlaceLineBreak(linebreak);
                                outChars[outCharactersCount + 1] = base64[((inData[inIndex] & 0x03) << 4) | ((inData[inIndex + 1] & 0xf0) >> 4)];
                                ++numCharsInLine;

                                CheckAndPlaceLineBreak(linebreak);
                                outChars[outCharactersCount + 2] = base64[(inData[inIndex + 1] & 0x0f) << 2];
                                ++numCharsInLine;

                                // padding?
                                if (true == this.Padding)
                                {
                                    CheckAndPlaceLineBreak(linebreak);
                                    outChars[outCharactersCount + 3] = this._paddingChar.Value; //Pad
                                    ++numCharsInLine;
                                    outCharactersCount += 4;
                                }
                                else
                                {
                                    ++numCharsInLine;
                                    outCharactersCount += 3;
                                }
                                inIndex += 2;
                                break;
                            case 1: // Two character padding needed
                                CheckAndPlaceLineBreak(linebreak);
                                outChars[outCharactersCount] = base64[(inData[inIndex] & 0xfc) >> 2];
                                ++numCharsInLine;

                                CheckAndPlaceLineBreak(linebreak);
                                outChars[outCharactersCount + 1] = base64[(inData[inIndex] & 0x03) << 4];
                                ++numCharsInLine;

                                if (true == this.Padding)
                                {
                                    CheckAndPlaceLineBreak(linebreak);
                                    outChars[outCharactersCount + 2] = this._paddingChar.Value; //Pad
                                    ++numCharsInLine;

                                    CheckAndPlaceLineBreak(linebreak);
                                    outChars[outCharactersCount + 3] = this._paddingChar.Value; //Pad
                                    ++numCharsInLine;
                                    outCharactersCount += 4;
                                }
                                else
                                {
                                    ++numCharsInLine;
                                    outCharactersCount += 2;
                                }
                                ++inIndex;
                                break;
                        }

                    }
                }
            }

            var retval = (inIndex: inIndex, outCharactersCount: outCharactersCount);
            return retval;
        }




        /// <summary>
        /// Determines the number of characters needed for output character buffer.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         On non final blocks remaining bytes (incomplete blocks at end of input byte sequence)
        ///         are returned to caller - those bytes are not encoded and thus not included in calculation
        ///         of the output buffer size! On final blocks those supernumerary bytes are encoded as well 
        ///         - and maybe the incomplete block is padded.
        ///     </para>
        /// </remarks>
        /// <param name="numInBytes">Number of bytes being encoded</param>
        /// <param name="finalBlock"><see langword="true"/> if this is the final block being encoded - otherwise <see langword="false"/></param>
        /// <param name="padding"><see langword="true"/> is padding is enabled - otherwise <see langword="false"/></param>
        /// <param name="lineBreakPosition">Number of characters per line; if &lt;=0 no line breaks are inserted</param>
        /// <param name="lineBreakLength">Length of line breaks (usually 1 or 2 - based on environment)</param>
        /// <returns>Number of characters needed for output buffer</returns>
        /// <exception cref="OutOfMemoryException">
        ///     The encoding buffer size exceeds the <see cref="Int32"/> value range.
        /// </exception>
        private int ComputeEncodeBufferSize(int numInBytes, bool finalBlock, bool padding, int lineBreakPosition, int lineBreakLength)
        {
            int retval;
            if (numInBytes <= 0)
            {
                retval = 0;
            }
            else
            {
                // we may get those from the Alphabet... but we are doing Base64 Encoding/Decoding - so these values are fixed anyway...
                //int byteSequenceLenth = this._alphabet.ByteSequenceLength;
                //int charSequenceLengt = this._alphabet.CharSequenceLength;

                long outBufferSize = ((long)numInBytes) / 3 * 4;
                int remainingIn = numInBytes % 3;

                // calculating needed space for remaining bytes (incomplete block at the end)
                // if this is a non final block the remaining bytes are given back to the caller - we do
                // not need to consider them here
                if ((true == finalBlock) && (remainingIn != 0))
                {
                    // if padding is used always a full encoded block is used
                    if (true == padding)
                    {
                        outBufferSize += 4;
                    }
                    else
                    {
                        // otherwise only the characters used for encoding are needed
                        switch (remainingIn)
                        {
                            case 1: outBufferSize += 2; break;
                            case 2: outBufferSize += 3; break;
                        }
                    }
                }

                // factoring the line breaks in
                // rules:
                //   * calculation is done on current block only...
                //     if a non final block with incomplete last line has previously been encoded the caller must handle this
                //   * no line break is appended after last line
                if (lineBreakPosition > 0)
                {
                    long numNewLines = outBufferSize / lineBreakPosition;
                    if (outBufferSize % lineBreakPosition == 0)
                    {
                        // we do not want a line break after the last line
                        // so we need to remove it if the last line is complete
                        --numNewLines;
                    }
                    outBufferSize += numNewLines * lineBreakLength;
                }

                if (outBufferSize > int.MaxValue)
                {
                    throw new OutOfMemoryException($"Needed Base64 encoding output character buffer exceeds maximum (max={int.MaxValue}, actual={outBufferSize})");
                }

                retval = (int)outBufferSize;
            }

            return retval;
        }


        public unsafe (byte[] decodedData, char[] remaining) Decode(char[] encodedData, int offset, int length, bool finalBlock, bool stopOnPadding)
        {
            (byte[] decodedData, char[] remaining) retval;

            int encodedDataLength = encodedData?.Length ?? 0;

            // special case: length is zero (0) - offset must be valid (or zero)
            if ((length == 0)
                && ((offset == 0) || ((offset >= 0) && (offset < encodedDataLength))))
            {
                retval = (decodedData: Array.Empty<byte>(), remaining: Array.Empty<char>());
            }
            else if (encodedData == null)
            {
                throw new ArgumentNullException(nameof(encodedData));
            }
            else if (encodedDataLength <= 0)
            {
                throw new ArgumentException("In buffer is empty", nameof(encodedData));
            }
            // otherwise: If offset is out of range, it is an argument error
            else if ((offset < 0) || (offset >= encodedDataLength))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Start offset is out of range: Expected=[0..{encodedDataLength - 1}], Actual={offset}.");
            }
            else if ((length < 0) || (length + offset > encodedDataLength))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Length is out of range: Expected=[0..{encodedDataLength - offset}], Actual={length}.");
            }
            else
            {
                // ok, some valid arguments
                // fixing in buffer
                fixed (char* inBuffer = encodedData)
                {
                    retval = this.Decode(inBuffer + offset, length, finalBlock, stopOnPadding);
                }
            }

            return retval;
        }


        public unsafe (byte[] decodedData, char[] remaining) DecodeFromString(string encodedData, int offset, int length, bool finalBlock, bool stopOnPadding)
        {
            (byte[] decodedData, char[] remaining) retval;

            int encodedDataLength = encodedData?.Length ?? 0;

            // special case: length is zero (0) - offset must be valid (or zero)
            if ((length == 0)
                && ((offset == 0) || ((offset >= 0) && (offset < encodedDataLength))))
            {
                retval = (decodedData: Array.Empty<byte>(), remaining: Array.Empty<char>());
            }
            else if (encodedData == null)
            {
                throw new ArgumentNullException(nameof(encodedData));
            }
            else if (encodedDataLength <= 0)
            {
                throw new ArgumentException("In buffer is empty", nameof(encodedData));
            }
            // otherwise: If offset is out of range, it is an argument error
            else if ((offset < 0) || (offset >= encodedDataLength))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Start offset is out of range: Expected=[0..{encodedDataLength - 1}], Actual={offset}.");
            }
            else if ((length < 0) || (length + offset > encodedDataLength))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Length is out of range: Expected=[0..{encodedDataLength - offset}], Actual={length}.");
            }
            else
            {
                // ok, some valid arguments
                // fixing in buffer
                fixed (char* inBuffer = encodedData)
                {
                    retval = this.Decode(inBuffer + offset, length, finalBlock, stopOnPadding);
                }
            }

            return retval;
        }


        private unsafe (byte[] decodedData, char[] remaining) Decode(char* encodedData, int length, bool finalBlock, bool stopOnPadding)
        {
            byte[] decodedData;
            char[] remaining;


            // calculating out buffer size - and the number of characters to be consumed
            char* ptr = encodedData;
            (int outBufferSize, int charsToConsume) = this.ComputDecodeBufferSize(ptr, length, finalBlock, stopOnPadding);


            decodedData = new byte[outBufferSize];

            if (outBufferSize > 0)
            {
                fixed (byte* outBufferPtr = decodedData)
                {
                    int symbolCount = 0;
                    uint decodingCache = 0;

                    byte* outPtr = outBufferPtr;
                    char* endPtr = ptr + charsToConsume;

                    // at this point we are safe to just consume the significant contents of the in buffer
                    // (otherwise the compute method would have thrown an exception)
                    if (true == this._paddingChar.HasValue)
                    {
                        char paddingChar = this._paddingChar.Value;

                        while (ptr < endPtr)
                        {
                            char currentChar = *ptr;

                            if (currentChar == paddingChar)
                            {
                                decodingCache = decodingCache << 6;
                                ++symbolCount;
                            }
                            else if (this._decodingMap.TryGetValue(currentChar, out uint value))
                            {
                                decodingCache = (decodingCache << 6) | (value & 0x3f);
                                ++symbolCount;
                            }
                            // in any other case we ignore the character (it is a line break or whitespace...

                            if (symbolCount == 4)
                            {
                                // we have got a quartet of symbols decoded into a triplet of bytes...
                                // now put them into the out buffer
                                *outPtr = (byte)(decodingCache >> 16);
                                *(outPtr + 1) = (byte)(decodingCache >> 8);
                                *(outPtr + 2) = (byte)(decodingCache);

                                outPtr += 3;
                                symbolCount = 0;
                                decodingCache = 0;
                            }

                            ++ptr;
                        }
                    }
                    else
                    {
                        // no padding availablee
                        while (ptr < endPtr)
                        {
                            char currentChar = *ptr;

                            if (this._decodingMap.TryGetValue(currentChar, out uint value))
                            {
                                decodingCache = (decodingCache << 6) | (value & 0x3f);
                                ++symbolCount;
                            }
                            // in any other case we ignore the character (it is a line break or whitespace...

                            if (symbolCount == 4)
                            {
                                // we have got a quartet of symbols decoded into a triplet of bytes...
                                // now put them into the out buffer
                                *outPtr = (byte)(decodingCache >> 16);
                                *(outPtr + 1) = (byte)(decodingCache >> 8);
                                *(outPtr + 2) = (byte)(decodingCache);

                                outPtr += 3;
                                symbolCount = 0;
                                decodingCache = 0;
                            }

                            ++ptr;
                        }
                    }

                    // we may have some surplus symbols (if no padding is used)
                    if (symbolCount > 0)
                    {
                        // we may have 2 or 3 (encoding 1 or 2 bytes)
                        // if we have only 1 this is a format error (and the compute method should already have thrown an exception)
                        // we cannot have 4 (or more), because that many would have been processed inside the loop
                        switch (symbolCount)
                        {
                            case 2:
                                *outPtr = (byte)(decodingCache >> 4);
                                ++outPtr;
                                break;
                            case 3:
                                *outPtr = (byte)(decodingCache >> 10);
                                *(outPtr + 1) = (byte)(decodingCache >> 2);
                                outPtr += 2;
                                break;
                            default:
                                // 1 - or 4
                                throw new FormatException($"Invalid number of surplus symbols (at end of encoded data) encountered (expected=2 or 3, actual={symbolCount}).");

                        }
                    }
                }
            }

            // now preparing the collection of remaining characters...
            if (charsToConsume < length)
            {
                remaining = new char[length - charsToConsume];
                fixed(char* remainingPtr = remaining)
                {
                    for (int i = 0; i< remaining.Length; i++)
                    {
                        *(remainingPtr + i) = *ptr;
                        ++ptr;
                    }
                }
            }
            else
            {
                remaining = Array.Empty<char>();
            }

            (byte[] decodedData, char[] remaining) retval = (decodedData: decodedData, remaining: remaining);
            return retval;
        }




        /// <summary>
        /// Compute the exact size of decoding output (byte)buffer - and the number of characters
        /// which need to be consumed from input buffer.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Currently the whole input buffer is processed.
        ///     </para>
        ///     <para>
        ///         Any quartet of significant characters (as of part of the encoding alphabet)
        ///         need to be decoded into a triplet (three, 3) bytes.
        ///     </para>
        ///     <para>
        ///         Line breaks are always skipped. If a custom line break is defined (<see cref="LineBreakChars"/>)
        ///         it is also recognized.
        ///     </para>
        ///     <para>
        ///         If whitespaces are allowed (<see cref="IgnoreWhitespaces"/> is set to <see langword="true"/>)
        ///         they are skipped too. Otherwise a <see cref="FormatException"/> is thrown if a whitespace
        ///         character is encountered.
        ///     </para>
        ///     <para>
        ///         All characters indicated by <paramref name="inputLength"/> are considered. If padding
        ///         characters are encountered - and they are not the last character(s) in input data, the
        ///         behavior depend on <paramref name="stopOnPadding"/>. If <paramref name="stopOnPadding"/>
        ///         is set to <see langword="true"/>, processing stops after the last required padding
        ///         character is consumed. Otherwise: Any number of whitespaces may follow the last
        ///         required padding character - they are ignored whatever state is indicated by
        ///         <see cref="IgnoreWhitespaces"/>. But if any non whitespace character is present
        ///         a <see cref="FormatException"/> will be thrown.
        ///     </para>
        ///     <para>
        ///         If input does not contain enough significant characters (multiple of four (4) characters)
        ///         to form a quartet of symbols at the end of the input the behaviour depends on the state
        ///         of <paramref name="finalBlock"/>. If <paramref name="finalBlock"/> is set to <see langword="true"/>
        ///         all present symbols (at least two (2) are needed) are decoded - if less than two remaining
        ///         (significant) characters are found, a <see cref="FormatException"/> is thrown.
        ///     </para>
        ///     <para>
        ///         If <paramref name="finalBlock"/> is set to <see langword="false"/>, the remaining characters
        ///         are ignored (they will be given back to caller later).
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     Tuple with two elements
        ///     <list type="table">
        ///         <item>
        ///             <term>outBufferSize</term>
        ///             <description>Size of output buffer needed for decoding (number of bytes)</description>
        ///         </item>
        ///         <item>
        ///             <term>charsToConsume</term>
        ///             <description>Number of characters to be consumed from input buffer</description>
        ///         </item>
        ///     </list>
        /// </returns>
        [SecurityCritical]
        private unsafe (int outBufferSize, int charsToConsume) ComputDecodeBufferSize(char* inputPtr, int inputLength, bool finalBlock, bool stopOnPadding)
        {
            int outBufferSize;
            int charsToConsume;


            // special case: input length is zero --> no output buffer needed
            if (inputLength == 0)
            {
                outBufferSize = 0;
                charsToConsume = 0;
            }

            // sanity checks
            else if (inputLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputLength), $"Input length must not be less than zero (actual value={inputLength}");
            }
            else if (inputPtr == null)
            {
                throw new ArgumentNullException(nameof(inputPtr));
            }
            else
            {
                // do we have a custom linebreak?
                bool uncommonLineBreakChars = this._uncommonLineBreakChars;
                int customLineBreakLength = 0;
                char* customLineBreak = stackalloc char[this._lineBreakChars.Length];
                if (true == uncommonLineBreakChars)
                {
                    customLineBreakLength = this._lineBreakChars.Length;
                    for (int i = 0; i < customLineBreakLength; i++)
                    {
                        customLineBreak[i] = this._lineBreakChars[i];
                    }
                }
                // map of common line break characters
                ISet<char> commonLineBreakCharsMap = LineBreakProvider.CommonLineBreakCharsMap;

                // do we have a padding character?
                bool hasPaddingChar = this._paddingChar.HasValue;
                int paddingCharCount = 0;

                // do we ignore whitespaces in encoded data?
                bool ignoreWhitespaces = this.IgnoreWhitespaces;


                // now walk the input buffer...
                char* ptr = inputPtr;
                char* endPtr = inputPtr + inputLength;
                long significantInput = inputLength;


                while (ptr < endPtr)
                {
                    char c = *ptr;
                    // is current character a valid symbol?
                    // (need to check this first because whitespaces - and even line breaks - may be regular symbols in current alphabet)
                    if (true == this._decodingMap.ContainsKey(c))
                    {
                        // NOP - this is a significant char, just skip to next character
                    }
                    // is current character a padding char?
                    else if ((true == hasPaddingChar) && (c == this._paddingChar))
                    {
                        // padding characters are significant... but we need only 1 or 2 depending on the
                        // length of the last encoded group of bytes (3 bytes --> 4 chars, 2 bytes --> 3 char + 1 padding, 1 bytes --> 2 char + 2 padding)
                        ++paddingCharCount;
                        // breaking out of loop - so we need no extra comparison for this case...
                        ++ptr;
                        break;
                    }
                    // we ignore line breaks... do we have a custom line break (need to check first, because it may contain common line break characters)?
                    // perhaps we should roll an extra loop to cut the extra comparison per loop pass?
                    else if ((true == uncommonLineBreakChars)
                            && (true == this.CheckForCharSequence(ptr, endPtr, customLineBreak, customLineBreakLength)))
                    {
                        significantInput -= customLineBreakLength;
                        ptr += (customLineBreakLength - 1);
                    }
                    else if (true == commonLineBreakCharsMap.Contains(c))
                    {
                        --significantInput;
                    }
                    // is it a whitespace (which is not a valid symbol)?
                    else if (true == Char.IsWhiteSpace(c))
                    {
                        if (true == ignoreWhitespaces)
                        {
                            --significantInput;
                        }
                        else
                        {
                            throw new FormatException($"Invalid whitespace encountered (current position={ptr - inputPtr + 1}).");
                        }
                    }
                    else
                    {
                        // invalid character
                        throw new FormatException($"Invalid character encountered (current position={ptr - inputPtr + 1}, invalid character='{c}').");
                    }

                    ++ptr;
                }

                // if we have already scanned all characters...
                if (ptr >= endPtr)
                {
                    // special case: if we have found a single padding character - but need two this is a format error
                    if ((paddingCharCount > 0) && (significantInput % 4 != 0))
                    {
                        throw new FormatException($"Invalid number of padding characters found (expecting 2, found 1).");
                    }
                }
                else
                {
                    // we are not at end of input... so we have found a padding character
                    // we assume all following characters being not significant...
                    significantInput -= (endPtr - ptr);
                    // ...but we may need more padding...
                    int expectedPaddingCharCount = 4 - (int)(significantInput % 4) + 1;  // +1 because we already have found one
                    while ((ptr < endPtr) && (paddingCharCount < expectedPaddingCharCount))
                    {
                        char c = *ptr;

                        if (c == this._paddingChar)
                        {
                            // found a padding character
                            ++paddingCharCount;
                            ++significantInput;
                        }
                        // we ignore line breaks... do we have a custom line break (need to check first, because it may contain common line break characters)?
                        // perhaps we should roll an extra loop to cut the extra comparison per loop pass?
                        else if ((true == uncommonLineBreakChars)
                                && (true == this.CheckForCharSequence(ptr, endPtr, customLineBreak, customLineBreakLength)))
                        {
                            // NOP - just skipping beyond
                            ptr += (customLineBreakLength - 1);
                        }
                        else if (true == commonLineBreakCharsMap.Contains(c))
                        {
                            // NOP - just skipping to next char
                        }
                        // is it a whitespace 
                        else if (true == Char.IsWhiteSpace(c))
                        {
                            // NOP - just skippping to next char
                        }
                        else
                        {
                            // invalid character
                            throw new FormatException($"Invalid character (neither whitespace nor line break) encountered after (first) padding character (current position={ptr - inputPtr + 1}, invalid character='{c}').");
                        }
                        ++ptr;
                    }

                    // if we do not have found the expected number of padding characters
                    // it is a form error
                    if ((paddingCharCount > 0) && (paddingCharCount < expectedPaddingCharCount))
                    {
                        throw new FormatException($"Invalid number of padding characters found (expecting={expectedPaddingCharCount}, found={paddingCharCount}).");
                    }

                }

                // if still not at end of input, behaviour depends on stopOnPadding
                if (ptr < endPtr)
                {
                    if ((paddingCharCount > 0) && (true == stopOnPadding))
                    {
                        // any remaining characters are ignored (and given back to caller later on)
                        charsToConsume = (int)(ptr - inputPtr);
                    }
                    else
                    {
                        // all remaining characters must be line breaks or whitespaces...
                        charsToConsume = inputLength;

                        while (ptr < endPtr)
                        {
                            char c = *ptr;

                            // we ignore line breaks... do we have a custom line break (need to check first, because it may contain common line break characters)?
                            // perhaps we should roll an extra loop to cut the extra comparison per loop pass?
                            if ((true == uncommonLineBreakChars)
                                && (true == this.CheckForCharSequence(ptr, endPtr, customLineBreak, customLineBreakLength)))
                            {
                                // NOP - just skipping beyond
                                ptr += (customLineBreakLength - 1);
                            }
                            else if (true == commonLineBreakCharsMap.Contains(c))
                            {
                                // NOP - just skipping to next char
                            }
                            // is it a whitespace?
                            else if (true == Char.IsWhiteSpace(c))
                            {
                                // NOP - just skippping to next char
                            }
                            else
                            {
                                // invalid character
                                throw new FormatException($"Invalid character (neither whitespace nor line break) encountered after padding (current position={ptr - inputPtr + 1}, invalid character='{c}').");
                            }
                        }
                    }
                }
                else
                {
                    // we will consume all characters from input
                    charsToConsume = inputLength;
                }


                // now calculating the needed outbuffer size...
                // we need 3 bytes for any full quartet of significant characters (symbols) - including trailing padding characters
                outBufferSize = ((int)significantInput >> 2) * 3;
                int remainingSiginificantChars = (int)significantInput % 4;

                // if we have found a multiple of 4 significant characters - OR
                // if this is NOT a final block, we are done (remaining characters will be given back to caller)
                // otherwise:
                if ((true == finalBlock) && (remainingSiginificantChars > 0))
                {
                    // if we have found padding characters but do not have a full quartet of characters
                    // it is a format error
                    if (paddingCharCount > 0)
                    {
                        int expectedCharCount = ((int)significantInput >> 2) * 4 + 4;
                        throw new FormatException($"Invalid number of (significant) characters found (despite of padding) (expected number of significant characters={expectedCharCount}, found={significantInput}).");
                    }

                    // otherwise we need to increase outbuffer size depending on the number of
                    // additional significant characters found:
                    switch (remainingSiginificantChars)
                    {
                        case 0: break;          // won't match ever
                        case 2:
                            ++outBufferSize;    // 2 symbols can encode up to 12 bits --> 1 byte
                            break;
                        case 3:
                            outBufferSize += 2; // 3 symbols can encode up to 18 bits --> 2 bytes
                            break;
                        case 1:                 // now this is a format error: 1 symbol can only encode 6 bits --> not a full byte
                        default:                // just a catch all: only 1..3 are even possible
                            int expectedCharCount = ((int)significantInput >> 2) * 4 + 4;
                            throw new FormatException($"Invalid number of (significant) characters found (expected number of significant characters={expectedCharCount}, found={significantInput}).");
                    }
                }

                // now we need to take padding into account...
                if (paddingCharCount == 1)
                    --outBufferSize;
                else if (paddingCharCount == 2)
                    outBufferSize -= 2;
            }

            (int outBufferSize, int charsToConsume) retval = (outBufferSize: outBufferSize, charsToConsume: charsToConsume);
            return retval;
        }

        /// <summary>
        /// Check whether a given char sequence is present at current position [of a text/character buffer].
        /// </summary>
        /// <param name="inPtr">Pointer to current textposition</param>
        /// <param name="endPtr">Pointer AFTER last character in buffer</param>
        /// <param name="charSequence">Sequence of characters that need to be checked (sequence
        /// is compared case sensitive)</param>
        /// <param name="charSequenceLength">Length of character sequence</param>
        /// <returns>
        ///     <see langword="true"/> if the exact character sequence is found at <paramref name="inPtr"/>;
        ///     otherweise <see langword="false"/>; it is also <see langword="false"/> if <paramref name="charSequenceLength"/>
        ///     is &lt;=0 or <paramref name="charSequence"/> is <see langword="null"/>.
        /// </returns>
        private unsafe bool CheckForCharSequence(char* inPtr, char* endPtr, char* charSequence, int charSequenceLength)
        {
            bool retval;
            if ((charSequenceLength <= 0) || (charSequence == null))
            {
                retval = false;
            }
            else if ((endPtr - inPtr) >= charSequenceLength)
            {
                retval = true;
                for (int i = 0; i < charSequenceLength; i++)
                {
                    if (inPtr[i] != charSequence[i])
                    {
                        retval = false;
                        break;
                    }
                }
            }
            else
            {
                retval = false;
            }
            return retval;
        }


    }
}
