using MediaLib;
using MediaLib.Lib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


namespace MediaLabUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    /// 

    public delegate bool MediaFilter(MediaLib.Lib.Media media);
    public sealed partial class MainPage : Page, INotifyPropertyChanged, MediaUI
    {
        private MediaInfo persistedItem;
        
        public ObservableCollection<MediaInfo> Images { get;  } = new ObservableCollection<MediaInfo>();
        public Dictionary<string,MediaInfo> ItemCache = new Dictionary<string, MediaInfo>();
        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
        }

        
        public MediaFilter viewfilter;

        private BitmapImage _defaultAnimeImg;
        private async Task<BitmapImage> DefaultAnimeImg()
        {
            if (_defaultAnimeImg == null)
            {
                StorageFile defaultImg = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Default\\Anime.png");
                using (IRandomAccessStream fileStream = await defaultImg.OpenReadAsync())
                {
                    _defaultAnimeImg = new BitmapImage();
                    _defaultAnimeImg.SetSource(fileStream);
                }
            }
            return _defaultAnimeImg;
        }

        private async Task BuildDisplayList()
        {
            Images.Clear();
            var medialib = MediaLib.Lib.MediaLib.instance;

            BitmapImage defaultImg = await DefaultAnimeImg();
            //add new medium
            MediaLib.Lib.MediaLib.instance.TravelMedium((MediaLib.Lib.Media media) =>
            {
                if (!ItemCache.ContainsKey(media.UID))    // add the item not in the cache
                {
                    var mediainfo = new MediaInfo(media as MediaLib.Lib.Anime, defaultImg);
                    ItemCache.Add(mediainfo.UID, mediainfo);
                }
                if(viewfilter==null || viewfilter(media))
                {
                    Images.Add(ItemCache[media.UID]);
                }
            });

            return;            
           
        }

        private async Task LoadItems()
        {
  
            //load media info first
            await BuildDisplayList();

            //load images
            foreach (var image in Images)
            {
                var media =  MediaLib.Lib.MediaLib.instance.getMedia(image.UID);
                if (image.imageStatus == MediaInfo.ImageStatus.Default || image.imageStatus == MediaInfo.ImageStatus.UnLoad)
                {
                    string imagePath = await media.imgMgr.AsyncGetImageFromMedia(media);
                    if (imagePath != null)
                    {
                        imagePath = imagePath.Replace('/', '\\');
                        StorageFile imageFile = await StorageFile.GetFileFromPathAsync(imagePath);
                        using (IRandomAccessStream fileStream = await imageFile.OpenReadAsync())
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(fileStream);
                            image.ThumbImage = bitmapImage;
                            image.imageStatus = MediaInfo.ImageStatus.Loaded;
                        }
                    }
                    else
                    {
                        image.imageStatus = MediaInfo.ImageStatus.FailedToLoad;
                    }
                }
            }

        }

       
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Collapsed;
            await MediaLib.Lib.MediaLib.instance.AsyncInitRootManagersFromLocal();
            MediaLib.Lib.MediaLib.instance.setUI(this);
            await LoadItems();      
            base.OnNavigatedTo(e);          
        }

        // Called by the Loaded event of the ImageGridView.
        private async void StartConnectedAnimationForBackNavigation()
        {
            // Run the connected animation for navigation back to the main page from the detail page.
            if (persistedItem != null)
            {
                ImageGridView.ScrollIntoView(persistedItem);
                ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("backAnimation");
                if (animation != null)
                {
                    await ImageGridView.TryStartConnectedAnimationAsync(animation, persistedItem, "ItemImage");
                }
            }
        }
     
        
        private async void  ImageGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
             persistedItem = e.ClickedItem as MediaInfo;
            if (persistedItem.enable)
            {
                await Windows.System.Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(persistedItem.MediaSubTitle));
            }
        }
       

        public double ItemSize
        {
            get => _itemSize;
            set
            {
                if (_itemSize != value)
                {
                    _itemSize = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemSize)));
                }
            }
        }
        private double _itemSize;

        
        private void DetermineItemSize()
        {
            if (FitScreenToggle != null
                && FitScreenToggle.IsOn == true
                && ImageGridView != null
                && ZoomSlider != null)
            {
                // The 'margins' value represents the total of the margins around the
                // image in the grid item. 8 from the ItemTemplate root grid + 8 from
                // the ItemContainerStyle * (Right + Left). If those values change,
                // this value needs to be updated to match.
                int margins = (int)this.Resources["LargeItemMarginValue"] * 4;
                double gridWidth = ImageGridView.ActualWidth - (int)this.Resources["DesktopWindowSidePaddingValue"];

                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" &&
                    UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Touch)
                {
                    margins = (int)this.Resources["SmallItemMarginValue"] * 4;
                    gridWidth = ImageGridView.ActualWidth - (int)this.Resources["MobileWindowSidePaddingValue"];
                }

                double ItemWidth = ZoomSlider.Value + margins;
                // We need at least 1 column.
                int columns = (int)Math.Max(gridWidth / ItemWidth, 1);

                // Adjust the available grid width to account for margins around each item.
                double adjustedGridWidth = gridWidth - (columns * margins);

                ItemSize = (adjustedGridWidth / columns);
            }
            else
            {
                ItemSize = ZoomSlider.Value;
            }
        }


        private async Task UpdateItemsForQuery(string text)
        {
            // no input 
            if (text == null || text.Length == 0)
                viewfilter = null;
            else
            {
                // some input
                viewfilter = new MediaFilter((MediaLib.Lib.Media media) =>
                {
                    if (media.contentDir.ToLower().Contains(text.ToLower()))
                        return true;
                    return false;
                });
            }

           await  LoadItems();

        }
        private async void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            await UpdateItemsForQuery(args.QueryText);
        }

        private async void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            await UpdateItemsForQuery(args.QueryText);
        }

        private async void addRootButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(AddRootPage), null);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();
                newViewId = ApplicationView.GetForCurrentView().Id;
              
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        public async void refreshAllItem()
        {
           await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                await LoadItems();
            });
            
        }
    }
}
