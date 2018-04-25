using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.addic7ed.Model
{
    internal class RemoteSeries
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public List<RemoteEpisode> Episodes { get; set; }
    }
}
