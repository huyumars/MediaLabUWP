using MediaLib.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaLib.Config
{
    public interface IRootConfig : IConfig
    {
        MediaType type { get; }
        string name { get; set; }

        IConfigHelper getConfigHelper();

        IO.IMediaRootManager buildRootMangerFromConfig();
        bool valid();
    }

    public abstract class RootConfig<T> : IRootConfig, IO.IMediaTravelConfig where T : Lib.Media
    {
        public string name { set; get; }
        public string dirName { set; get; }
        public MediaType type
        {
            get
            {
                string prefix = typeof(T).ToString().Substring(13);
                return (MediaType)Enum.Parse(typeof(MediaType), prefix);
            }
        }
        public Dictionary<String, T> mediaDic;
        public string configFilePath()
        {
            string prefix = typeof(T).ToString().Substring(13);
            
            return Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\UserData\\" + prefix + "RootConfig.json";
            // return "./UserData/" + prefix + "RootConfig.json";
        }

        public virtual bool valid()
        {
            return name != null;
        }
        public abstract MediaFileTraveler getFileTraveler();
        public abstract IConfigHelper getConfigHelper();

        public IMediaRootManager buildRootMangerFromConfig()
        {
            return new IO.MediaRootManager<T>(this);
        }
    }
}
