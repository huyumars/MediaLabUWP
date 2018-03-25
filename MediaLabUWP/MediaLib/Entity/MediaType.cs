using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MediaLib
{
    namespace Lib
    {
        
        [DataContract]
        public abstract class Media
        {
            Config.MediaType _type;
            public Media() { }
            public Media(String _UID, String dirPath)
            {
                _contenDir = dirPath;
                UID = _UID;
                title = System.IO.Path.GetFileName(dirPath);
                enable = true;
                String prefix = this.GetType().ToString().Substring(13);
                _type = (Config.MediaType)Enum.Parse(typeof(Config.MediaType), prefix);
            }
            public Media(String _UID, System.IO.DirectoryInfo dirPath)
            {
                mPath = dirPath;
                UID = _UID;
                if(dirPath!=null)
                    title = dirPath.Name;
                enable = true;
                String prefix  = this.GetType().ToString().Substring(13);
                _type = (Config.MediaType)Enum.Parse(typeof(Config.MediaType), prefix);
            }

            protected System.IO.DirectoryInfo mPath;

           String  _contenDir;
            [DataMember]
            public virtual String contentDir { get => _contenDir; set { _contenDir = value; } }

            [DataMember]
            public String UID { get; set; }

            [DataMember]
            public  String title { get; set; }

            [DataMember]
            public EvalType star { get; set; }

            public bool enable;

            public Config.MediaType type { get {

                    return _type;
                } }

            public static Media MediaFactory(String dirPath, String UID, Config.MediaType type)
            {
                Assembly assembly = Assembly.GetExecutingAssembly(); // 获取当前程序集
                Type t = Type.GetType("MediaLib.Lib." + type);
                try
                {
                    Media entity = (Media)Activator.CreateInstance(t, new object[] { UID, dirPath });// 创建类的实例
                    return entity;
                }
                catch (Exception ex)
                {
                    Logger.ERROR(ex.Message);
                }
                return null;
            }
            public static Media MediaFactory(System.IO.DirectoryInfo dirInfo, String UID, Config.MediaType type)
            {
                Assembly assembly = Assembly.GetExecutingAssembly(); // 获取当前程序集
                Type t = Type.GetType("MediaLib.Lib." + type);
                try
                {
                    Media entity = (Media)Activator.CreateInstance(t, new object[] { UID,dirInfo } );// 创建类的实例
                    return entity;
                }
                catch(Exception ex)
                {
                    Logger.ERROR(ex.Message);
                }
                return null;
            }
            public String this[String attr]
            {
                get
                {
                    Type type = this.GetType();
                    PropertyInfo pi = type.GetProperties().FirstOrDefault(x => x.Name == attr);
                    if (pi != null) return pi.GetValue(this, null).ToString();
                    else return "";
                }
            }

            public abstract Resource.ImgManager imgMgr { get; }
        }
    }
}
