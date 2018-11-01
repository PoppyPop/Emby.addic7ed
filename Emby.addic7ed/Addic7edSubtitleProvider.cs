using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.addic7ed.Data;
using Emby.addic7ed.Model;
using HtmlAgilityPack;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.addic7ed
{
    // ReSharper disable once InconsistentNaming
    public class Addic7edSubtitleProvider : ISubtitleProvider, IHasOrder
    {
        public string Name => Plugin.StaticName;

        public int Order => 1;

        public IEnumerable<VideoContentType> SupportedMediaTypes => new List<VideoContentType> { VideoContentType.Episode };

        private readonly string self_release_pattern = "Version (.+), (?:[0-9]+).(?:[0-9]+) MBs";

        private readonly RemoteCall _remote;
        private readonly CacheStorage _cache;
        private readonly IJsonSerializer _json;
        private readonly ILogger _logger;

        public Addic7edSubtitleProvider(IHttpClient httpClient, IFileSystem fileSystem, IApplicationPaths appPaths, IJsonSerializer json, ILogger logger)
        {
            _json = json;
            _remote = new RemoteCall(httpClient);
            _cache = new CacheStorage(json, appPaths, fileSystem);
            _logger = logger;
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await SearchRecursive(request, null);
            }
            catch (Exception e)
            {
                _logger.ErrorException("Error Subtitle", e);
            }

            return new List<RemoteSubtitleInfo>();
        }

        private async Task<IEnumerable<RemoteSubtitleInfo>> SearchRecursive(SubtitleSearchRequest request, bool? recurse)
        {
            var result = new List<RemoteSubtitleInfo>();

            // to far we giveup
            if (request.ContentType == VideoContentType.Episode && request.ParentIndexNumber.HasValue && request.IndexNumber.HasValue)
            {
                var series = request.SeriesName;
                var episodeNumber = request.IndexNumber.Value;
                var seasonId = request.ParentIndexNumber.Value;

                //series = series.ToLower().Replace(" ", "_").Replace("$#*!", "shit")
                //    .Replace("'", ""); // need this for $#*! My Dad Says and That 70s show

                _logger.Error("Search {0} | {1} | {2}", series, episodeNumber, seasonId);

                var cacheSeries = _cache.GetSeries;

                var matchingSeries = cacheSeries.Where(c => c.Levenshtein(series) < 3).ToList();

                if (!matchingSeries.Any())
                {
                    if (!recurse.HasValue)
                    {
                        // No cache entry
                        await GetRemoteSeries();
                        return await SearchRecursive(request, false);
                    }

                    return result;
                }

                if (matchingSeries.Count == 1)
                {
                    var serie = matchingSeries.Single();
                    var cacheEpisodes = _cache.GetEpisodes;

                    var episode = cacheEpisodes.SingleOrDefault(e =>
                        e.ShowId == serie.Id && e.SeasonId == seasonId && e.Id == episodeNumber);

                    if (episode == null)
                    {
                        if (!recurse.HasValue || !recurse.Value)
                        {
                            // No cache entry
                            await GetRemoteEpisodes(serie.Id, seasonId);
                            return await SearchRecursive(request, true);
                        }

                        return result;
                    }

                    var srts = await GetRemoteEpisodeDetail(episode);

                    foreach (var srt in srts)
                    {
                        if (string.IsNullOrEmpty(request.Language) || srt.LanguageCode == request.Language)
                        {
                            result.Add(new RemoteSubtitleInfo
                            {
                                Id = srt.EncodeId(_json),
                                Name = srt.Name,
                                Author = Plugin.StaticName,
                                ProviderName = Plugin.StaticName,
                                ThreeLetterISOLanguageName = srt.LanguageCode
                            });
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<RemoteSrt>> GetRemoteEpisodeDetail(RemoteEpisode episode)
        {
            List<RemoteSrt> srts = new List<RemoteSrt>();

            string html = await _remote.GetRemoteEpisodeDetail(episode).ConfigureAwait(false);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//div")
                .Where(div => div.Id == "container95m");

            foreach (HtmlNode htmlNode in nodes)
            {
                var htmlTitle = htmlNode.Descendants() /*.SelectNodes("//td")*/
                    .Where(n => n.HasClass("NewsTitle") && n.Attributes["colspan"].Value == "3");

                foreach (HtmlNode node in htmlTitle)
                {
                    string name = node.InnerText.Trim();

                    Regex release = new Regex(self_release_pattern);
                    var regMatch = release.Match(name);
                    string releaseTeam = null;
                    if (regMatch.Groups[1].Success)
                    {
                        releaseTeam = regMatch.Groups[1].Value;
                    }

                    var tableRef = node.ParentNode.ParentNode;

                    foreach (var lang in tableRef.Descendants().Where(l => l.HasClass("language")))
                    {
                        var parent = lang.ParentNode;

                        var impaired = parent.NextSibling.NextSibling.ChildNodes.SingleOrDefault(c => c.HasClass("newsDate"))?.InnerHtml
                            .Contains("Hearing Impaired");

                        if (!impaired.HasValue || !impaired.Value)
                        {
                            var langGroup = parent.ChildNodes.Where(c => c.Name == "td").ToList();

                            var status = langGroup[3].InnerText.Trim();

                            if (status == "Completed")
                            {
                                var nbDl = langGroup[4].ChildNodes.Count(c => c.Name == "a");
                                var dlLink = "";
                                if (nbDl == 1)
                                {
                                    dlLink = langGroup[4].ChildNodes.SingleOrDefault(c => c.Name == "a")
                                        ?.Attributes["href"].Value;
                                }
                                else if (nbDl > 1)
                                {
                                    dlLink = langGroup[4].ChildNodes.SingleOrDefault(c => c.Name == "a" && c.InnerText.Contains("most updated"))
                                        ?.Attributes["href"].Value;
                                }

                                srts.Add(new RemoteSrt
                                {
                                    RemoteId = episode.RemoteId,
                                    LongLanguage = lang.InnerText.Trim(),
                                    Release = releaseTeam,
                                    RemoteUrl = dlLink,
                                });

                            }
                        }
                    }
                }
            }

            return srts;
        }

        public async Task GetRemoteEpisodes(long showId, int season)
        {
            List<RemoteEpisode> episodes = new List<RemoteEpisode>();

            string str = await _remote.GetRemoteEpisodes(showId, season).ConfigureAwait(false);

            var doc = new HtmlDocument();
            doc.LoadHtml(str);

            var nodes = doc.DocumentNode.SelectNodes("//select")
                .Where(select => select.Attributes["name"].Value == "qsiEp")
                .SelectMany(y => y.Descendants("option"))
                .ToList();


            foreach (HtmlNode htmlNode in nodes)
            {
                var key = htmlNode.GetAttributeValue("value", "");

                if (key.StartsWith(showId.ToString()))
                {
                    episodes.Add(new RemoteEpisode(key, showId, season));
                }
            }

            _cache.Save(episodes);
        }

        public async Task GetRemoteSeries()
        {
            List<RemoteSeries> remoteSeries = new List<RemoteSeries>();

            string str = await _remote.GetRemoteSeries().ConfigureAwait(false);

            var doc = new HtmlDocument();
            doc.LoadHtml(str);

            var nodes = doc.DocumentNode.SelectNodes("//select")
                .Where(select => select.Attributes["name"].Value == "qsShow")
                .SelectMany(y => y.Descendants("option"))
                .ToList();

            foreach (HtmlNode htmlNode in nodes)
            {
                remoteSeries.Add(new RemoteSeries
                {
                    Id = htmlNode.GetAttributeValue("value", -1),
                    Name = htmlNode.InnerText,
                });
            }

            // Clean fake Id
            remoteSeries.RemoveAll(s => s.Id < 1);

            _cache.Save(remoteSeries);
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var decode = RemoteSrt.DecodeId(id, _json);

            var res = await _remote.GetSubtitles(decode).ConfigureAwait(false);

            return new SubtitleResponse
            {
                Format = res.Value,
                Stream = res.Key,
                Language = decode.LanguageCode,
            };
        }
    }
}
