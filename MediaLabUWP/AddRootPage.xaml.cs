using MediaLabUWP.FileAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using MediaLib.Config;
using MediaLib.IO;
using MediaLib.Lib;
using MediaLib;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MediaLabUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    /// 

    public sealed partial class AddRootPage : Page, INotifyPropertyChanged
    {
        public string chosenPath { set
            {
                if (value!=null && _chosenPath != value)
                {
                    _chosenPath = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(chosenPath)));
                }
            }  get => _chosenPath; }
        
        private string _chosenPath;

        public string rootName
        {
            set
            {
                if (value != null && _rootName != value)
                {
                    _rootName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(rootName)));
                }
            }
            get => _rootName;
        }
        private string _rootName;

        public string depth
        {
            set
            {
                if (value != null && _depth != value)
                {
                    _depth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(depth)));
                }
            }
            get => _depth;
        }
        private string _depth;

        public event PropertyChangedEventHandler PropertyChanged;
        public AddRootPage()
        {
            chosenPath = "";
            this.InitializeComponent();
        }

        private async void AddRootChooseFoler_Click(object sender, RoutedEventArgs e)
        {
            chosenPath = await UWPIOImplementation.AsyncFolderPicker() ;
        }

        private async void Finish_Click(object sender, RoutedEventArgs e)
        {
            string a = rootName;
            string b = chosenPath;
            string d = SDBox.Text;
            string mType = "Anime";
            IRootConfig config = null;
            try
            {
                String rootCofigName = "MediaLib.Lib." + mType + "RootConfig";
                config = (IRootConfig)System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(rootCofigName);
                (config).name = rootCofigName;
                (config as IFixDepthFileTravelerConfig).dirName = chosenPath;
                (config as IFixDepthFileTravelerConfig).mediaFileExistDirLevel = int.Parse(d);

               await  MediaLib.Lib.MediaLib.instance.AsyncAddRoot(config);
                if ((config).valid())
                {
                    
                }
                else throw new Exception();
            }
            catch
            {
                Logger.ERROR("error");
            }
        }
    }
}
