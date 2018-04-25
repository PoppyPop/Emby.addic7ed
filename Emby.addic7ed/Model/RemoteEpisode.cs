using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.addic7ed.Model
{
    public class RemoteEpisode
    {
        public long ShowId { get; set; }

        public long SeasonId { get; set; }

        public long Id { get; set; }

        public string RemoteId { get; set; }

        public RemoteEpisode(string remoteId, long showId, long seasonId)
        {
            RemoteId = remoteId;
            ShowId = showId;
            SeasonId = seasonId;
            // key is {showId}-{seasonId}x{Id}
            Id = int.Parse(remoteId.Substring(showId.ToString().Length + 1 + seasonId.ToString().Length + 1));
        }
    }
}
