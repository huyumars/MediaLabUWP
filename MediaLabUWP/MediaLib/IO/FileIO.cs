using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaLib.Lib;
using System.ComponentModel;
using System.Threading;

namespace MediaLib
{
    namespace IO
    {
        public interface IMediaRootManager
        {
            bool enable { get; }
            string Prefix {  get; }

            string Name { get; }
            string rootPath { get; }
            string type { get; }
            UpdateMediaForList updateMediaForListDelegate { set; get; }
             UpdateMediaForList deleteMediaForListDelegate { set; get; }
            UpdateMediumForList updateMediumForListDelegate { set; get; }
            bool  existMedia(String UID);
             Lib.Media getMedia(String UID);
            void travelMedia(Lib.MediaHandler handler);
            IEnumerable<Lib.Media> getMedium();
            void load();
            void saveConfig();
             
        }
        class MediaRootManager<T>:IMediaRootManager where T: Lib.Media 
        {
          
            MediaFileTraveler fileTraveler;
            MediaWatcher fileWatcher;
            Config.RootConfig<T> _config;
            Dictionary<String, T> mediaLib;
            Dictionary<String, String> pathIndex;
            BackgroundWorker worker;
            private void buildPathIndex()
            {
                needDeleteMedia = new List<string>();
                travelMedia((Lib.Media media) => {
                    //only build the valid path index
                    if (System.IO.Directory.Exists(media.contentDir))
                        pathIndex[media.contentDir] = media.UID;
                    //else marks as unenable
                    else
                        needDeleteMedia.Add(media.UID);
                });
               
            }

            List<String> needDeleteMedia;
            private Stack<Newtonsoft.Json.Linq.JObject> TemplateStack = null;
            static string[] NoTemplate = new string[] {"UID", "contentDir", "title"};
            private void refineWithTemplate(T media)
            {
                if(TemplateStack!=null && TemplateStack.Count > 0)
                {
                    Newtonsoft.Json.Linq.JObject template = TemplateStack.Peek();
                    foreach(var tp in template)
                    {
                        if (typeof(T).GetProperty(tp.Key)!= null && !NoTemplate.Contains(tp.Key))
                        {
                            var p = typeof(T).GetProperty(tp.Key);
                            //only set template value with stirng and Enum and Int
                            if (p.GetValue(media,null) is String && p.SetMethod != null)
                            {
                                p.SetValue(media, (string)tp.Value);
                            }
                            else if (p.GetValue(media, null) is Enum && p.SetMethod != null)
                            {
                                if ((int)tp.Value != 0) //not default one
                                {
                                    p.SetValue(media, (int)tp.Value);
                                }
                            }
                        }
                    }
                }
            }
            public void load()
            {
                if (enable == false)
                {
                    //guarantee all UI jobs should be in sub-threads
                    worker.DoWork += (object sender, DoWorkEventArgs e) =>
                    {
                        updateMediumForListDelegate(this.getMedium());
                    };               
                    return;
                }
                else
                {
                    //update async
                    worker.DoWork += (object sender, DoWorkEventArgs e) =>
                    {
                        buildPathIndex();
                        fileTraveler.templateHandler = new MediaTemplateHandler((String file) =>
                        {
                            Newtonsoft.Json.Linq.JObject template = Config.JSONHelper.readFromJsonFile<Newtonsoft.Json.Linq.JObject>(file);
                            template["filePath"] = file;
                            if (template != null)
                            {
                                if (TemplateStack == null) TemplateStack = new Stack<Newtonsoft.Json.Linq.JObject>();
                                //if is the second time, template end, pop the template
                                if (TemplateStack.Count > 0 && TemplateStack.Peek()["filePath"].ToString() == template["filePath"].ToString())
                                {
                                    TemplateStack.Pop();
                                }
                                else //the first time
                                {
                                    TemplateStack.Push(template);
                                }
                            }
                        });
                        fileTraveler.travel((DirectoryInfo info) =>
                        {
                            T media = null;

                            if (pathIndex.ContainsKey(info.FullName))
                            {
                                media = (T)getMedia(pathIndex[info.FullName]);
                                refineWithTemplate(media);
                                return;
                            }
                            //need to be update
                            String UID = Lib.MediaLib.assignUID(this);
                            media = (T)Lib.Media.MediaFactory(info, UID, _config.type);
                            refineWithTemplate(media);
                            mediaLib[media.UID] = media;
                            pathIndex[media.contentDir] = media.UID;
                        });

                        //remove items do not need
                        foreach (String uid in needDeleteMedia)
                        {
                            deleteMedia(uid);
                        }
                        updateMediumForListDelegate(this.getMedium());
                    };
                    worker.RunWorkerAsync();
                }
                //start wathing the directory
                fileWatcher = new MediaWatcher(_config.dirName);
                fileWatcher.Start();
                fileWatcher.addMediaDir += AddMediaDir;
                fileWatcher.deleteMediaDir += DeleteMediaDir;
                fileWatcher.renameMediaDir += RenameMediaDir;
            }

