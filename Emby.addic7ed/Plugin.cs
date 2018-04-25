using System;
using MediaBrowser.Common.Plugins;


namespace Emby.addic7ed
{
    public class Plugin : BasePlugin
    {
        public override string Name => StaticName;

        public static string StaticName => "Addic7ed";

        public override string Description => "Download subtitles from Addic7ed";

        public override Guid Id => new Guid("3467D851-8F1A-4D80-A734-9BB7CB7C0AC2");
    }
}
