using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP
{
    public class SupportedLanguage
    {
        public static readonly SupportedLanguage English = new SupportedLanguage("en");
        public static readonly SupportedLanguage Russian = new SupportedLanguage("ru");
        public static readonly SupportedLanguage German = new SupportedLanguage("de");
        public static readonly SupportedLanguage Portuguese = new SupportedLanguage("pt");
        public static readonly SupportedLanguage PortugueseBrazil = new SupportedLanguage("pt-BR");
        public static readonly SupportedLanguage Spanish = new SupportedLanguage("es");
        public static readonly SupportedLanguage French = new SupportedLanguage("fr");
        public static readonly SupportedLanguage Italian = new SupportedLanguage("it");
        public static readonly SupportedLanguage Dutch = new SupportedLanguage("nl");
        public static readonly SupportedLanguage Japanese = new SupportedLanguage("ja");
        public static readonly SupportedLanguage ChineseChina = new SupportedLanguage("zh-CN");
        public static readonly SupportedLanguage ChineseHongKong = new SupportedLanguage("zh-HK");
        public static readonly SupportedLanguage ChineseTaiwan = new SupportedLanguage("zh-TW");

        private static readonly SupportedLanguage[] AllLangs =
        {
                English,
                Russian,
                German,
                Portuguese,
                PortugueseBrazil,
                Spanish,
                French,
                Italian,
                Dutch,
                Japanese,
                ChineseChina,
                ChineseHongKong,
                ChineseTaiwan
        };

        public readonly string code;

        private SupportedLanguage(string code)
        {
            this.code = code;
        }

        public static SupportedLanguage FromLanguageTag(string languageTag)
        {
            foreach (var item in AllLangs)
            {
                if (string.Equals(item.code, languageTag, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return English;
        }
    }
}
