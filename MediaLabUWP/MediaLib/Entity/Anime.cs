using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MediaLib.Config;
using MediaLib.IO;
using MediaLib.Resource;

namespace MediaLib
{
    namespace Lib
    {
        class AnimeRootConfig : Config.RootConfig<Anime>, IO.IFixDepthFileTravelerConfig
        {

            public int mediaFileExistDirLevel { get; set; }

            public override IConfigHelper getConfigHelper()
            {
                return Config.ConfigHelper<AnimeRootConfig>.instance;
            }

            public override MediaFileTraveler getFileTraveler()
            {
                return new IO.FixDepthFileTraveler(this);
            }

            public override bool valid()
            {
                return base.valid()&&mediaFileExistDirLevel>0&& System.IO.Directory.Exists(dirName) && Config.ConfigHelper<AnimeRootConfig>.instance.Config(dirName) == null;
            }
        }

        public enum EvalType{
            Default=0,
            OneStar,
            TwoStars,
            ThreeStars,
            FourStars,
            FiveStars,
        }

        [DataContract]
        public class Anime : Media
        {

            private const byte fullStar = (byte)EvalType.FiveStars;

            [DataMember]
            public EvalType star { get; set; }
            public static ImgManager _imgMgr { get {
                    var imgMgrConfig = Config.ConfigHelper<Config.ResourceConfig>.instance.Config("LargeImgResource");

                    var defaultConfig =  Config.ConfigHelper<Config.ResourceConfig>.instance.Config("LargeImgResourceDefault");

                    if (imgMgrConfig == null)
                        return null;
                    var mgr = new Resource.ImgManager(imgMgrConfig);
                    if (defaultConfig!=null && System.IO.File.Exists(defaultConfig.storePath))
                        mgr.defaultImage = defaultConfig.storePath;
                    return mgr;
                } }
            public Anime(String _UID, System.IO.DirectoryInfo dirPath) : base(_UID, dirPath)
            {
                star = 0;
                if(title!=null)
                    title = refineTitle(title);
            }

            public override string contentDir { get { return base.contentDir;  }
                set
                {
                    base.contentDir = value;
                    title = refineTitle(mPath.Name);
                }
            }

            static ImgManager mgr = _imgMgr;
            public override ImgManager imgMgr  { get {  return mgr; } }

            // try to remove symbol in () []
            // if failed return orignal one
            static Dictionary<char, char> brackets = new Dictionary<char, char>();

            public String starStr {
                get{
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < (byte)star; ++i)
                        sb.Append("★");
                    for (int i = 0; i < fullStar - (byte)star; i++)
                        sb.Append("☆");
                    return sb.ToString();
                }
            }
            private String refineTitle(String t)
            {
                StringBuilder sb = new StringBuilder();
                Stack<char> bracketStack = new Stack<char>();
                brackets[')'] = '(';
                brackets[']'] = '[';
                foreach(char c in t)
                {
                    if (brackets.Values.Contains(c))
                        bracketStack.Push(c);
                    else if (brackets.Keys.Contains(c))
                    {
                        if(bracketStack.Count()>0&& brackets[c] == bracketStack.Peek())
                        {
                            bracketStack.Pop();
                        }
                        else
                        {
                            //can not match brackets
                            return t;
                        }
                    }
                    else if(bracketStack.Count==0)
                    {
                        sb.Append(c);
                    }
                }
                //can not match all brackets
                if (bracketStack.Count != 0 || sb.Length==0)
                    return t;
                return sb.ToString();
                
            }
        }
    }
    
}
