using MediaLib;
using MediaLib.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace MediaLabUWP.FileAccess
{

    class UWPIOImplementation
    {
        static public async Task<bool> AsyncGetFolderExists(String path)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                return true;
            }
            catch(UnauthorizedAccessException ex)
            {
               await HandleUnauthorizedAccess(ex,path);
            }
            return false;
        
        }
        static public bool CanUseOldApi(String path)
        {
            try
            {
                System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(path);
                info.GetFiles();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        static public async Task HandleUnauthorizedAccess(UnauthorizedAccessException ex, string path)
        {

            Windows.UI.Popups.MessageDialog msgDialog = new Windows.UI.Popups.MessageDialog("我是一个提示内容"+path) { Title = "提示标题" };
            msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("确定",null,0));
            msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("取消",null,1));
            var result = await msgDialog.ShowAsync();
            if ((int)result.Id == 0)
            {
                await AsyncFolderPicker();
            }
        }
        static public async Task<string> AsyncFolderPicker()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.Add(folder);
                return folder.Path;
            }
            return null;
        }
    }
    public class UWPFixDepthFileTraveler : MediaFileTraveler
    {
        public UWPFixDepthFileTraveler(IFixDepthFileTravelerConfig rootConfig) : base(rootConfig) { }

        public override async Task AsyncTravel(AsyncMediaDirHandle _handler)
        {
            _asyncHandler = _handler;
            StorageFolder curFolder =await StorageFolder.GetFolderFromPathAsync(config.dirName);
            await _AsyncTravel(curFolder);
        }
        private async Task _AsyncTravel(StorageFolder curFolder, int depth = 0)
        {

            //TODO find template
            if (depth == (config as IFixDepthFileTravelerConfig).mediaFileExistDirLevel)
            {
                await _asyncHandler(curFolder.Path);
                return;
            }
            try
            {
                //todo template
                var folders = await curFolder.GetFoldersAsync();
                foreach(var folder in folders)
                {
                    await _AsyncTravel(folder, depth + 1);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await UWPIOImplementation.HandleUnauthorizedAccess(ex,curFolder.Path);
            }
            catch(Exception ex)
            {
                Logger.ERROR("meet error when traveling folder: " + curFolder.Path);
            }
        }
        private async Task<bool> AsyncGetValid(string dir)
        {
            StorageFolder curDir = await StorageFolder.GetFolderFromPathAsync(dir);
            for (int i = 0; i < (config as IFixDepthFileTravelerConfig).mediaFileExistDirLevel; i++)
            {
                curDir = await curDir.GetParentAsync();
            }
            if (curDir.Path == config.dirName)
                return true;
            return false;
        }
        public override bool isValid(string dir)
        {
            return AsyncGetValid(dir).Result;          
        }
    }
}
