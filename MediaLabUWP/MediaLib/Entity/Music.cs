using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaLib.Config;
using MediaLib.IO;
using MediaLib.Resource;

namespace MediaLib.Lib
{
    public class MusicRootConfig : Config.RootConfig<Music>, IO.IFixDepthFileTravelerConfig
    {
        public int mediaFileExistDirLevel { get; set; }
        

        public override IConfigHelper getConfigHelper()
        {
            return Config.ConfigHelper<MusicRootConfig>.instance;
        }

        public override MediaFileTraveler getFileTraveler()
        {
            return new IO.FixDepthFileTraveler(this);
        }
    }
    public class Music : Media
    {
        public static ImgManager _imgMgr
        {
            get
            {
                var imgMgrConfig = Config.ConfigHelper<Config.ResourceConfig>.instance.Config("LargeImgResource");

                var defaultConfig = Config.ConfigHelper<Config.ResourceConfig>.instance.Config("LargeImgResourceDefault");

                if (imgMgrConfig == null)
                    return null;
                var mgr = new Resource.ImgManager(imgMgrConfig);
                if (defaultConfig != null && System.IO.File.Exists(defaultConfig.storePath))
                    mgr.defaultImage = defaultConfig.storePath;
                return mgr;
            }
        }

        static ImgManager mgr = _imgMgr;

        public Music(string _UID, DirectoryInfo dirPath) : base(_UID, dirPath)
        {
        }

        public override ImgManager imgMgr
        {
            get
            {
                return mgr;
            }
        }
    }
}
