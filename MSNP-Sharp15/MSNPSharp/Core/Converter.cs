#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Text;
using System.Web;

namespace MSNPSharp.Core
{

    /// <summary>
    /// Provides methods for encoding and decoding URLs when processing Web requests. This class cannot be inherited. 
    /// </summary>
    public sealed class MSNHttpUtility
    {
        /// <summary>
        /// Encodes a URL string using UTF-8 encoding by default.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8);
        }

        /// <summary>
        /// Encodes a URL string using the specified encoding object.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <param name="e">The <see cref="Encoding"/> object that specifies the encoding scheme. </param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string str, Encoding e)
        {
            string tmp = HttpUtility.UrlEncode(str, e);
            tmp = tmp.Replace("+", "%20");
            tmp = tmp.Replace("%7B", "&#x7B;");
            tmp = tmp.Replace("%7b", "&#x7b;");
            tmp = tmp.Replace("%7D", "&#x7D;");
            tmp = tmp.Replace("%7d", "&#x7d;");
            return tmp;
        }

        /// <summary>
        /// Converts a string that has been encoded for transmission in a URL into a decoded string using UTF-8 encoding by default.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string str)
        {
            return UrlDecode(str, Encoding.UTF8);
        }

        /// <summary>
        /// Converts a URL-encoded string into a decoded string, using the specified encoding object.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <param name="e">The <see cref="Encoding"/> that specifies the decoding scheme.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string str, Encoding e)
        {
            return HttpUtility.UrlDecode(str.Replace("%20", "+"), e);
        }

        /// <summary>
        /// Decode the QP encoded string.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string QPDecode(string str)
        {
            return QPDecode(str, Encoding.Default);
        }

        /// <summary>
        /// Decode the QP encoded string using an encoding
        /// </summary>
        /// <param name="value">The string to decode.</param>
        /// <param name="encode">The <see cref="Encoding"/> that specifies the decoding scheme.</param>
        /// <returns>A decoded string.</returns>
        public static string QPDecode(string value, Encoding encode)
        {
            string inputString = value;
            StringBuilder builder1 = new StringBuilder();
            inputString = inputString.Replace("=\r\n", "");
            for (int num1 = 0; num1 < inputString.Length; num1++)
            {
                if (inputString[num1] == '=')
                {
                    try
                    {
                        if (HexToDec(inputString.Substring(num1 + 1, 2)) < 0x80)
                        {
                            if (HexToDec(inputString.Substring(num1 + 1, 2)) >= 0)
                            {
                                byte[] buffer1 = new byte[1] { (byte)HexToDec(inputString.Substring(num1 + 1, 2)) };
                                builder1.Append(encode.GetString(buffer1));
                                num1 += 2;
                            }
                        }
                        else if (inputString[num1 + 1] != '=')
                        {
                            byte[] buffer2 = new byte[2] { (byte)HexToDec(inputString.Substring(num1 + 1, 2)), (byte)HexToDec(inputString.Substring(num1 + 4, 2)) };
                            builder1.Append(encode.GetString(buffer2));
                            num1 += 5;
                        }
                    }
                    catch
                    {
                        builder1.Append(inputString.Substring(num1, 1));
                    }
                }
                else
                {
                    builder1.Append(inputString.Substring(num1, 1));
                }
            }
            return builder1.ToString();
        }

        private static int HexToDec(string hex)
        {
            int num1 = 0;
            string text1 = "0123456789ABCDEF";
            for (int num2 = 0; num2 < hex.Length; num2++)
            {
                if (text1.IndexOf(hex[num2]) == -1)
                {
                    return -1;
                }
                num1 = (num1 * 0x10) + text1.IndexOf(hex[num2]);
            }
            return num1;
        }

    }
}