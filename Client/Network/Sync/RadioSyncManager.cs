// using System;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;
// using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
// using Ciribob.SRS.Common;
// using Ciribob.SRS.Common.PlayerState;
// using Newtonsoft.Json;
// using NLog;
//
// /**
// Keeps radio information in Sync Between DCS and
//
// **/
//
// namespace Ciribob.SRS.Client.Network.Sync
// {
//     public class RadioSyncManager
//     {
//         private readonly SendRadioUpdate _clientRadioUpdate;
//         private readonly ClientSideUpdate _clientSideUpdate;
//         public static readonly string AWACS_RADIOS_FILE = "awacs-radios.json";
//         private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
//
//         private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;
//         private readonly RadioSyncHandler _radioSyncHandler;
//
//         public delegate void ClientSideUpdate();
//         public delegate void SendRadioUpdate();
//
//         private volatile bool _stopExternalAWACSMode;
//
//         private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance; 
//
//         public bool IsListening { get; private set; }
//
//         public RadioSyncManager(SendRadioUpdate clientRadioUpdate, ClientSideUpdate clientSideUpdate,
//            string guid, RadioSyncHandler.NewAircraft _newAircraftCallback)
//         {
//             _clientRadioUpdate = clientRadioUpdate;
//             _clientSideUpdate = clientSideUpdate;
//             IsListening = false;
//             _radioSyncHandler = new RadioSyncHandler(clientRadioUpdate, _newAircraftCallback);
//         }
//
//       
//
//         public void Start()
//         {
//             IsListening = true;
//         }
//
//         public void StartExternalAWACSModeLoop()
//         {
//             _stopExternalAWACSMode = false;
//         }
//
//        
//
//
//         public void Stop()
//         {
//             _stopExternalAWACSMode = true;
//             IsListening = false;
//
//             _radioSyncHandler.Stop();
//
//         }
//     }
// }