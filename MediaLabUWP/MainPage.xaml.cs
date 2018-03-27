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
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private MediaInfo persistedItem;
        
        public ObservableCollection<MediaInfo> Images { get;  } = new ObservableCollection<MediaInfo>();
        public Dictionary<string,MediaInfo> ItemCache = new Dictionary<string, MediaInfo>();
        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
        }


        private async Task LoadItems()
        {

           await  MediaLib.Lib.MediaLib.instance.AsyncInitRootManagersFromLocal();

            StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Default\\Anime.png");
            using (IRandomAccessStream fileStream = await file.OpenReadAsync())
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(fileStream);
                MediaLib.Lib.MediaLib.instance.TravelMedium((MediaLib.Lib.Media media) =>
                {
                    var mediainfo = new MediaInfo(media as MediaLib.Lib.Anime, bitmapImage);
                    Images.Add(mediainfo);
                    ItemCache.Add(mediainfo.UID, mediainfo);
                });              
            }
            foreach(var image in Images)
            {
                var media =  MediaLib.Lib.MediaLib.instance.getMedia(image.UID);

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
                    }
                }
            }
            
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Collapsed;
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


        private void updateItems(string text)
        {
            Images.Clear();
            if (text != null && text.Length > 0)
            {
                
                foreach (var item in ItemCache.Values)
                {
                    if (item.MediaSubTitle.ToLower().Contains(text.ToLower()))
                    {
                        Images.Add(item);
                    }
                }
            }
            else
            {
                foreach (var item in ItemCache.Values)
                {                 
                        Images.Add(item);
                }
            }
        }
        private void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            updateItems(args.QueryText);
        }

        private void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            updateItems(args.QueryText);
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
    }
}
