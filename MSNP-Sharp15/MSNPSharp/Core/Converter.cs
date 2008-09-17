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

namespace MSNPSharp.Core
{
    public static class Converter
    {
        /// <summary>
        /// Decode the QP encoded string using an encoding
        /// </summary>
        /// <param name="quoted_printableString"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string ConvertFromQPString(string quoted_printableString, Encoding encode)
        {
            string InputString = quoted_printableString;
            StringBuilder builder1 = new StringBuilder();
            InputString = InputString.Replace("=\r\n", "");
            for (int num1 = 0; num1 < InputString.Length; num1++)
            {
                if (InputString[num1] == '=')
                {
                    try
                    {
                        if (HexToDec(InputString.Substring(num1 + 1, 2)) < 0x80)
                        {
                            if (HexToDec(InputString.Substring(num1 + 1, 2)) >= 0)
                            {
                                byte[] buffer1 = new byte[1] { (byte)HexToDec(InputString.Substring(num1 + 1, 2)) };
                                builder1.Append(encode.GetString(buffer1));
                                num1 += 2;
                            }
                        }
                        else if (InputString[num1 + 1] != '=')
                        {
                            byte[] buffer2 = new byte[2] { (byte)HexToDec(InputString.Substring(num1 + 1, 2)), (byte)HexToDec(InputString.Substring(num1 + 4, 2)) };
                            builder1.Append(encode.GetString(buffer2));
                            num1 += 5;
                        }
                    }
                    catch
                    {
                        builder1.Append(InputString.Substring(num1, 1));
                    }
                }
                else
                {
                    builder1.Append(InputString.Substring(num1, 1));
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