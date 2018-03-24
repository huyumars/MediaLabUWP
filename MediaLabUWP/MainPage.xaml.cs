using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
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
        
        public ObservableCollection<MediaInfo> Images { get; } = new ObservableCollection<MediaInfo>();
        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Collapsed;

            MediaLib.Lib.MediaLib.instance.initRootManagersFromLocal();
            StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Default\\Anime.png");

            using (IRandomAccessStream fileStream = await file.OpenReadAsync())
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(fileStream);
                MediaLib.Lib.MediaLib.instance.TravelMedium((MediaLib.Lib.Media media) =>
                {
                    Images.Add(new MediaInfo(media as MediaLib.Lib.Anime, bitmapImage));
                });
            }
            
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
        
        private async Task ChangeImage(MediaInfo info)
        {
            StorageFile file= await Package.Current.InstalledLocation.GetFileAsync("Assets\\Samples\\1 (113).jpg");
            
            using (IRandomAccessStream fileStream = await file.OpenReadAsync())
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(fileStream);
                info.ThumbImage = bitmapImage;
            }
        }
        
        private void ImageGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Prepare the connected animation for navigation to the detail page.
            persistedItem = e.ClickedItem as MediaInfo;
            ImageGridView.PrepareConnectedAnimation("itemAnimation", e.ClickedItem, "ItemImage");

            // this.Frame.Navigate(typeof(DetailPage), e.ClickedItem);
          var task =  ChangeImage(persistedItem);
        }
        

        private async Task GetItemsAsync()
        {
            // https://docs.microsoft.com/uwp/api/windows.ui.xaml.controls.image#Windows_UI_Xaml_Controls_Image_Source
            // See "Using a stream source to show images from the Pictures library".
            // This code is modified to get images from the app folder.

            // Get the app folder where the images are stored.
            StorageFolder appInstalledFolder = Package.Current.InstalledLocation;
            StorageFolder assets = await appInstalledFolder.GetFolderAsync("Assets\\Samples");

            // Get and process files in folder
            IReadOnlyList<StorageFile> fileList = await assets.GetFilesAsync();
            foreach (StorageFile file in fileList)
            {
                // Limit to only png or jpg files.
                if (file.ContentType == "image/png" || file.ContentType == "image/jpeg")
                {
                    Images.Add(await LoadImageInfo(file));
                }
            }
        }

        public async static Task<MediaInfo> LoadImageInfo(StorageFile file)
        {
            // Open a stream for the selected file.
            // The 'using' block ensures the stream is disposed
            // after the image is loaded.
            using (IRandomAccessStream fileStream = await file.OpenReadAsync())
            {
                // Create a bitmap to be the image source.
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(fileStream);

                var properties = await file.Properties.GetImagePropertiesAsync();
                MediaInfo info = new MediaInfo(
                    properties, file, bitmapImage,
                    file.DisplayName, file.DisplayType);

                return info;
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
    }
}
