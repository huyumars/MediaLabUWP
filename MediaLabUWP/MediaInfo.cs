using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace MediaLabUWP
{


    public class MediaInfo : INotifyPropertyChanged
    {
        public MediaInfo(ImageProperties properties, StorageFile imageFile,
            BitmapImage src, string name, string type)
        {
            ThumbImage = src;
            MediaName = name;
            MediaSubTitle = type;
            var rating = (int)properties.Rating;
            var random = new Random();
            MediaRating = rating == 0 ? random.Next(1, 5) : rating;
        }

        public MediaInfo(MediaLib.Lib.Anime media, BitmapImage src)
        {
            UID = media.UID;
            MediaName = media.title;
            MediaTitle = media.title;
            MediaSubTitle = media.contentDir;
            MediaRating = (int)media.star;
            ThumbImage = src;
        }

        private BitmapImage _thumbImage = null;
        public BitmapImage ThumbImage
        {
            get => _thumbImage;
            set => SetProperty(ref _thumbImage, value);
        }

        public string MediaName { get; }

       public string MediaSubTitle { get; }

        public string UID { get;  }

        public string ImageDimensions => $"{ThumbImage.PixelWidth} x {ThumbImage.PixelHeight}";

        string _mediaTitle;
        public string MediaTitle
        {
            get => _mediaTitle;
            set
            {
                _mediaTitle = value;
                OnPropertyChanged();
            }
        }

        int _mediaRating;
        public int MediaRating
        {
            get => _mediaRating;
            set
            {
                _mediaRating = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value))
            {
                return false;
            }
            else
            {
                storage = value;
                OnPropertyChanged(propertyName);
                return true;
            }
        }
    }
}
