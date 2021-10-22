// using System;
// using System.ComponentModel;
// using System.Windows.Threading;
// using Ciribob.SRS.Client.Network;
// using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.RadioChannels;
// using Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.Common.PresetChannels;
// using Ciribob.SRS.Common;
// using Ciribob.SRS.Common.Network;
// using Ciribob.SRS.Common.Network.Models;
//
// namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons
// {
//     public sealed class ClientStateSingleton : INotifyPropertyChanged
//     {
//         private static volatile ClientStateSingleton _instance;
//         private static object _lock = new object();
//
//         public delegate bool RadioUpdatedCallback();
//
//
//         public event PropertyChangedEventHandler PropertyChanged;
//
//         public PlayerUnitState PlayerUnitState { get; }
//         public PlayerSideInfo PlayerCoaltionLocationMetadata { get; set; }
//
//
//         // Timestamp for the last time 
//
//         //store radio channels here?
//         public PresetChannelsViewModel[] FixedChannels { get; }
//
//         public long LastSent { get; set; }
//
//
//         private static readonly DispatcherTimer _timer = new DispatcherTimer();
//
//         public RadioSendingState RadioSendingState { get; set; }
//         public RadioReceivingState[] RadioReceivingState { get; }
//
//         private bool isConnected;
//
//         public bool IsConnected
//         {
//             get => isConnected;
//             set
//             {
//                 isConnected = value;
//                 NotifyPropertyChanged("IsConnected");
//             }
//         }
//
//         private bool isVoipConnected;
//
//         public bool IsVoipConnected
//         {
//             get => isVoipConnected;
//             set
//             {
//                 isVoipConnected = value;
//                 NotifyPropertyChanged("IsVoipConnected");
//             }
//         }
//
//         private bool isConnectionErrored;
//         public string ShortGUID { get; }
//
//         public bool IsConnectionErrored
//         {
//             get => isConnectionErrored;
//             set
//             {
//                 isConnectionErrored = value;
//                 NotifyPropertyChanged("isConnectionErrored");
//             }
//         }
//
//
//         public string LastSeenName { get; set; }
//
//         private ClientStateSingleton()
//         {
//             RadioSendingState = new RadioSendingState();
//             RadioReceivingState = new RadioReceivingState[11];
//
//             ShortGUID = ShortGuid.NewGuid();
//             PlayerUnitState = new PlayerUnitState();
//             PlayerCoaltionLocationMetadata = new PlayerSideInfo();
//
//             FixedChannels = new PresetChannelsViewModel[10];
//
//             LastSent = 0;
//
//             IsConnected = false;
//
//             LastSeenName = Settings.GlobalSettingsStore.Instance
//                 .GetClientSetting(Settings.GlobalSettingsKeys.LastSeenName).RawValue;
//         }
//
//         public static ClientStateSingleton Instance
//         {
//             get
//             {
//                 if (_instance == null)
//                     lock (_lock)
//                     {
//                         if (_instance == null)
//                             _instance = new ClientStateSingleton();
//                     }
//
//                 return _instance;
//             }
//         }
//
//         public int IntercomOffset { get; set; }
//
//         private void NotifyPropertyChanged(string propertyName = "")
//         {
//             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//         }
//     }
// }