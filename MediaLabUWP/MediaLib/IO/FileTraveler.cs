using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaLib
{
    namespace IO
    {
        public delegate void MediaDirHandler(DirectoryInfo dirName);
        public delegate void MediaTemplateHandler(String fileName);
       public interface IMediaTravelConfig {
            String dirName { get; set; }
            MediaFileTraveler getFileTraveler();
        }
        public abstract class  MediaFileTraveler
        {
            protected IMediaTravelConfig config;
            protected MediaDirHandler handler;
            public MediaFileTraveler(IMediaTravelConfig _config) { config = _config; }
            public abstract void travel(MediaDirHandler handler);
            public MediaTemplateHandler templateHandler;
            public abstract bool isValid(string dir);
        }


        public interface IFixDepthFileTravelerConfig : IMediaTravelConfig
        {
             int mediaFileExistDirLevel { set; get; }
        }

        public class FixDepthFileTraveler : MediaFileTraveler
        {
            public FixDepthFileTraveler(IFixDepthFileTravelerConfig rootConfig):base(rootConfig) {  }
            
            public override void travel(MediaDirHandler _handler)
            {
                handler = _handler;
                DirectoryInfo dirInfo = new DirectoryInfo(config.dirName);
                _travel(dirInfo);
            }
            private void _travel(DirectoryInfo dir, int depth = 0)
            {
                //find template
                if (templateHandler!=null && File.Exists(dir.FullName + "\\template.json"))
                    templateHandler(dir.FullName + "\\template.json");

                if (depth == (config as IFixDepthFileTravelerConfig).mediaFileExistDirLevel)
                {
                    handler(dir);
                    return;
                }
                foreach (DirectoryInfo NextFolder in dir.GetDirectories())
                {
                    _travel(NextFolder, depth + 1);
                }

                //end find template
                if (templateHandler != null && File.Exists(dir.FullName + "\\template.json"))
                    templateHandler(dir.FullName + "\\template.json");
            }

            public override bool isValid(string dir)
            {
                string folder = dir;
                for (int i = 0; i < (config as IFixDepthFileTravelerConfig).mediaFileExistDirLevel; i++)
                {
                    folder = Path.GetDirectoryName(folder);
                }
                if (folder == config.dirName)
                    return true;
                return false;
            }

            public void init(IMediaTravelConfig config)
            {
                throw new NotImplementedException();
            }
        }
    }
}