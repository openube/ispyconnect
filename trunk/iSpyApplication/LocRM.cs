using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace iSpyApplication
{
    public static class LocRm
    {
        private static Translations _translationsList;
        public static TranslationsTranslationSet CurrentSet;
        private static Dictionary<string, string> Res = new Dictionary<string, string>();

        public static List<TranslationsTranslationSet> TranslationSets
        {
            get { return TranslationsList.TranslationSet.ToList(); }
        }

        public static Translations TranslationsList
        {
            get
            {
                if (_translationsList != null)
                    return _translationsList;
                var s = new XmlSerializer(typeof (Translations));
                using (var fs = new FileStream(Program.AppDataPath + @"\XML\Translations.xml", FileMode.Open))
                {
                    fs.Position = 0;
                    using (TextReader reader = new StreamReader(fs))
                    {
                        _translationsList = (Translations)s.Deserialize(reader);
                        reader.Close();
                    }
                    fs.Close();
                }

                return _translationsList;
            }
            set { 
                _translationsList = value;
                CurrentSet = null;
            }
        }

        public static string GetString(string identifier)
        {
            string lang = MainForm.Conf.Language;
            if (lang == "NotSet")
            {
                lang = CultureInfo.CurrentCulture.Name.ToLower();
                string lang1 = lang;
                if (TranslationSets.FirstOrDefault(p => p.CultureCode == lang1) != null)
                    MainForm.Conf.Language = lang;
                else
                {
                    lang = lang.Split('-')[0];
                    string lang2 = lang;
                    if (TranslationSets.FirstOrDefault(p => p.CultureCode == lang2) != null)
                        MainForm.Conf.Language = lang;
                    else
                        MainForm.Conf.Language = lang = "en";
                }
            }

            
            if (CurrentSet == null)
            {
                Res.Clear();
                CurrentSet = TranslationSets.FirstOrDefault(p => p.CultureCode == lang);
                if (CurrentSet != null)
                    foreach (TranslationsTranslationSetTranslation tran in CurrentSet.Translation)
                    {
                        Res.Add(tran.Token,tran.Value.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("，", ","));
                    }
            }
            try
            {
                return Res[identifier];
            }
            catch
            {
                //possible threading error where language is reset
            }
            return "!" + identifier + "!";
        }


    }
}