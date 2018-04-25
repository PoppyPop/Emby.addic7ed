using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Emby.addic7ed.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

namespace Emby.addic7ed.Data
{
    internal class CacheStorage
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;

        private string CacheFolder => Path.Combine(_appPaths.CachePath, Plugin.StaticName);

        private string SeriesCacheFile => Path.Combine(CacheFolder, "series.json");
        private string EpisodesCacheFile => Path.Combine(CacheFolder, "episodes.json");

        public CacheStorage(IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _json = json;

            fileSystem.CreateDirectory(CacheFolder);

            if (!File.Exists(SeriesCacheFile))
            {
                using (File.Create(SeriesCacheFile))
                {
                }
            }

            if (!File.Exists(EpisodesCacheFile))
            {
                using (File.Create(EpisodesCacheFile))
                {
                }
            }
        }

        public List<RemoteSeries> GetSeries
        {
            get
            {
                List<RemoteSeries> result = null;

                if (File.Exists(SeriesCacheFile))
                {
                    result = _json.DeserializeFromFile<List<RemoteSeries>>(SeriesCacheFile);
                }

                return result ?? new List<RemoteSeries>();
            }
        }

        public List<RemoteEpisode> GetEpisodes
        {
            get
            {
                List<RemoteEpisode> result = null;

                if (File.Exists(EpisodesCacheFile))
                {
                    result = _json.DeserializeFromFile<List<RemoteEpisode>>(EpisodesCacheFile);
                }

                return result ?? new List<RemoteEpisode>();
            }
        }

        public void Save(List<RemoteSeries> series)
        {
            _json.SerializeToFile(series, SeriesCacheFile);
        }

        public void Save(List<RemoteEpisode> episodes)
        {
            _json.SerializeToFile(episodes, EpisodesCacheFile);
        }
    }
}
