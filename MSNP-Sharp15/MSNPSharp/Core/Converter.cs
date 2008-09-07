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