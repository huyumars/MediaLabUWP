using MediaLib.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

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
               await HandleUnauthorizedAccess(ex);
            }
            return false;
        
        }
        static public async Task HandleUnauthorizedAccess(UnauthorizedAccessException ex)
        {

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
                await UWPIOImplementation.HandleUnauthorizedAccess(ex);
            }
        }

        public override bool isValid(string dir)
        {
            return false;
        }
    }
}
