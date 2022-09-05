using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Nikse.SubtitleEdit.Logic.DetectEncoding;

namespace SubTran
{
    class EncodingLib
    {
        private static bool IsUtf8(byte[] buffer)
        {
            int utf8Count = 0;
            int i = 0;
            while (i < buffer.Length - 3)
            {
                byte b = buffer[i];
                if (b > 127)
                {
                    if (b >= 194 && b <= 223 && buffer[i + 1] >= 128 && buffer[i + 1] <= 191)
                    { // 2-byte sequence
                        utf8Count++;
                        i++;
                    }
                    else if (b >= 224 && b <= 239 && buffer[i + 1] >= 128 && buffer[i + 1] <= 191 &&
                                                     buffer[i + 2] >= 128 && buffer[i + 2] <= 191)
                    { // 3-byte sequence
                        utf8Count++;
                        i += 2;
                    }
                    else if (b >= 240 && b <= 244 && buffer[i + 1] >= 128 && buffer[i + 1] <= 191 &&
                                                     buffer[i + 2] >= 128 && buffer[i + 2] <= 191 &&
                                                     buffer[i + 3] >= 128 && buffer[i + 3] <= 191)
                    { // 4-byte sequence
                        utf8Count++;
                        i += 3;
                    }
                    else
                    {
                        return false;
                    }
                }
                i++;
            }
            if (utf8Count == 0)
                return false; // not utf-8

            return true;
        }
        private static int GetCount(string text,
                            string word1,
                            string word2,
                            string word3,
                            string word4,
                            string word5,
                            string word6)
        {
            var regEx1 = new Regex("\\b" + word1 + "\\b");
            var regEx2 = new Regex("\\b" + word2 + "\\b");
            var regEx3 = new Regex("\\b" + word3 + "\\b");
            var regEx4 = new Regex("\\b" + word4 + "\\b");
            var regEx5 = new Regex("\\b" + word5 + "\\b");
            var regEx6 = new Regex("\\b" + word6 + "\\b");
            int count = regEx1.Matches(text).Count;
            count += regEx2.Matches(text).Count;
            count += regEx3.Matches(text).Count;
            count += regEx4.Matches(text).Count;
            count += regEx5.Matches(text).Count;
            count += regEx6.Matches(text).Count;
            return count;
        }
        public static Encoding GetEncodingFromFile(string fileName)
        {
            Encoding encoding = Encoding.Default;
            try
            {
                var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                var bom = new byte[12]; // Get the byte-order mark, if there is one
                file.Position = 0;
                file.Read(bom, 0, 12);
                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                    encoding = Encoding.UTF8;
                else if (bom[0] == 0xff && bom[1] == 0xfe)
                    encoding = Encoding.Unicode;
                else if (bom[0] == 0xfe && bom[1] == 0xff) // utf-16 and ucs-2
                    encoding = Encoding.BigEndianUnicode;
                else if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) // ucs-4
                    encoding = Encoding.UTF32;
                else if (encoding == Encoding.Default && file.Length > 12)
                {
                    int length = (int)file.Length;
                    if (length > 100000)
                        length = 100000;

                    file.Position = 0;
                    var buffer = new byte[length];
                    file.Read(buffer, 0, length);

                    if (IsUtf8(buffer))
                    {
                        encoding = Encoding.UTF8;
                    }
                    else 
                    {
                        encoding = EncodingTools.DetectInputCodepage(buffer);

                        Encoding greekEncoding = Encoding.GetEncoding(1253); // Greek
                        if (GetCount(greekEncoding.GetString(buffer), "μου", "είναι", "Είναι", "αυτό", "Τόμπυ", "καλά") > 5)
                            return greekEncoding;

                        Encoding russianEncoding = Encoding.GetEncoding(1251); // Russian
                        if (GetCount(russianEncoding.GetString(buffer), "что", "быть", "весь", "этот", "один", "такой") > 5)
                            return russianEncoding;
                        russianEncoding = Encoding.GetEncoding(28595); // Russian
                        if (GetCount(russianEncoding.GetString(buffer), "что", "быть", "весь", "этот", "один", "такой") > 5)
                            return russianEncoding;

                        Encoding arabicEncoding = Encoding.GetEncoding(28596); // Arabic
                        Encoding hewbrewEncoding = Encoding.GetEncoding(28598); // Hebrew
                        if (GetCount(arabicEncoding.GetString(buffer), "من", "هل", "لا", "فى", "لقد", "ما") > 5)
                        {
                            if (GetCount(hewbrewEncoding.GetString(buffer), "אולי", "אולי", "אולי", "אולי", "טוב", "טוב") > 10)
                                return hewbrewEncoding;
                            return arabicEncoding;
                        }
                        else if (GetCount(hewbrewEncoding.GetString(buffer), "אתה", "אולי", "הוא", "בסדר", "יודע", "טוב") > 5)
                            return hewbrewEncoding;
                    }
                }
                file.Close();
                file.Dispose();
            }
            catch
            {

            }
            return encoding;
        }
    }
}
