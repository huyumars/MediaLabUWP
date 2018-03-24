using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaLib.IO;

namespace MediaLib
{
    namespace Config
    {

        public enum MediaType
        {
            Anime,
            Music
           
        }

        public interface IConfig
        {
             String configFilePath();
        }     

        

        public enum ResourceType
        {
            LargeImage,
        }
        public class ResourceConfig: IConfig
        {
            public string resouceType;
            public string mediaType;
            public string storePath;
            public ResourceType resouceType_t { get { return (ResourceType)Enum.Parse(typeof(ResourceType), resouceType); } }
            public MediaType mediaType_t { get { return (MediaType)Enum.Parse(typeof(MediaType), mediaType); } }
            public string configFilePath()
            {
                return Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\Config\\ResourceConfig.json";
                return "./Config/ResourceConfig.json";
            }
        }

    }
}
