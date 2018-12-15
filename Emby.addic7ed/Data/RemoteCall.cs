using Emby.addic7ed.Model;
using MediaBrowser.Common.Net;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Emby.addic7ed.Data
{
    internal class RemoteCall
    {
        private readonly IHttpClient _httpClient;

        private const string ServiceUrl = "http://www.addic7ed.com";

        private HttpRequestOptions BaseRequestOptions => new HttpRequestOptions
        {
            UserAgent =
                "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 65.0.3325.181 Safari / 537.36",
            Referer = ServiceUrl
        };

        public RemoteCall(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetRemoteSeries()
        {
            HttpRequestOptions httpOpt = BaseRequestOptions;
            httpOpt.Url = ServiceUrl + "/ajax_getShows.php";

            using (HttpResponseInfo httpResponseInfo = await _httpClient.GetResponse(httpOpt).ConfigureAwait(false))
            {
                using (StreamReader streamReader = new StreamReader(httpResponseInfo.Content))
                {
                    return await streamReader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<string> GetRemoteEpisodes(long showId, long season)
        {
            HttpRequestOptions httpOpt = BaseRequestOptions;
            httpOpt.Url = ServiceUrl + string.Format("/ajax_getEpisodes.php?showID={0}&season={1}", showId, season);

            using (HttpResponseInfo httpResponseInfo = await _httpClient.GetResponse(httpOpt).ConfigureAwait(false))
            {
                using (StreamReader streamReader = new StreamReader(httpResponseInfo.Content))
                {
                    return await streamReader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<string> GetRemoteEpisodeDetail(RemoteEpisode episode)
        {
            HttpRequestOptions httpOpt = BaseRequestOptions;
            httpOpt.Url = ServiceUrl + string.Format("/re_episode.php?ep={0}", episode.RemoteId);

            using (HttpResponseInfo httpResponseInfo = await _httpClient.GetResponse(httpOpt).ConfigureAwait(false))
            {
                using (StreamReader streamReader = new StreamReader(httpResponseInfo.Content))
                {
                    return await streamReader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<KeyValuePair<Stream, string>> GetSubtitles(RemoteSrt srt)
        {
            HttpRequestOptions httpOpt = BaseRequestOptions;
            httpOpt.Referer = ServiceUrl + srt.GetReferer();
            httpOpt.Url = ServiceUrl + srt.RemoteUrl;
            httpOpt.AcceptHeader =
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";

            using (HttpResponseInfo httpResponseInfo = await _httpClient.GetResponse(httpOpt).ConfigureAwait(false))
            {
                string srtType = "srt";
                if (!string.IsNullOrEmpty(httpResponseInfo.ContentType))
                {
                    srtType = httpResponseInfo.ContentType.Substring(5);
                }

                MemoryStream ms = new MemoryStream();
                await httpResponseInfo.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0L;
                return new KeyValuePair<Stream, string>(ms, srtType);
            }

            //    using (HttpResponseInfo httpResponseInfo = await _httpClient.GetResponse(httpOpt).ConfigureAwait(false))
            //{
            //    using (StreamReader streamReader = new StreamReader(httpResponseInfo.Content))
            //    {
            //        var resulaty = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            //    }
            //}

            //return null;
        }
    }
}
