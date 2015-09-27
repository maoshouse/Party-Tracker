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

using System.Windows;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Profile;
using System.Diagnostics;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Party_Tracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // user value
        private bool I_am_a_the_host; // this is flipped to true when user clicks connect to selected. On nagivated away from i think this should be set to false.
        private Dictionary<string, string> phonebook;
        private string session_pin; // stores the tracker's session PIN

        // shitty logic
        bool finished_connecting_to_peers;
        bool finalizing_connection; // used to prevent the button from responding to anything before start
        bool is_initial_connect;

        // Storage constants:
        const string setting_username = "setting_username";
        const string setting_phone_no = "setting_phone_no";

        // communications
        StreamSocket _socket;
        DataReader _dataReader;
        DataWriter _dataWriter;
        string[] stringSeparators = new string[] { "[stop]" };
        string[] stringSeparators_dict_message = new string[] { "[to]" };

        // send headers
        const string msg_peerlist = "[x01]";
        const string msg_phone = "[x02]";
        const string msg_phone_book = "[x03]";
        const string msg_ack = "[x04]";
        const string msg_copy = "[x05]";
        const string msg_awaiting = "[lal]";

        // request headers
        const string msg_phone_request = "[r01]";

        // command headers
        const string msg_terminate = "[c04]";

        

        // Error codes:
        private const uint ERR_BLUETOOTH_OFF = 0x8007048F; // radio off

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public MainPage()
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

        private async void ReinitializeMainPage()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            I_am_a_the_host = false;
            finished_connecting_to_peers = false;
            is_initial_connect = true;
            phonebook = new Dictionary<string, string>();
            session_pin = null;

            connect_to_selected_button.Content = "Connect to Selected";
            PeerFinder.Stop();
            // display name will be a 6 char header containing the pin, derived from the last 4 digits of the telephone number [0000] followed by the username
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
            PeerFinder.Start();
            await PeerFinder.FindAllPeersAsync();
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
            // reset the logic.

            PeerFinder.Stop();
            grid_Loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            cancel_button.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            I_am_a_the_host = false;
            finished_connecting_to_peers = false;
            is_initial_connect = true;
            phonebook = new Dictionary<string, string>();
            session_pin = null;
            finalizing_connection = false;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.Count == 0)
            {
                // there are no settings, prompt user
                display_first_run_dialog();
            }
            else
            {
                // if the settings are invalid, ie. no username or no phone number make another prompt
                if (localSettings.Values[setting_username] == "" || localSettings.Values[setting_phone_no] == "")
                {
                    display_first_run_dialog();
                }

                 

            }

            if (localSettings.Values.ContainsKey(setting_username))
            {
                // display name will be a 6 char header containing the pin, derived from the last 4 digits of the telephone number [0000] followed by the username
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
                //PeerFinder.DisplayName = localSettings.Values[setting_username] as string;
            }
            else
            {
                // this thing freaks out//
                //display_missing_settings_dialog();
            }

            PeerFinder.ConnectionRequested += PeerFinder_ConnectionRequested;

            PeerFinder.Start();
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
            // reset the logic
            I_am_a_the_host = false;
            finished_connecting_to_peers = false;
            is_initial_connect = true;
            finalizing_connection = false;
            // stop the peerfinder on this specific page
            PeerFinder.ConnectionRequested -= PeerFinder_ConnectionRequested;

            PeerFinder.Stop();
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

        private async void display_first_run_dialog()
        {
            ContentDialog_settings cdiag_settings = new ContentDialog_settings();
            ContentDialogResult cdiag_result = await cdiag_settings.ShowAsync();



            if (cdiag_result == ContentDialogResult.Primary)
            {
                this.Frame.Navigate(typeof(settings_page));
            }
        }

        private async void display_missing_settings_dialog()
        {
            ContentDialog_missing_settings cdiag_settings = new ContentDialog_missing_settings();
            ContentDialogResult cdiag_result = await cdiag_settings.ShowAsync();

            if (cdiag_result == ContentDialogResult.Primary)
            {
                this.Frame.Navigate(typeof(settings_page));
            }
        }

        private void Refresh_peers_Click(object sender, RoutedEventArgs e)
        {
            bt_devices_list.Items.Clear();
            initialize_list();
        }

        private void abb_settings_Click(object sender, RoutedEventArgs e)
        {
            // navigate to a settings page
            Frame.Navigate(typeof(settings_page));
        }

        private async void initialize_list()
        {
            // on initialize list, scan for peers
            try
            {
                Debug.WriteLine("Finding Peers");
                var peers = await PeerFinder.FindAllPeersAsync();

                if (peers.Count > 0)
                {
                    foreach (var peer in peers)
                    {   
                        // only add if peer has a pin...... but this could backfire
                        if (peer.DisplayName.Substring(0, 1) == "[" && peer.DisplayName.Substring(5, 1) == "]")
                        {
                            bt_devices_list.Items.Add(peer);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == ERR_BLUETOOTH_OFF)
                {
                    Debug.WriteLine("Bluetooth is off");
                }

            }
        }


        private async void PeerFinder_ConnectionRequested(object sender, ConnectionRequestedEventArgs args)
        {


            try
            {
                // we will need two cases. of course the initial connection is what happens below. if it's not the initial connection, then we skip the accept and just go straight into it.
                if (is_initial_connect)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        ContentDialog diag_connection_requested = new ContentDialog();
                        diag_connection_requested.Title = "Connection requested!";
                        string prompt = args.PeerInformation.DisplayName.Substring(6) + " requests you to join the group!";
                        diag_connection_requested.PrimaryButtonText = "Accept";
                        diag_connection_requested.SecondaryButtonText = "Reject";
                        diag_connection_requested.Content = prompt;

                        ContentDialogResult connection_requested_result = await diag_connection_requested.ShowAsync();

                        if (connection_requested_result == ContentDialogResult.Primary)
                        {
                            grid_Loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            await connection_helper(args.PeerInformation);

                        }
                    });
                }
                else
                {
                    // we've connected before, user program now expects to receive the userlist with phone numbers

                    await connection_helper(args.PeerInformation);
                    finalizing_connection = false;

                }



            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }
           
        }


        private async Task<string> Get_Message()
        {
            if (_dataReader == null)
            {
                _dataReader = new DataReader(_socket.InputStream);
            }

            await _dataReader.LoadAsync(4);
            uint message_length = (uint)_dataReader.ReadInt32();
            await _dataReader.LoadAsync(message_length);
            return _dataReader.ReadString(message_length);
        }

        private async void SendMessage(string message)
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                // each message gets an extra header that is his username
                message = localSettings.Values[setting_username] + "[stop]" + message;
                if (_socket == null)
                {
                    Debug.WriteLine("socket broken");
                    return;
                }

                if (_dataWriter == null)
                {
                    _dataWriter = new DataWriter(_socket.OutputStream);
                }

                Debug.WriteLine("sending message");
                _dataWriter.WriteInt32(message.Length);
                await _dataWriter.StoreAsync();

                _dataWriter.WriteString(message);
                await _dataWriter.StoreAsync();
                Debug.WriteLine("message sent");
                //return 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //return -1; // -1 tells the sender to try again.
            }


        }

        private void close_connection()
        {
            if (_dataReader != null)
            {
                _dataReader.Dispose();
                _dataReader = null;
            }

            if (_dataWriter != null)
            {
                _dataWriter.Dispose();
                _dataWriter = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }


        private async void connect_to_selected_Click(object sender, RoutedEventArgs e)
        {
            // check to see if your settings are set
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.Count == 0)
            {
                // there are no settings, prompt user
                display_first_run_dialog();
            }
            else if (localSettings.Values[setting_username] == "" || localSettings.Values[setting_phone_no] == "")
            {
                // if the settings are invalid, ie. no username or no phone number make another prompt

                display_first_run_dialog();

            }

            else
            {
                // you're now the host for this instance
                I_am_a_the_host = true;
                // go through each of the selected items and establish a connection:

                if (!finished_connecting_to_peers)
                {
                    if (bt_devices_list.SelectedItems.Count != 0)
                    {
                        grid_Loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        finalizing_connection = true;
                        await connect_to_peers();
                        finished_connecting_to_peers = true;
                        // on finished connecting to peers change button content to be "start tracking"
                        grid_Loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        connect_to_selected_button.Content = "Start!";
                        cancel_button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    else
                    {
                        // prompt user to select users
                        ContentDiaglog_NoPeersSelected cdiag = new ContentDiaglog_NoPeersSelected();
                        await cdiag.ShowAsync();

                    }
                }
                else
                {
                    if (!finalizing_connection)
                    {
                        // no longer needs to connect, this click will now take us to the tracking page.
                        // pass the selected peers as an object into the next page
                        var selection = bt_devices_list.Items.ToArray();

                        //this.Frame.Navigate(typeof(Tracking_Page), phonebook);

                        this.Frame.Navigate(typeof(Tracking_Page), new KeyValuePair<string, Dictionary<string, string>>(session_pin, phonebook));
                    }
                }

            }

        }

        // connection_helper takes in PeerInformation peer and attempts to make a connection. On an unsuccessful connection, it prompts the user to try again or skip the peer.
        private async Task connection_helper(PeerInformation peer)
        {
            int connect_success = -1;

            connect_success = await connect(peer);


            if (connect_success == -1)
            {
                ContentDialog diag_connection_requested = new ContentDialog();
                diag_connection_requested.Title = "Couldn't connect!";
                string prompt = "There was some trouble connecting to " + peer.DisplayName.Substring(6);
                diag_connection_requested.PrimaryButtonText = "Try again";
                diag_connection_requested.SecondaryButtonText = "Skip";
                diag_connection_requested.Content = prompt;

                try
                {
                    ContentDialogResult connection_requested_result = await diag_connection_requested.ShowAsync();

                    if (connection_requested_result == ContentDialogResult.Primary)
                    {
                        connect_success = -1;
                        await connection_helper(peer);
                    }
                    else if (connection_requested_result == ContentDialogResult.Secondary)
                    {
                        connect_success = 1;
                    }
                }
                catch
                {
                    connect_success = -1;
                }



               

            }

            /* old
            while (connect_success == -1)
            {
                connect_success = await connect(peer);

                if (connect_success == -1)
                {
                    // the connect didn't work. ask to try again.
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        ContentDialog diag_connection_requested = new ContentDialog();
                        diag_connection_requested.Title = "Couldn't connect!";
                        string prompt = "There was some trouble connecting to " + peer.DisplayName;
                        diag_connection_requested.PrimaryButtonText = "Try again";
                        diag_connection_requested.SecondaryButtonText = "Skip";
                        diag_connection_requested.Content = prompt;

                        try
                        {
                            ContentDialogResult connection_requested_result = await diag_connection_requested.ShowAsync();

                            if (connection_requested_result == ContentDialogResult.Primary)
                            {
                                connect_success = -1;
                            }
                            else if (connection_requested_result == ContentDialogResult.Secondary)
                            {
                                connect_success = 1;
                                

                            }
                        }
                        catch
                        {
                            connect_success = -1;
                        }

                    });
                }
            }
             old */
        }

        // connect_to_peers() iterates through the selected peers and exchanges phone numbers. 
        private async Task connect_to_peers()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            connect_to_selected_button.Content = "Connecting...";
            foreach (PeerInformation peer in bt_devices_list.SelectedItems)
            {
                // connect to peer
                await connection_helper(peer);

                // double check to close connection.
                close_connection();
            }
            is_initial_connect = false;

            // For some reason, after a host device successfully connects to a peer, it can't connect again without first attempting a refresh.
            PeerFinder.Stop();
            PeerFinder.Start();
            await PeerFinder.FindAllPeersAsync();


            // after exchanging phone numbers, host now as a master list of phone numbers now go back and give the peerlist to the peers along with their phone numbers
            //connect_to_selected_button.Content = "Finalizing connection..."; <====

            foreach (PeerInformation peer in bt_devices_list.SelectedItems)
            {
                // connect to peer
                await connection_helper(peer);

                // double check to close connection
                close_connection();
            }

            finalizing_connection = false;
        }

        // initial_connect is used to establish that first connection where host and peer exchange info then disconnect
        private async Task<int> connect(PeerInformation peer)
        {
            if (is_initial_connect)
            {
                // this is the initial connect, just give the peer a request and receive their phone number
                try
                {
                    // make a a socket connection and wait for the peer to accept.
                    _socket = await PeerFinder.ConnectAsync(peer);

                    if (I_am_a_the_host)
                    {
                        // set session PIN to self
                        session_pin = (PeerFinder.DisplayName as string).Substring(0, 6);
                        await send_initial_connect_messages();
                    }
                    else
                    {
                        // set session PIN to peer's
                        session_pin = (peer.DisplayName as string).Substring(0, 6);

                        // this first listen_for_message listens for the initial connection made by a host.
                        await listen_for_message();

                        // as a peer, after this step, we've made an initial connection:
                        is_initial_connect = false;
                    }

                    //tb_output.Text = "Finished initial connect";
                    connect_to_selected_button.Content = "Finalizing Connection...";
                    finalizing_connection = true;
                    return 1;
                }
                catch
                {
                    // couldn't connect, return with a signal to retry.
                    return -1;
                }
            }
            else
            {
                try
                {
                    // make a socket connection and wait for the pper to accept
                    _socket = await PeerFinder.ConnectAsync(peer);

                    if (I_am_a_the_host)
                    {
                        await send_secondary_connect_messages((peer.DisplayName as string).Substring(6));
                    }
                    else
                    {
                        await listen_for_message();
                    }

                    //tb_output.Text = "Finished secondary connect";
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            connect_to_selected_button.Content = "Start!";
                            grid_Loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            cancel_button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            finished_connecting_to_peers = true;

                        });
                    //connect_to_selected_button.Content = "Start!";
                    return 1;
                }
                catch
                {
                    return -1;
                }
            }
        }

        private async Task send_initial_connect_messages()
        {
            Task<bool> listen = listen_for_message();

            SendMessage(msg_phone_request);

            bool ack = await listen;

            if (!ack)
            {
                // there was a message failure somehwere in the chain, re send it.
                await send_initial_connect_messages();
            }

        }
        private async Task send_secondary_connect_messages(string peerName)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            // first send self as an entry
            string message = msg_phone_book + localSettings.Values[setting_username] + "[to]" + localSettings.Values[setting_phone_no];
            await send_phonebook_entry(message);

            foreach (string k in phonebook.Keys)
            {
                //tb_output.Text = peerName + " vs " + k;
                if (peerName != k)
                {
                    // send each key-value pair from phone book 
                    message = msg_phone_book + k + "[to]" + phonebook[k];

                    Debug.WriteLine("sending: " + message);
                    await send_phonebook_entry(message);
                    Debug.WriteLine("sent complete");
                }

            }

            // finished phonebook. send message to peer telling them it's over. and to terminate the connection.

            SendMessage(msg_terminate);

            // terminate connection yourself
            close_connection();
        }

        private async Task<bool> listen_for_message()
        {
            try
            {
                var message = await Get_Message();

                // then give the message to the message_helper to interpret
                int ret = message_helper(message); // message_helper used to be task and this line used to await it. i guess it isnt required.

                switch (ret)
                {
                    case 1:
                        // case 1 means we responded to a phone number request. after sending the phone number we start listening again for the host to say that he's received our number
                        await listen_for_message();
                        return true;
                    case 2:
                        // case 2 occurs when host receives a phone number from its original request. this will trigger another call to listen_for_message so it can hear 
                        await listen_for_message();
                        return true;
                    case -1:
                        return false;
                    case 3:
                        close_connection();
                        return true;
                    case 4:
                        // host receives awaiting message from peer. proceeds with next phonebook entry send
                        return true;
                }


                return false;

            }
            catch (Exception)
            {
                close_connection();
                return false;
            }
        }



        // 3 possible returns (1: receive more, -1: error, try previous action again, 2: receive more 3: terminate
        private int message_helper(string message)
        {
            Debug.WriteLine(message);
            //tb_output.Text = message;

            var split_message = message.Split(stringSeparators, StringSplitOptions.None);

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string to_send = "";

            string header = split_message[1].Substring(0, 5);
            string msg_content = split_message[1].Substring(5);

            if (header == msg_phone_request)
            {
                // if we receive a phone number request, respond by giving this device's associated phone number.
                to_send = msg_phone + (localSettings.Values[setting_phone_no] as string);

                SendMessage(to_send);
                // return with status 1:
                return 1;
            }
            else if (header == msg_phone)
            {
                // received a phone number, save it in our dictionary
                //phonebook.Add(split_message[0], msg_content);
                phonebook[split_message[0]] = msg_content;
                SendMessage(msg_ack + msg_copy);

                // return with status 1 or 2, they're the same
                return 2;


            }
            else if (header == msg_phone_book)
            {
                // this is the start of a phone book message
                var dict_pair = msg_content.Split(stringSeparators_dict_message, StringSplitOptions.None);

                //phonebook.Add(dict_pair[0], dict_pair[1]);
                phonebook[dict_pair[0]] = dict_pair[1];
                SendMessage(msg_awaiting);
                return 2;
            }
            else if (header == msg_terminate)
            {
                return 3;
            }
            else if (header == msg_ack)
            {
                // host has acknowledged receipt, tell them to terminate, then terminate self.
                SendMessage(msg_terminate);
                return 3;
            }
            else if (header == msg_awaiting)
            {
                return 4;
            }

            Debug.WriteLine(message);


            return -1;
        }


        private async Task send_phonebook_entry(string message)
        {
            Task<bool> listen = listen_for_message();

            // send user-phone list and wait for ack 
            SendMessage(message);
            bool ack = await listen;

            if (!ack)
            {
                // there was a message failure somehwere in the chain, re send it.
                await send_phonebook_entry(message);
            }

        }



        private async void cancel_button_Click(object sender, RoutedEventArgs e)
        {

            Grid contentGrid = new Grid() { Width = Window.Current.Bounds.Width, Height = double.NaN };
            TextBlock contentText = new TextBlock() { Text = "Click yes to start over.", TextWrapping = TextWrapping.Wrap };
            contentGrid.Children.Add(contentText);

            ContentDialog exitSessionDialog = new ContentDialog() { Title = "Cancel Connection?", PrimaryButtonText = "Yes", SecondaryButtonText = "No" };
            exitSessionDialog.Content = contentGrid;

            ContentDialogResult cdiag_result = await exitSessionDialog.ShowAsync();

            if (cdiag_result == ContentDialogResult.Primary)
            {
                ReinitializeMainPage();
                cancel_button.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void abb_about_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }



    }
}
