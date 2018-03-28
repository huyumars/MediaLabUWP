using System;
using System.Collections;
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
        public delegate void MediaHandler(Media media);
        public delegate void UpdateMediaForList(Media media);
        public delegate void UpdateMediumForList(IEnumerable<Media> medium);

        interface MediaUI
        {
            void refreshAllItem();
        }
        class MediaLib
        { 
            public Media getMedia(String UID)
            {
                String[] str = UID.Split('#');
                String Prefix = str[0];
                if (rootManagers.ContainsKey(Prefix))
                {
                    return rootManagers[Prefix].getMedia(UID);
                }
                return null;
            }
            public void  TravelMedium(MediaHandler handler)
            {
                foreach(IO.IMediaRootManager rootMgr in rootManagers.Values)
                {
                    rootMgr.travelMedia(handler);
                }
            }
            public IEnumerable<IO.IMediaRootManager> getRootMgrs()
            {
                return rootManagers.Values.ToList();
            }
              
            Dictionary<String,IO.IMediaRootManager> rootManagers;
            private MediaLib() {
                rootManagers = new Dictionary<String, IO.IMediaRootManager>();
            }
            static MediaLib _instance = new MediaLib();
            public static String assignUID(IO.IMediaRootManager manager)
            {
                String newID;
                do
                {
                    newID = manager.Prefix + "#" + System.IO.Path.GetRandomFileName();
                }
                while (manager.existMedia(newID));
                return newID;
                
            }
            public static MediaLib instance { get { return _instance; } }

            MediaUI _uiPage;
            public void setUI(MediaUI _ui) { _uiPage = _ui; }
            public void RefreshUI() { _uiPage.refreshAllItem(); }
            public async Task AsyncInitRootManagersFromLocal()
            {
                //load each media
                foreach (string name in Enum.GetNames(typeof(Config.MediaType)))
                {
                    try
                    {
                        String rootCofigName = "MediaLib.Lib." + name + "RootConfig";
                        Config.IRootConfig fakeRootConfig = (Config.IRootConfig)System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(rootCofigName);
                        foreach (var config in fakeRootConfig.getConfigHelper().Configs())
                        {
                            await instance.AsyncAddRoot(config as Config.IRootConfig);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ERROR("loading" + name + " root manager failed");
                        Logger.ERROR("Hint: you should rename the rootconfig class name with type+RootConfig in the same namespace, for example: Anime, AnimeRootConifg ");
                        Logger.ERROR(ex.Message);
                    }
                }
            }
            public void initRootManagersFromLocal()
            {
                //load each media
                foreach (string name in Enum.GetNames(typeof(Config.MediaType)))
                {
                    try
                    {
                        String rootCofigName = "MediaLib.Lib." + name + "RootConfig";
                        Config.IRootConfig fakeRootConfig = (Config.IRootConfig)System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(rootCofigName);
                        fakeRootConfig.getConfigHelper().foreachConfig(
                        new Config.ConfigHandler((Config.IConfig config) =>
                        {
                            instance.addRoot(config as Config.IRootConfig);
                        })
                        );
                    }
                    catch (Exception ex)
                    {
                        Logger.ERROR("loading"+name+" root manager failed");
                        Logger.ERROR("Hint: you should rename the rootconfig class name with type+RootConfig in the same namespace, for example: Anime, AnimeRootConifg ");
                        Logger.ERROR(ex.Message);
                    }
                }
                
            }

            public void saveRootManagers()
            {
                foreach (IO.IMediaRootManager mgr in rootManagers.Values)
                    mgr.saveConfig();
            }

            private async Task AsyncAddManager(IO.IMediaRootManager mgr)
            {
                await mgr.AsyncLoad();
                rootManagers.Add(mgr.Prefix, mgr);
            }
            private void addManager(IO.IMediaRootManager mgr)
            {
                //ui delegate
                /*
                mgr.updateMediaForListDelegate = UI.MediaListViewHelper.instance.updateItem;
                mgr.deleteMediaForListDelegate = UI.MediaListViewHelper.instance.removeItem;
                mgr.updateMediumForListDelegate = UI.MediaListViewHelper.instance.updateItems;
                */
                //aync load;
                mgr.load();
                rootManagers.Add(mgr.Prefix, mgr);
                
            }

            public async Task AsyncAddRoot(Config.IRootConfig rootConfig)
            {
                IO.IMediaRootManager mgr = rootConfig.buildRootMangerFromConfig();
                await AsyncAddManager(mgr);
            }

            public void addRoot(Config.IRootConfig rootConfig)
            {
                IO.IMediaRootManager mgr = rootConfig.buildRootMangerFromConfig();
                addManager(mgr);       
            }

            public void removeRoot(String Prefix)
            {
                if (rootManagers.ContainsKey(Prefix))
                {
                    IO.IMediaRootManager mgr = rootManagers[Prefix];  
                    rootManagers.Remove(Prefix);
                    /*
                    UI.MediaListViewHelper.instance.removeItems(mgr.getMedium());
                    */
                    mgr.saveConfig();
                }                                  
            }

            void updateMediaForList(Media media)
            {
                _uiPage.refreshAllItem();
            }

            void updateMediumForList(IEnumerable<Media> medium)
            {
                _uiPage.refreshAllItem();
            }
        }
    }
}
