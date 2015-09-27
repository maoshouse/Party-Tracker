using Party_Tracker.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Storage;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Party_Tracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class settings_page : Page
    {
        // Storage constants:
        const string setting_username = "setting_username";
        const string setting_phone_no = "setting_phone_no";

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public settings_page()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // Populate the username and phone number fields if they are available in the isolated storage settings.
            // If there are none, then leave blank
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(setting_username))
            {
                tb_username.Text = localSettings.Values[setting_username] as string;
            }
            else
            {
                tb_username.Text = "";
            }

            if (localSettings.Values.ContainsKey(setting_phone_no))
            {
                tb_Phone_No.Text = localSettings.Values[setting_phone_no] as string;
            }
            else
            {
                tb_Phone_No.Text = "";
            }


        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void bt_save_settings_Click(object sender, RoutedEventArgs e)
        {
            // store the settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            localSettings.Values[setting_username] = tb_username.Text;
            localSettings.Values[setting_phone_no] = tb_Phone_No.Text;


            // return to main page only if settings are valid and saved.
            if (localSettings.Values[setting_username] == "" || localSettings.Values[setting_phone_no] == "" || (localSettings.Values[setting_phone_no] as string).Length < 7 || !IsDigitsOnly(localSettings.Values[setting_phone_no] as string))
            {
                ContentDialog_invalid_settings cdiag_settings = new ContentDialog_invalid_settings();
                await cdiag_settings.ShowAsync();
            }
            else
            {
                Frame.Navigate(typeof(MainPage));
            }
        }

        private Boolean IsDigitsOnly(string text)
        {
            {
                foreach (char c in text)
                {
                    if (c < '0' || c > '9')
                        return false;
                }

                return true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var navigate = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
        }
    }
}
