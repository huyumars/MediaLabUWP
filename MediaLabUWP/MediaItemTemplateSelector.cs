using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MediaLabUWP
{
    class MediaItemTemplateSelector: DataTemplateSelector
    {
        public DataTemplate InvalidItemTemplate { get; set; }


        public DataTemplate DefaultItemTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            MediaInfo model = item as MediaInfo;
            if (model.enable)//根据数据源设置的数据显示模式返回前台样式模版
            {
                return DefaultItemTemplate;
            }
            else
            {
                return InvalidItemTemplate;
            }
        }
    }
}
