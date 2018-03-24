using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;

namespace MediaLib
{
    namespace Config
    {
        public class JSONHelper
        {
            //序列化
            //对象转化为Json字符串
            public static string Serialize(object objectToSerialize)
            {
                return  JsonConvert.SerializeObject(objectToSerialize, Newtonsoft.Json.Formatting.Indented);       
            }

            //反序列化
            //Json字符串转化为对象
            public static T Deserialize<T>(string jsonString)
            {
                    return JsonConvert.DeserializeObject<T>(jsonString);
              
            }

            public static T readFromJsonFile<T>(string path)
            {
                try
                {
                    StreamReader sr = new StreamReader(path, System.Text.Encoding.GetEncoding("utf-8"));
                    string content = sr.ReadToEnd().ToString();
                    sr.Close();
                    Logger.INFO("read " + path + " sucessfully");
                    return  JSONHelper.Deserialize<T>(content);
                }
                catch (System.Exception ex)
                {
                    Logger.ERROR("can not open config file " + path);
                    Logger.ERROR(ex.Message);                  
                    return default(T);
                }
            }

            public static void SaveAsJsonFile(string path, object objectToSerialize)
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    StreamWriter sw = new StreamWriter(path, false);
                    sw.Write(Serialize(objectToSerialize));
                    sw.Close();
                    Logger.INFO("save "+path+" sucessfully");
                }
                catch (System.Exception ex)
                {
                    Logger.ERROR("can not save config file " + path);
                    Logger.ERROR(ex.Message);
                }
                
            }
        }


        public delegate void ConfigHandler(IConfig config);

        public interface IConfigHelper
        {
            void UpdateConfig(string configName, IConfig config);

            void RemoveConfig(string configName);

            void saveConfig();

            void foreachConfig(ConfigHandler handler);
        }

        public class ConfigHelper<T>: IConfigHelper where T: IConfig, new()
        {

            public class Singleton<S> where S : new()
            {
                /// 对象实例
                private static readonly S Inst = new S();
                /// 获取 类型 单例
                public static S Instance
                {
                    get
                    {
                        return Inst;
                    }
                }
            }

            public static ConfigHelper<T> instance { get { return Singleton<ConfigHelper<T>>.Instance; } }

            Dictionary<string, T> configDic;
            public ConfigHelper()
            {
                string configfilePath = new T().configFilePath();
                Logger.INFO("starting loading config file :" + configfilePath);
                configDic = JSONHelper.readFromJsonFile<Dictionary<string, T>>(configfilePath);
                if (configDic == null) configDic = new Dictionary<string, T>();
                
            }
                         
            public T Config(string configName)
            {
                if (!configDic.ContainsKey(configName))
                    return default(T);
                return configDic[configName];
            }

            public void UpdateConfig(string configName, IConfig config)
            {
                configDic[configName] =(T) config ;
            }

            public void RemoveConfig(string configName)
            {
                configDic.Remove(configName);
            }


            public void saveConfig()
            {
                    JSONHelper.SaveAsJsonFile(new T().configFilePath(), configDic);
            }
                  
            public void foreachConfig(ConfigHandler handler)
            {
                foreach(var config in configDic.Values)
                {
                    handler(config);
                }
            }
        }
    }
}
