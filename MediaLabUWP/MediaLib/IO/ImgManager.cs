using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections;
using MediaLib.Lib;
using MediaLib.Config;

namespace MediaLib
{
    namespace Resource
    {
        public delegate void finishProcessImage(String imageSrc, Lib.Media media);
        public class ffmpegHepler
        {
            static ffmpegHepler _instance = new ffmpegHepler();
            static string[] videoExt = new String[] { ".mp4", ".avi", ".mkv" };
            //ffmpeg seems can only work in single thread
            static ThreadQueue ffmpegQueue = new ThreadQueue();
            public static ffmpegHepler instance { get { return _instance; } }
            ffmpegHepler() { }

            public static string CatchImg(string path, string imgFile)
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "ffmpeg";//要执行的程序名称
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                //启动程序   
                //向CMD窗口发送输入信息：  
                // string arguments = " -ss 00:00:10  -i  \"" + path + "\"  -f image2  -y \"" + imgFile + "\"";
                string arguments = " -ss 00:00:10  -i  \"" + path + "\" -vframes 1 -q:v 2 \"" + imgFile + "\"";
                p.StartInfo.Arguments = arguments;
                p.Start();
                // 异步获取命令行内容
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.OutputDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
                {
                    if (String.IsNullOrEmpty(e.Data) == false)
                        Logger.INFO(e.Data);
                });
                p.ErrorDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
                {
                    if (String.IsNullOrEmpty(e.Data) == false)
                        Logger.INFO(e.Data);
                });
                bool end = p.WaitForExit(1000*20);
                if(end == false)
                {
                    p.Kill();
                }
                //p.StandardInput.WriteLine(get_utf8(str).ToCharArray());
                //-ss表示搜索到指定的时间 -i表示输入的文件 -y表示覆盖输出 -f表示强制使用的格式  
                if (System.IO.File.Exists(imgFile))
                    return imgFile;
                return "";
            }
            static String getMediaFile(String contentDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(contentDir);
                foreach (var file in dirInfo.GetFiles())
                    //find video file
                    if (videoExt.Contains(file.Extension.ToLower()))
                        return file.FullName;
                //go deep to find media file
                foreach (var dir in dirInfo.GetDirectories())
                {
                    String meidaPath = getMediaFile(dir.FullName);
                    if (meidaPath != null) return meidaPath;
                }
                return null;
            }

            public void enqueueImgFetching(Lib.Media media, string imgPath, finishProcessImage finish)
            {
                string mediaFile = getMediaFile(media.contentDir);
                if (mediaFile == null) return;
                ffmpegQueue.enqueueTask(new ThreadQueue.Task(() =>
                {
                    CatchImg(mediaFile, imgPath);
                    if (File.Exists(imgPath))
                        finish(imgPath, media);
                }));
            }
        }

        public abstract class ThumbsHepler
        {
            static string[] ImgExt = new String[] { "jpg", "png", "bmp" };
            static string[] videoExt = new String[] { ".mp4", ".avi", ".mkv" };
            static string firstSearchImg = "cover";

            public string tmpPath { set; get; }
            public abstract void getImageFromMedia(Lib.Media media, finishProcessImage handler);
            public abstract String cacheName(Lib.Media media);
            protected static String searchImageFileInDir(String path)
            {
                if (Directory.Exists(path) == false) return null;
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                String fileName = null;
                foreach (var file in dirInfo.GetFiles())
                {
                    if (ImgExt.Contains(file.Extension.ToLower()))
                    {
                        fileName = file.FullName;
                        if (Path.GetFileNameWithoutExtension(file.FullName) == firstSearchImg)
                            break;
                    }
                }
                if (fileName != null)
                    return fileName;
                else return null;
            }
        }

        public class VideoThumbsHelper : ThumbsHepler
        {
            public override string cacheName(Media media)
            {
                return tmpPath + media.UID + ".jpg";
            }

            ThreadQueue readFileQueue = new ThreadQueue();
            public override void getImageFromMedia(Media media, finishProcessImage handler)
            {
                Logger.INFO("start process Img for " + media.contentDir);
                String imagePath = null;
                //seach in resource cache
                if (File.Exists(cacheName(media)))
                    imagePath = cacheName(media);

                //search in media folder
                if (imagePath == null&& Directory.Exists(media.contentDir))
                {
                    imagePath = searchImageFileInDir(media.contentDir);
                    if (imagePath != null)
                    {
                        File.Copy(imagePath, cacheName(media));
                        imagePath = cacheName(media);
                    }
                }


                if (imagePath == null && Directory.Exists(media.contentDir))
                {
                    //create from ffmpeg
                    ffmpegHepler.instance.enqueueImgFetching(media,
                                                                                                cacheName(media),
                                                                                                (String image, Lib.Media _media) =>
                                                                                                {
                                                                                                    Logger.INFO("end process Img for " + media.contentDir);
                                                                                                    handler(image, _media);
                                                                                                });

                }
                else if(imagePath!=null)
                {
                    Logger.INFO("end process Img for " + media.contentDir);
                    //read file in muti-thread
                    readFileQueue.enqueueTask(new ThreadQueue.Task(() =>
                    {

                        //System.Threading.Thread.Sleep(300);
                        handler(cacheName(media), media);
                    }));

                }

            }
        }

        public class AnimeThumbsHelper : VideoThumbsHelper
        {

        }

        public delegate void finishProcessHandler(Lib.Media media, String imgPath);

        public class ImgManager : IDisposable
        {
            public String defaultImage;
            static public void loadConfigs()
            {
                //anime imgmanager
                var mgr = Anime._imgMgr;
            }

            static List<ImgManager> imgMangers = new List<ImgManager>();
            public static void ClearUseLessImg()
            {
                foreach (var mgr in imgMangers)
                {
                    mgr.clearUselessImg();
                }
            }

            /*
            static List<Image> list;
            public static List<Image> getDefaultImageList()
            {
                if (list != null) return list;
                list = new List<Image>();
                int index = 0;
                foreach (var mgr in imgMangers)
                {
                    list.Add(mgr.defaultImage);
                    mgr._defaultImgIndex = index;
                    index++;
                }
                return list;
            }*/

            ThumbsHepler thumbsHelper;
            Config.ResourceConfig config;

            int _defaultImgIndex;
            public int defaultImgIndex { get { return _defaultImgIndex; } }

            public ImgManager(Config.ResourceConfig _config)
            {
                config = _config;
                switch (config.mediaType_t)
                {
                    case Config.MediaType.Anime:
                        thumbsHelper = new AnimeThumbsHelper();
                        break;
                }
                thumbsHelper.tmpPath = config.storePath;
                if (!Directory.Exists(Path.GetDirectoryName(config.storePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(config.storePath));

                imgMangers.Add(this);
            }

            public void getImageFromMedia(Media media, finishProcessImage handler)
            {
                thumbsHelper.getImageFromMedia(media, handler);
            }

            public void Dispose()
            {
                imgMangers.Remove(this);
            }

            public void clearUselessImg()
            {
                DirectoryInfo dir = new DirectoryInfo(config.storePath);
                foreach (var file in dir.GetFiles())
                {
                    String UID = Path.GetFileNameWithoutExtension(file.Name);
                    if (Lib.MediaLib.instance.getMedia(UID) == null)
                    {
                        File.Delete(file.FullName);
                        Logger.INFO(file.FullName + "is useless,  removed");
                    }

                }

            }
        }
    }
           

       
    
}
