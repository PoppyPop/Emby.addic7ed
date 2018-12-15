using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Model.Serialization;

namespace Emby.addic7ed.Model
{
    public class RemoteSrt
    {
        public string ShowName { get; set; }

        public long SeasonId { get; set; }

        public long EpisodeId { get; set; }

        public string RemoteId { get; set; }

        public string RemoteUrl { get; set; }

        public string LanguageCode { get; set; }

        public string Release { get; set; }

        public string Name
        {
            get { return Release + " | " + LanguageCode; }
        }

        public string LongLanguage
        {
            set
            {
                switch (value)
                {
                    case "French":
                        LanguageCode = "fre";
                        break;
                    case "English":
                        LanguageCode = "eng";
                        break;
                    default:
                        LanguageCode = "oth";
                        break;
                }
            }
        }

        public string EncodeId(IJsonSerializer json)
        {
           return Convert.ToBase64String(Encoding.Unicode.GetBytes(json.SerializeToString(this)));
        }

        public static RemoteSrt DecodeId(string id, IJsonSerializer json)
        {
            byte[] decodedBytes = Convert.FromBase64String(id);
            var str = Encoding.Unicode.GetString(decodedBytes);

            return json.DeserializeFromString<RemoteSrt>(str);
        }

        public string GetReferer()
        {
            return string.Concat("/serie/", this.ShowName, "/", SeasonId, "/", EpisodeId, "/0");
        }
    }
}
