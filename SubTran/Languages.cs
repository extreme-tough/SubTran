using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Languages
{
    public static class Languages
    {
        public static Dictionary<string, string> _languageModeMap = new Dictionary<string, string>()
        { 
            {"Afrikaans", "af"},
            {"Albanian", "sq"},
            {"Arabic", "ar"},
            {"Armenian", "hy"},
            {"Azerbaijani", "az"},
            {"Basque", "eu"},
            {"Belarusian", "be"},
            {"Bengali", "bn"},
            {"Bulgarian", "bg"},
            {"Catalan", "ca"},
            {"Chinese", "zh-CN"},
            {"Croatian", "hr"},
            {"Czech", "cs"},
            {"Danish", "da"},
            {"Dutch", "nl"},
            {"English", "en"},
            {"Estonian", "et"},
            {"Filipino", "tl"},
            {"Finnish", "fi"},
            {"French", "fr"},
            {"Galician", "gl"},
            {"Georgian", "ka"},
            {"German", "de"},
            {"Greek", "el"},
            {"Gujarati", "gu"},
            {"Haitian Creole ALPHA", "ht"},
            {"Hebrew", "iw"},
            {"Hindi", "hi"},
            {"Hungarian", "hu"},
            {"Icelandic", "is"},
            {"Indonesian", "id"},
            {"Irish", "ga"},
            {"Italian", "it"},
            {"Japanese", "ja"},
            {"Kannada", "kn"},
            {"Korean", "ko"},
            {"Latvian", "lv"},
            {"Lithuanian", "lt"},
            {"Macedonian", "mk"},
            {"Malay", "ms"},
            {"Maltese", "mt"},
            {"Norwegian", "no"},
            {"Persian", "fa"},
            {"Polish", "pl"},
            {"Portuguese", "pt"},
            {"Romanian", "ro"},
            {"Russian", "ru"},
            {"Serbian", "sr"},
            {"Slovak", "sk"},
            {"Slovenian", "sl"},
            {"Spanish", "es"},
            {"Swahili", "sw"},
            {"Swedish", "sv"},
            {"Tamil", "ta"},
            {"Telugu", "te"},
            {"Thai", "th"},
            {"Turkish", "tr"},
            {"Ukrainian", "uk"},
            {"Vietnamese", "vi"},
            {"Welsh", "cy"},
            {"Yiddish", "yi"},

        
        };

        /// <summary>
        /// Converts a language to its identifier.
        /// </summary>
        /// <param name="language">The language."</param>
        /// <returns>The identifier or <see cref="string.Empty"/> if none.</returns>
        /// 
        public static string LanguageEnumToIdentifier (string language)
        {
            string mode = string.Empty;
            _languageModeMap.TryGetValue(language, out mode);
            return mode;
        }

        public static CultureInfo GetLanguageCulture(string lang)
        {
            object val = lang;
            try
            {
                return CultureInfo.GetCultureInfoByIetfLanguageTag(lang);
            }
            catch
            {
                foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
                {
                    if (ci.TwoLetterISOLanguageName == lang)
                    {
                        return ci;
                    }
                }
            }
            throw new ArgumentException("lang", "Unknwon language");
        }
    }
}