            void AddMediaDir(String dir)
            {
                if (fileTraveler.isValid(dir)) {
                    String UID = Lib.MediaLib.assignUID(this);
                    T media = (T)Lib.Media.MediaFactory(new DirectoryInfo(dir), UID, _config.type);
                    updateMedia(media);
                }
            }
            void RenameMediaDir(string oldDir, string newDir)
            {
                if (pathIndex.ContainsKey(oldDir))
                {
                    T media = (T)getMedia(pathIndex[oldDir]);
                    media.contentDir = newDir;               
                    updateMedia(media);
                }
            }
            void DeleteMediaDir(string dir)
            {
                if (pathIndex.ContainsKey(dir))
                {
                    deleteMedia(pathIndex[dir]);
                }
            }


            public MediaRootManager(Config.RootConfig<T> config)
            {
                _config = config;
                fileTraveler = config.getFileTraveler();
                
                pathIndex = new Dictionary<string, string>();
                worker = new BackgroundWorker();

                //dir exits?
                if (Directory.Exists(config.dirName))
                    _enable = true;
                else
                    _enable = false;

                if (config.mediaDic == null)
                {
                    //create lib
                    mediaLib = new Dictionary<String, T>();
                    _config.mediaDic = mediaLib;
                }
                else
                {
                    //read lib from config
                    mediaLib = config.mediaDic;
                }
 
            }
          
            public bool existMedia(String UID) { return mediaLib.ContainsKey(UID); }

            public Media getMedia(string UID)
            {
                if(mediaLib.ContainsKey(UID))
                    return mediaLib[UID];
                return null;
            }

            static object travelLock = new object();
            private void updateMedia(T media)
            {
                mediaLib[media.UID] = media;
                pathIndex[media.contentDir] = media.UID;
                updateMediaForListDelegate(media);
            }
           static  Mutex mutex = new Mutex();
            private void deleteMedia(string UID)
            {
                lock (travelLock)
                {
                    Media media = mediaLib[UID];
                    mediaLib.Remove(UID);
                    pathIndex.Remove(media.contentDir);
                    deleteMediaForListDelegate(media);
                }
            }
            public void travelMedia(MediaHandler handler)
            {
                lock (travelLock)
                {
                    foreach (T media in mediaLib.Values)
                    {
                        media.enable = enable;
                        handler(media);
                    }
                }
            }

            public IEnumerable<Media> getMedium()
            {
                return mediaLib.Values;
            }

            //avoid muti-thread save
            static object locker = new object();
            public void saveConfig()
            {
                lock (locker)
                {
                    if (Lib.MediaLib.instance.getRootMgrs().Contains(this))
                        //update
                        _config.getConfigHelper().UpdateConfig(_config.dirName, _config);
                    else
                        //deleted
                        _config.getConfigHelper().RemoveConfig(_config.dirName);
                    _config.getConfigHelper().saveConfig();
                }
            }

            public  string Prefix { get { return _config.dirName.Replace('\\','_').Replace(':','_'); }  }

            public UpdateMediaForList updateMediaForListDelegate { set; get; }
            public UpdateMediaForList deleteMediaForListDelegate { set; get; }
            public UpdateMediumForList updateMediumForListDelegate { set; get; }
            bool _enable;
            public bool enable { get { return _enable;} }

            public string Name
            {
                get
                {
                    return _config.name;
                }
            }

            public string rootPath
            {
                get
                {
                    return _config.dirName;
                }
            }

            public string type
            {
                get
                {
                    return _config.type.ToString();
                }
            }
        }

        

        public delegate void AddMediaDir(String dir);
        public delegate void RenameMediaDir(string oldDir, string newDir);
        public delegate void DeleteMediaDir(string dir);
        class MediaWatcher 
        {
            public AddMediaDir addMediaDir;
            public RenameMediaDir renameMediaDir;
            public DeleteMediaDir deleteMediaDir;
            FileSystemWatcher fileWather;
            String rootPath;
            public MediaWatcher(String path)
            {
                rootPath = path;
                fileWather = new FileSystemWatcher();
                fileWather.Path = rootPath;
                fileWather.IncludeSubdirectories = true;
                fileWather.NotifyFilter = NotifyFilters.DirectoryName;
                fileWather.Changed += new FileSystemEventHandler(OnChanged);
                fileWather.Created += new FileSystemEventHandler(OnChanged);
                fileWather.Deleted += new FileSystemEventHandler(OnChanged);
                fileWather.Renamed += new RenamedEventHandler(OnRenamed);
            }

            public void Start()
            {
                fileWather.EnableRaisingEvents = true;
            }

            public void End()
            {
                fileWather.EnableRaisingEvents = false;
            }

            private  void OnChanged(object source, FileSystemEventArgs e)
            {
                // Specify what is done when a file is changed, created, or deleted.
                Logger.INFO("File: " + e.FullPath + " " + e.ChangeType);
                if(e.ChangeType == WatcherChangeTypes.Created)
                {
                    addMediaDir(e.FullPath);
                }
                else if(e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    deleteMediaDir(e.FullPath);
                }

            }

            private  void OnRenamed(object source, RenamedEventArgs e)
            {
                // Specify what is done when a file is renamed.
                Logger.INFO("File: "+e.OldFullPath+" renamed to "+ e.FullPath);
                renameMediaDir(e.OldFullPath, e.FullPath);
            }
        }
    }
}
