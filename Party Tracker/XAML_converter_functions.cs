using Windows.UI.Xaml.Data;
using System;
using Windows.Networking.Proximity;
using System.Collections.Generic;

namespace Party_Tracker
{
    public class bt_device_Name_BindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            PeerInformation p = value as PeerInformation;
            return p.DisplayName.Substring(6);
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

    public class bt_device_HostName_BindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            PeerInformation p = value as PeerInformation;

            return p.Id;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

    public class tracking_page_peerlist_peername_BindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            KeyValuePair<string, string> p = (KeyValuePair<string, string>)value;
            return p.Key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

    public class tracking_page_peerlist_peerphone_BindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            KeyValuePair<string, string> p = (KeyValuePair<string, string>)value;
            return p.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

    public class loadingScreen_BindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object paramater, string language)
        {
            Boolean p = (Boolean)value;
            if (p)
            {
                return Windows.UI.Xaml.Visibility.Visible;
            }
            else return Windows.UI.Xaml.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

}
