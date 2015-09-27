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
using System.Diagnostics;
using Windows.ApplicationModel.Calls;
using Windows.Networking.Proximity;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.Storage;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Phone.UI.Input;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Party_Tracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Tracking_Page : Page
    {

        /* event handlers */
        private List<EventHandler<BackPressedEventArgs>> listOfHandlers = new List<EventHandler<BackPressedEventArgs>>();

        private void InvokingMethod(object sender, BackPressedEventArgs e)
        {
            for (int i = 0; i < listOfHandlers.Count; i++)
                listOfHandlers[i](sender, e);
        }

        public event EventHandler<BackPressedEventArgs> myBackKeyEvent
        {
            add { listOfHandlers.Add(value); }
            remove { listOfHandlers.Remove(value); }
        }

        /* event handlers */

        private Dictionary<string, string> phonebook;
        private string sessionPin;

        private Dictionary<string, KeyValuePair<double, double>> peerLocations;

        private Geoposition myGeoposition;
        private Geolocator myGeolocator;
        private MapIcon myMapIcon;

        /* Objects for drawing on map */
        private MapControl mc_MapControl;

        private Dictionary<string, BasicGeoposition> myGeopositions; // possibly dont need
        private Dictionary<string, MapIcon> myMapIcons;
        /* /// */


        private ListView lv_peerlist;
        private TextBlock tb_debug;
        private Boolean tb_debugIsLoaded;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        // Storage constants:
        const string setting_username = "setting_username";
        const string setting_phone_no = "setting_phone_no";

        string[] displayNameSeperator = new string[] { "[x]" };
        string[] coordinateSeparator = new string[] { "[y]" };


        /* generic helper functions */
        private string getPeerName(string displayName)
        {
            return displayName.Substring(6);
        }

        private string getPIN(string displayName)
        {
            return displayName.Substring(1, 4);
        }

        private KeyValuePair<double, double> getCoordinates(string coordinateString)
        {
            string[] longLat = coordinateString.Split(coordinateSeparator, StringSplitOptions.None);

            KeyValuePair<double, double> kvp = new KeyValuePair<double, double>(Convert.ToDouble(longLat[0]), Convert.ToDouble(longLat[1]));
            return kvp;
        }

        private BasicGeoposition getGeoposition(string coordinateString)
        {
            string[] longLat = coordinateString.Split(coordinateSeparator, StringSplitOptions.None);

            BasicGeoposition basicGeoposition = new BasicGeoposition() { Longitude = Convert.ToDouble(longLat[0]), Latitude = Convert.ToDouble(longLat[1]) };
            return basicGeoposition;
        }

        /* peerWatcher stuff */

        private PeerWatcher peerWatcher;
        private bool peerWatcherIsRunning = false;
        private bool peerFinderStarted = false;
        private ObservableCollection<PeerInformation> discoveredPeers = new ObservableCollection<PeerInformation>();

        private void PeerWatcher_Added(PeerWatcher sender, PeerInformation peerInfo)
        {
            var result = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                lock (this)
                {
                    discoveredPeers.Add(peerInfo);


                    // now compare to the peerList
                    // splitName[0] contains the pin and the peername, splitName[1] has the longitude and latitudes
                    string[] splitName = peerInfo.DisplayName.Split(displayNameSeperator, StringSplitOptions.None);


                    if (getPIN(splitName[0]) == sessionPin)
                    {
                        // session pins are the same therefore this peer's location information must be refreshed.
                        // on the initial added case, there won't be a coordinate yet. so we check to see if splitname is length 2. 

                        if (splitName.Length == 2)
                        {
                            peerLocations[getPeerName(splitName[0])] = getCoordinates(splitName[1]);
                            myMapIcons[getPeerName(splitName[0])].Location = new Geopoint(getGeoposition(splitName[1]));
                            myMapIcons[getPeerName(splitName[0])].Visible = true;
                        }
                        else
                        {
                            // this is the first run so the name won't have the coordinate componenent. in which case we'll give the coordinate 0,0
                            myMapIcons[getPeerName(splitName[0])].Location = new Geopoint(new BasicGeoposition() { Longitude = 0, Latitude = 0 });
                            myMapIcons[getPeerName(splitName[0])].Visible = false;
                        }
                    }

                }
            });
        }

        private void PeerWatcher_Removed(PeerWatcher sender, PeerInformation peerInfo)
        {
            var result = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                lock (this)
                {
                    // Find and remove the peer form the list of discovered peers.
                    for (int i = 0; i < discoveredPeers.Count; i++)
                    {
                        if (discoveredPeers[i].Id == peerInfo.Id)
                        {
                            discoveredPeers.RemoveAt(i);
                        }
                    }

                    // see if this removed peer was part of this session via the PIN
                    string[] splitName = peerInfo.DisplayName.Split(displayNameSeperator, StringSplitOptions.None);

                    if (getPIN(splitName[0]) == sessionPin)
                    {
                        // session pins are the same therefore this peer's location information must be refreshed.
                        //peerLocations[getPeerName(splitName[0])] = getCoordinates(splitName[1]);

                        // other than refreshing the coordinates, we also have to change the color of the peerList for this peer to red to reflect this change.


                        // <==================================================================== DO STUFF HEREERERE

                    }

                }
            });
        }
        private void PeerWatcher_Updated(PeerWatcher sender, PeerInformation peerInfo)
        {
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                lock (this)
                {
                    // Find and update the peer in the list of discovered peers.
                    for (int i = 0; i < discoveredPeers.Count; i++)
                    {
                        if (discoveredPeers[i].Id == peerInfo.Id)
                        {
                            discoveredPeers[i] = peerInfo;

                            // see if this removed peer was part of this session via the PIN
                            string[] splitName = peerInfo.DisplayName.Split(displayNameSeperator, StringSplitOptions.None);

                            if (getPIN(splitName[0]) == sessionPin)
                            {
                                // session pins are the same therefore this peer's location information must be refreshed.
                                peerLocations[getPeerName(splitName[0])] = getCoordinates(splitName[1]);

                                // other than refreshing the coordinates, we also have to change the color of the peerList for this peer to red to reflect this change.


                                // <==================================================================== DO STUFF HEREERERE
                                myMapIcons[getPeerName(splitName[0])].Location = new Geopoint(getGeoposition(splitName[1]));

                            }

                        }
                    }
                }
            });
        }

        private void PeerWatcher_EnumerationCompleted(PeerWatcher sender, object o)
        {
            var result = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                lock (this)
                {
                    if (discoveredPeers.Count == 0)
                    {
                        // No peers discovered for this enumeration.
                    }

                    else
                    {
                        // there are discovered peers so filter them based 
                    }
                }
            });
        }

        private void PeerWatcher_Stopped(PeerWatcher sender, object o)
        {
            peerWatcherIsRunning = false;
            var result = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Update UI now that the PeerWatcher is stopped.
            });
        }

        void PeerFinder_StartPeerWatcher(object sender, RoutedEventArgs e)
        {
            if (!peerFinderStarted)
            {
                // PeerFinder must be started first.
                return;
            }

            if (peerWatcherIsRunning)
            {
                // PeerWatcher is already running.
                return;
            }

            try
            {
                if (peerWatcher == null)
                {
                    peerWatcher = PeerFinder.CreateWatcher();

                    // Add PeerWatcher event handlers. Only add handlers once.
                    peerWatcher.Added += PeerWatcher_Added;
                    peerWatcher.Removed += PeerWatcher_Removed;
                    peerWatcher.Updated += PeerWatcher_Updated;
                    peerWatcher.EnumerationCompleted += PeerWatcher_EnumerationCompleted;
                    peerWatcher.Stopped += PeerWatcher_Stopped;
                }

                // Empty the list of discovered peers.
                discoveredPeers.Clear();

                // Start the PeerWatcher.
                peerWatcher.Start();

                peerWatcherIsRunning = true;
            }
            catch (Exception ex)
            {
                // Exceptions can occur if PeerWatcher.Start is called multiple times or
                // PeerWatcher.Start is called the PeerWatcher is stopping.
            }
        }

        /* end peerWatcher stuff */


        /* Geolocator / geoposition */

        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                myGeoposition = e.Position;

                /*
                if (!init_center)
                {
                    this.map_display.Center = pos.Coordinate.Point;
                    this.map_display.ZoomLevel = 13;
                    init_center = true;
                }
                */


                /* new */
                myMapIcon.Visible = true;
                myMapIcon.Location = myGeoposition.Coordinate.Point;
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string tempLongitude = (myGeoposition.Coordinate.Longitude.ToString());
                string tempLatitude = (myGeoposition.Coordinate.Latitude.ToString());

                if (tempLongitude.Length > 8)
                {
                    tempLongitude = tempLongitude.Substring(0, 8);
                }

                if (tempLatitude.Length > 8)
                {
                    tempLatitude = tempLatitude.Substring(0, 8);
                }

                string temp = "[" + sessionPin + "]" + (localSettings.Values[setting_username] as string) + "[x]" + (myGeoposition.Coordinate.Longitude.ToString()) + "[y]" + (myGeoposition.Coordinate.Latitude.ToString());
                PeerFinder.DisplayName = "[" + sessionPin + "]" + (localSettings.Values[setting_username] as string) + "[x]" + tempLongitude + "[y]" + tempLatitude;
                /*
                if (tb_debugIsLoaded == true)
                {
                    tb_debug.Text = PeerFinder.DisplayName;
                }
                */
                // restart peerfinder to have this update
                peerWatcher.Stop();
                PeerFinder.Stop();
                PeerFinder.Start();
                PeerFinder_StartPeerWatcher(null, null);
                /*  old (testing if this works
                foreach (MapIcon mapicon in myMapIcons.Values)
                {
                    mapicon.Visible = true;

                    if (mapicon.Title == "Me")
                    {
                        mapicon.Location =  myGeoposition.Coordinate.Point;
                    }
                    else
                    {
                        mapicon.Location = new Geopoint(new BasicGeoposition() { Longitude = myGeoposition.Coordinate.Longitude - 0.0001, Latitude = myGeoposition.Coordinate.Latitude - 0.0001 });
                    }
                }

                 * */
            });

        }
        /* end geolocator / geoposition */
        public Tracking_Page()
        {
            this.InitializeComponent();
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

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


        private async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack)
            {
                Grid contentGrid = new Grid() { Width = Window.Current.Bounds.Width, Height = double.NaN };
                TextBlock contentText = new TextBlock() { Text = "Going back will exit this session, are you sure?", TextWrapping = TextWrapping.Wrap };
                contentGrid.Children.Add(contentText);

                ContentDialog exitSessionDialog = new ContentDialog() { Title = "End Session?", PrimaryButtonText= "Yes", SecondaryButtonText="No"};
                exitSessionDialog.Content = contentGrid;

                ContentDialogResult cdiag_result = await exitSessionDialog.ShowAsync();

                if (cdiag_result == ContentDialogResult.Primary)
                {
                    peerWatcher.Stop();
                    PeerFinder.Stop();
                    frame.GoBack();
                    e.Handled = true;
                }

                
                
                
            }
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
            //HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            myGeolocator = new Geolocator();
            myGeolocator.DesiredAccuracy = PositionAccuracy.High;
            myGeolocator.ReportInterval = 30;
            myGeolocator.MovementThreshold = 5;
            myGeolocator.PositionChanged += new TypedEventHandler<Geolocator, PositionChangedEventArgs>(OnPositionChanged);

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            /* I'm going on a limb here and just assuming that the XAML assets are all loaded by the time this function is called */
            //phonebook = e.NavigationParameter as Dictionary<string, string>;
            KeyValuePair<string, Dictionary<string, string>> kvp = (KeyValuePair<string, Dictionary<string, string>>)e.NavigationParameter;

            phonebook = kvp.Value;
            sessionPin = kvp.Key.Substring(1, 4);

            peerLocations = new Dictionary<string, KeyValuePair<double, double>>();

            PeerFinder.DisplayName = kvp.Key + (localSettings.Values[setting_username] as string);
            PeerFinder.Start();
            peerFinderStarted = true;

            myMapIcons = new Dictionary<string, MapIcon>();
            foreach (string key in phonebook.Keys)
            {
                myMapIcons[key] = new MapIcon() { Title = key, Visible = true, NormalizedAnchorPoint = new Point(0.5, 0.5) };
            }



            myMapIcon = new MapIcon() { Title = "Me", Visible = true, NormalizedAnchorPoint = new Point(0.5, 0.5) };

            myMapIcons[localSettings.Values[setting_username] as string] = myMapIcon;


            PeerFinder_StartPeerWatcher(null, null);
            //peerWatcher.Start();

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
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
            myGeolocator.PositionChanged -= OnPositionChanged;
            if (peerWatcher != null)
            {
                // Remove event handlers.
                peerWatcher.Added -= PeerWatcher_Added;
                peerWatcher.Removed -= PeerWatcher_Removed;
                peerWatcher.Updated -= PeerWatcher_Updated;
                peerWatcher.EnumerationCompleted -= PeerWatcher_EnumerationCompleted;
                peerWatcher.Stopped -= PeerWatcher_Stopped;


                peerWatcher = null;
            }
            string display_name = localSettings.Values[setting_phone_no] as string;

            // check if somehow the phone number is less than 4 chars long. This will be functionally invalid, in the settings page, there should be a check that makes you enter a phone number that's at least valid.
            if (display_name.Length >= 4)
            {
                display_name = display_name.Substring(display_name.Length - 4, 4);
            }
            else
            {
                // padd it until it is of length 4
                for (int i = 0; i < (4 - display_name.Length); i++)
                {
                    display_name += "0";
                }
            }

            display_name = "[" + display_name + "]" + (localSettings.Values[setting_username] as string);

            PeerFinder.DisplayName = display_name;
            PeerFinder.Stop();
            peerFinderStarted = false;
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


        /*
        private void tb_debug_block_Loaded(object sender, RoutedEventArgs e)
        {
            // sender is the textblock control
            tb_debug = sender as TextBlock;
            tb_debugIsLoaded = true;
        }
        */

        private void lv_peerList_Loaded(object sender, RoutedEventArgs e)
        {
            // sender is listview control
            lv_peerlist = sender as ListView;

            InitializeTracker();
        }


        private int InitializeTracker()
        {
            try
            {
                foreach (string k in phonebook.Keys)
                {
                    lv_peerlist.Items.Add(new KeyValuePair<string, string>(k, phonebook[k]));
                }

                // After initializing the peer list, we'll now formally start the tracker by turning on the peerwatcher.
                //PeerWatcher peerWatcher;
                //peerWatcher.Start();
                //peerWatcher.


            }
            catch (Exception ex)
            {
                /*(
                Debug.WriteLine(ex.Message);
                tb_debug.Text = ex.Message;
                 * */
                return -1;
            }

            return 1;
        }


        private void btn_stop_session_Click(object sender, RoutedEventArgs e)
        {
            peerWatcher.Stop();
            PeerFinder.Stop();
            this.Frame.Navigate(typeof(MainPage));
        }

        private void lv_peerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ListView lv = sender as ListView;
            //KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)lv.SelectedItem;
            if (lv_peerlist.SelectedIndex != -1)
            {
                KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)e.AddedItems[0];
                PhoneCallManager.ShowPhoneCallUI(kvp.Value, kvp.Key);

                lv_peerlist.SelectedIndex = -1;
            }
            //lv_peerlist.UpdateLayout();
        }

        private void Hub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            if (Hub.SectionsInView[0] == hub_map)
            {
                baa_commandbar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                baa_commandbar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void MapControl_Loaded(object sender, RoutedEventArgs e)
        {
            mc_MapControl = sender as MapControl;



            foreach (string key in myMapIcons.Keys)
            {
                mc_MapControl.MapElements.Add(myMapIcons[key]);
            }


        }

        private void center_position_Click(object sender, RoutedEventArgs e)
        {
            mc_MapControl.Center = myGeoposition.Coordinate.Point;
            mc_MapControl.ZoomLevel = 18;
        }

    }
}
