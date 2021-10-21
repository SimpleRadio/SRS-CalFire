// using System;
// using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
// using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
// using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
// using Ciribob.SRS.Common;
// using Ciribob.SRS.Common.PlayerState;
// using NLog;
//
// namespace Ciribob.SRS.Client.Network.Sync
// {
//     public class RadioSyncHandler
//     {
//         private readonly RadioSyncManager.SendRadioUpdate _radioUpdate;
//         private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
//
//         private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;
//         private readonly SyncedServerSettings _serverSettings = SyncedServerSettings.Instance;
//         private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance;
//
//         private static readonly int RADIO_UPDATE_PING_INTERVAL = 60; //send update regardless of change every X seconds
//
//
//         private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;
//
//         private volatile bool _stop;
//
//         public delegate void NewAircraft(string name);
//
//         private readonly NewAircraft _newAircraftCallback;
//
//         private long _identStart = 0;
//
//         public RadioSyncHandler(RadioSyncManager.SendRadioUpdate radioUpdate, NewAircraft _newAircraft)
//         {
//             _radioUpdate = radioUpdate;
//             _newAircraftCallback = _newAircraft;
//         }
//
//         public void Start()
//         {
//             //reset last sent
//             _clientStateSingleton.LastSent = 0;
//         }
//
//
//         public void ProcessRadioInfo(PlayerUnitState message)
//         {
//           
//             // determine if its changed by comparing old to new
//             var update = UpdateRadio(message);
//
//             var diff = new TimeSpan( DateTime.Now.Ticks - _clientStateSingleton.LastSent);
//
//             if (update 
//                 || _clientStateSingleton.LastSent < 1 
//                 || diff.TotalSeconds > 60)
//             {
//                 Logger.Debug("Sending Radio Info To Server - Update");
//                 _clientStateSingleton.LastSent = DateTime.Now.Ticks;
//                 _radioUpdate();
//             }
//         }
//
//
//         private bool UpdateRadio(PlayerUnitState message)
//         {
//           
//             var playerRadioInfo = _clientStateSingleton.PlayerUnitState;
//
//             //copy and compare to look for changes
//             var beforeUpdate = playerRadioInfo.DeepClone();
//
//             //update common parts
//             playerRadioInfo.Name = message.Name;
//             playerRadioInfo.inAircraft = message.inAircraft;
//             playerRadioInfo.IntercomHotMic = message.IntercomHotMic;
//
//             playerRadioInfo.control = message.control;
//             
//
//             playerRadioInfo.simultaneousTransmissionControl = message.simultaneousTransmissionControl;
//
//             playerRadioInfo.UnitType = message.UnitType;
//
//          
//             var overrideFreqAndVol = false;
//
//             var newAircraft = playerRadioInfo.UnitId != message.UnitId || !playerRadioInfo.IsCurrent();
//
//             overrideFreqAndVol = playerRadioInfo.UnitId != message.UnitId;
//             
//             //save unit id
//             playerRadioInfo.UnitId = message.UnitId;
//             
//
//             if (newAircraft)
//             {
//                 if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.AutoSelectSettingsProfile))
//                 {
//                     _newAircraftCallback(message.UnitType);
//                 }
//
//                 playerRadioInfo.Transponder = message.Transponder;
//             }
//
//             if (overrideFreqAndVol)
//             {
//                 playerRadioInfo.SelectedRadio = message.SelectedRadio;
//             }
//
//             if (playerRadioInfo.control == PlayerUnitState.RadioSwitchControls.IN_COCKPIT)
//             {
//                 playerRadioInfo.SelectedRadio = message.SelectedRadio;
//             }
//
//             bool simul = false;
//
//
//             //copy over radio names, min + max
//             for (var i = 0; i < playerRadioInfo.Radios.Length; i++)
//             {
//                 var clientRadio = playerRadioInfo.Radios[i];
//
//                 //if we have more radios than the message has
//                 if (i >= message.Radios.Length)
//                 {
//                     clientRadio.Frequency = 1;
//                     clientRadio.freqMin = 1;
//                     clientRadio.freqMax = 1;
//                     clientRadio.SecondaryFrequency = 0;
//                     clientRadio.Retransmit = false;
//                     clientRadio.Modulation = RadioConfig.Modulation.DISABLED;
//                     clientRadio.Name = "No Radio";
//                     clientRadio.rtMode = RadioConfig.RetransmitMode.DISABLED;
//                     clientRadio.Retransmit = false;
//
//                     clientRadio.freqMode = RadioConfig.FreqMode.COCKPIT;
//                     clientRadio.guardFreqMode = RadioConfig.FreqMode.COCKPIT;
//                     clientRadio.volMode = RadioConfig.VolumeMode.COCKPIT;
//
//                     continue;
//                 }
//
//                 var updateRadio = message.Radios[i];
//
//
//                 if (updateRadio.Modulation == RadioConfig.Modulation.DISABLED)
//                 {
//                     //expansion radio, not allowed
//                     clientRadio.Frequency = 1;
//                     clientRadio.freqMin = 1;
//                     clientRadio.freqMax = 1;
//                     clientRadio.SecondaryFrequency = 0;
//                     clientRadio.Retransmit = false;
//                     clientRadio.Modulation = RadioConfig.Modulation.DISABLED;
//                     clientRadio.Name = "No Radio";
//                     clientRadio.rtMode = RadioConfig.RetransmitMode.DISABLED;
//                     clientRadio.Retransmit = false;
//
//                     clientRadio.freqMode = RadioConfig.FreqMode.COCKPIT;
//                     clientRadio.guardFreqMode = RadioConfig.FreqMode.COCKPIT;
//                     clientRadio.volMode = RadioConfig.VolumeMode.COCKPIT;
//                 }
//                 else
//                 {
//                     //update common parts
//                     clientRadio.freqMin = updateRadio.freqMin;
//                     clientRadio.freqMax = updateRadio.freqMax;
//
//                   
//
//                     if (updateRadio.SimultaneousTransmission)
//                     {
//                         simul = true;
//                     }
//
//                     clientRadio.Name = updateRadio.Name;
//
//                     clientRadio.Modulation = updateRadio.Modulation;
//
//                     //update modes
//                     clientRadio.freqMode = updateRadio.freqMode;
//                     clientRadio.guardFreqMode = updateRadio.guardFreqMode;
//                     clientRadio.rtMode = updateRadio.rtMode;
//
//
//                     clientRadio.volMode = updateRadio.volMode;
//
//                     if ((updateRadio.freqMode == RadioConfig.FreqMode.COCKPIT) || overrideFreqAndVol)
//                     {
//                         clientRadio.Frequency = updateRadio.Frequency;
//
//                         if (newAircraft && updateRadio.guardFreqMode == RadioConfig.FreqMode.OVERLAY)
//                         {
//                             //default guard to off
//                             clientRadio.SecondaryFrequency = 0;
//                         }
//                         else
//                         {
//                             if (clientRadio.SecondaryFrequency != 0 && updateRadio.guardFreqMode == RadioConfig.FreqMode.OVERLAY)
//                             {
//                                 //put back
//                                 clientRadio.SecondaryFrequency = updateRadio.SecondaryFrequency;
//                             }
//                             else if (clientRadio.SecondaryFrequency == 0 && updateRadio.guardFreqMode == RadioConfig.FreqMode.OVERLAY)
//                             {
//                                 clientRadio.SecondaryFrequency = 0;
//                             }
//                             else
//                             {
//                                 clientRadio.SecondaryFrequency = updateRadio.SecondaryFrequency;
//                             }
//
//                         }
//
//                         clientRadio.CurrentChannel = updateRadio.CurrentChannel;
//                     }
//                     else
//                     {
//                         if (clientRadio.SecondaryFrequency != 0)
//                         {
//                             //put back
//                             clientRadio.SecondaryFrequency = updateRadio.SecondaryFrequency;
//                         }
//
//                         //check we're not over a limit
//                         if (clientRadio.Frequency > clientRadio.freqMax)
//                         {
//                             clientRadio.Frequency = clientRadio.freqMax;
//                         }
//                         else if (clientRadio.Frequency < clientRadio.freqMin)
//                         {
//                             clientRadio.Frequency = clientRadio.freqMin;
//                         }
//                     }
//
//                     //reset encryption
//                     if (overrideFreqAndVol)
//                     {
//                         clientRadio.Enc = false;
//                         clientRadio.EncryptionKey = 0;
//                     }
//
//                  
//                     {
//                         clientRadio.Enc = false;
//                         clientRadio.EncryptionKey = 0;
//                     }
//
//                     //handle volume
//                     if ((updateRadio.volMode == RadioConfig.VolumeMode.COCKPIT) || overrideFreqAndVol)
//                     {
//                         clientRadio.Volume = updateRadio.Volume;
//                     }
//
//                     //handle Retransmit mode
//                     if ((updateRadio.rtMode == RadioConfig.RetransmitMode.COCKPIT))
//                     {
//                         clientRadio.rtMode = updateRadio.rtMode;
//                         clientRadio.Retransmit = updateRadio.Retransmit;
//                     }else if (updateRadio.rtMode == RadioConfig.RetransmitMode.DISABLED)
//                     {
//                         clientRadio.rtMode = updateRadio.rtMode;
//                         clientRadio.Retransmit = false;
//                     }
//
//                     //handle Channels load for radios
//                     if (newAircraft && i > 0)
//                     {
//                         if (clientRadio.freqMode == RadioConfig.FreqMode.OVERLAY)
//                         {
//                             var channelModel = _clientStateSingleton.FixedChannels[i - 1];
//                             channelModel.Max = clientRadio.freqMax;
//                             channelModel.Min = clientRadio.freqMin;
//                             channelModel.Reload();
//                             clientRadio.CurrentChannel = -1; //reset channel
//
//                             if (_globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.AutoSelectPresetChannel))
//                             {
//                                 RadioHelper.RadioChannelUp(i);
//                             }
//                         }
//                         else
//                         {
//                             _clientStateSingleton.FixedChannels[i - 1].Clear();
//                             //clear
//                         }
//                     }
//                 }
//             }
//
//
//            
//             playerRadioInfo.ptt = false;
//
//             //                }
//             //            }
//
//             //update
//             playerRadioInfo.LastUpdate = DateTime.Now.Ticks;
//
//             return !beforeUpdate.Equals(playerRadioInfo);
//         }
//
//         public void Stop()
//         {
//             _stop = true;
//           
//
//         }
//
//     }
// }

