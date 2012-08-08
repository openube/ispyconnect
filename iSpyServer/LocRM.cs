using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace iSpyServer
{
    public static class LocRM
    {

        public static string GetString(string Identifier)
        {
            string _lang = iSpyServer.Default.Language;
            if (_lang == "NotSet")
            {
                _lang = System.Globalization.CultureInfo.CurrentCulture.Name.Split('-')[0];
                if (TranslationSets.FirstOrDefault(p => p.CultureCode == _lang) != null)
                    iSpyServer.Default.Language = _lang;
                else
                    iSpyServer.Default.Language = _lang = "en";
            }

            TranslationsTranslationSetTranslation _t = TranslationSets.First(p => p.CultureCode == _lang).Translation.FirstOrDefault(p => p.Token == Identifier);
            if (_t != null)
            {
                return _t.Value;//.Replace("&amp;", "&").Replace(@"\n", Environment.NewLine).Replace("&lt;", "<").Replace("&gt;", ">");
            }
            return "!" + Identifier + "!";
        }

        public static List<TranslationsTranslationSet> TranslationSets
        {
            get
            {
                return TranslationsList.TranslationSet.ToList();
            }
        }

        
        private static Translations  _TranslationsList;
        public static Translations TranslationsList
        {
            get
            {
                if (_TranslationsList != null)
                    return _TranslationsList;
                Translations _t = new Translations();
                XmlSerializer _s = new XmlSerializer(typeof(Translations));
                FileStream _fs = new FileStream(Program.AppPath + @"\XML\Translations.xml", FileMode.Open);
                TextReader reader = new StreamReader(_fs);
                _fs.Position = 0;
                _t = (Translations)_s.Deserialize(reader);
                _fs.Close();
                reader.Dispose();
                _fs.Dispose();
                _s = null;
                _TranslationsList = _t;

                //decode
                foreach (TranslationsTranslationSet _set in _t.TranslationSet)
                {
                    foreach (TranslationsTranslationSetTranslation _tran in _set.Translation)
                    {
                        _tran.Value = _tran.Value.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
                    }
                }
                return _TranslationsList;
            }
            set
            {
                _TranslationsList = value;
            }
        }
    }
}
