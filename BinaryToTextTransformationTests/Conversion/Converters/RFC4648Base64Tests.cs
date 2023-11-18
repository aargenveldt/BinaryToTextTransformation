using de.Aargenveldt.BinaryToTextTransformation.Conversion;
using de.Aargenveldt.BinaryToTextTransformation.Conversion.Alphabets.Base64Alphabets;
using de.Aargenveldt.BinaryToTextTransformation.Conversion.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryToTextTransformationTests.Conversion.Converters
{
    /// <summary>
    /// Unit tests for <see cref="Base64Converter"/>.
    /// </summary>
    public class RFC4648Base64Tests
    {
        /// <summary>
        /// Base64 encoding of default test vectors provided by RFC4648
        /// (using default alphabet and padding - UTF8 is used to transform <paramref name="teststring"/>
        /// into a sequence of bytes).
        /// </summary>
        /// <param name="teststring">Teststring to be encoded</param>
        /// <param name="expected">Expected output</param>
        [TestCase("", "")]
        [TestCase("f", "Zg==")]
        [TestCase("fo", "Zm8=")]
        [TestCase("foo", "Zm9v")]
        [TestCase("foob", "Zm9vYg==")]
        [TestCase("fooba", "Zm9vYmE=")]
        [TestCase("foobar", "Zm9vYmFy")]
        public void When_EncodingRfcTestVectorsAsUtf8BytesWithDefaultAlphabetAndPadding_Expect_RfcResults(string teststring, string expected)
        {
            Base64Converter base64Converter = new Base64Converter(RFC4648Base64.Default, LineBreakMode.None, -1, true);

            byte[] testdata = Encoding.UTF8.GetBytes(teststring);
            char[] actual;
            byte[] remaining;

            (actual, remaining) = base64Converter.Encode(testdata, 0, testdata.Length);
            Assert.That(remaining, Is.Empty);
            Assert.That(actual, Is.EqualTo(expected));
        }

        /// <summary>
        /// Base64 encoding of default test vectors provided by RFC4648
        /// (using default alphabet and NO padding - UTF8 is used to transform <paramref name="teststring"/>
        /// into a sequence of bytes).
        /// </summary>
        /// <param name="teststring">Teststring to be encoded</param>
        /// <param name="expected">Expected output</param>
        [TestCase("", "")]
        [TestCase("f", "Zg")]
        [TestCase("fo", "Zm8")]
        [TestCase("foo", "Zm9v")]
        [TestCase("foob", "Zm9vYg")]
        [TestCase("fooba", "Zm9vYmE")]
        [TestCase("foobar", "Zm9vYmFy")]
        public void When_EncodingRfcTestVectorsAsUtf8BytesWithDefaultAlphabetAndNoPadding_Expect_RfcResults(string teststring, string expected)
        {
            Base64Converter base64Converter = new Base64Converter(RFC4648Base64.Default, LineBreakMode.None, -1, false);

            byte[] testdata = Encoding.UTF8.GetBytes(teststring);
            char[] actual;
            byte[] remaining;

            (actual, remaining) = base64Converter.Encode(testdata, 0, testdata.Length);
            Assert.That(remaining, Is.Empty);
            Assert.That(actual, Is.EqualTo(expected));
        }



        /// <summary>
        /// Base64 decoding of default test vectors provided by RFC4648
        /// (using default alphabet and padding - UTF8 is used to transform 
        /// decoded data into a string for comparison.
        /// </summary>
        /// <param name="encodedString">Teststring to be decoded</param>
        /// <param name="expected">Expected output</param>
        [TestCase("", "")]
        [TestCase("Zg==", "f")]
        [TestCase("Zm8=", "fo")]
        [TestCase("Zm9v", "foo" )]
        [TestCase("Zm9vYg==", "foob")]
        [TestCase("Zm9vYmE=", "fooba")]
        [TestCase("Zm9vYmFy", "foobar")]
        public void When_DecodingRfcTestVectorsWithDefaultAlphabetAndPadding_Expect_RfcResults(string encodedString, string expected)
        {
            Base64Converter base64Converter = new Base64Converter(RFC4648Base64.Default, LineBreakMode.None, -1, true);

            byte[] actual;
            char[] remaining;

            string actualString;

            (actual, remaining) = base64Converter.Decode(encodedString.ToArray(), 0, encodedString.Length, true, false);
            Assert.That(remaining, Is.Empty);

            actualString = Encoding.UTF8.GetString(actual);
            Assert.That(actualString, Is.EqualTo(expected));
        }

        /// <summary>
        /// Base64 decoding of default test vectors provided by RFC4648
        /// (using default alphabet and NO padding - UTF8 is used to transform 
        /// decoded data into a string for comparison.
        /// </summary>
        /// <param name="encodedString">Teststring to be decoded</param>
        /// <param name="expected">Expected output</param>
        [TestCase("", "")]
        [TestCase("Zg", "f")]
        [TestCase("Zm8", "fo")]
        [TestCase("Zm9v", "foo")]
        [TestCase("Zm9vYg", "foob")]
        [TestCase("Zm9vYmE", "fooba")]
        [TestCase("Zm9vYmFy", "foobar")]
        public void When_DecodingRfcTestVectorsWithDefaultAlphabetAndNoPadding_Expect_RfcResults(string encodedString, string expected)
        {
            Base64Converter base64Converter = new Base64Converter(RFC4648Base64.Default, LineBreakMode.None, -1, false);

            byte[] actual;
            char[] remaining;

            string actualString;

            (actual, remaining) = base64Converter.Decode(encodedString.ToArray(), 0, encodedString.Length, true, false);
            Assert.That(remaining, Is.Empty);

            actualString = Encoding.UTF8.GetString(actual);
            Assert.That(actualString, Is.EqualTo(expected));
        }
    }
}
