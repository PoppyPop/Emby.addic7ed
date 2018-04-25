using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.addic7ed.Model
{
    public class RemoteSrt
    {
        public string RemoteUrl { get; set; }

        public string RemoteUrlEncoded => Convert.ToBase64String(Encoding.Unicode.GetBytes(LanguageCode + "|" + RemoteUrl));

        public string LanguageCode { get; set; }

        public string Release { get; set; }

        public string Name { get; set; }

        public string LongLanguage
        {
            set
            {
                switch (value)
                {
                    case "French":
                        LanguageCode = "FRE";
                        break;
                    case "English":
                        LanguageCode = "ENG";
                        break;
                }
            }
        }

        public static KeyValuePair<string, string> DecodeId(string id)
        {
            byte[] decodedBytes = Convert.FromBase64String(id);
            var str = Encoding.Unicode.GetString(decodedBytes);

            var split = str.Split('|');

            return new KeyValuePair<string, string>(split[1], split[0]);
        }
    }
}
