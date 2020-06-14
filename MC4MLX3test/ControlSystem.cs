using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
                                                        // For UI support you would add Crestron.SimplSharpPro.UI 
using Crestron.SimplSharpPro.Remotes;                   // For RF remote support
using Crestron.SimplSharpPro.EthernetCommunication;     // for EISC support

namespace MC4MLX3test
{
    public class ControlSystem : CrestronControlSystem
    {
        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        ///
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        ///
        /// You cannot send / receive data in the constructor
        /// </summary>
        ///
        private Mlx3 remote01;  // required adding Crestron.SimplSharpPro.Remotes reference.
        public ThreeSeriesTcpIpEthernetIntersystemCommunications eisc; // for the eisc to talk to another system
        private const string LogHeader = "[Device] "; // used for writing error messages.
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);
                
                // below is setup code to talk to MLX3 on internal gateway
                remote01 = new Mlx3(0x30, this.ControllerRFGatewayDevice );  // reference the internal gateway for the paramGateway field
                this.remote01.ButtonStateChange += new ButtonEventHandler(this.Remote_SigChange ); // setup routine to handle a button press

                this.remote01.OnlineStatusChange +=
                    this.Remote_OnlineStatusChange ;                             // setup routine to handle online/offline status change
                if (this.remote01.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error(string.Format(LogHeader + "Error registering RF Remote: {0}", this.remote01.RegistrationFailureReason));
                }
                else
                {
                    // if we wanted to do something special here
                }

        /// below is the setup code to talk to other processor via EISC
        
                eisc = new ThreeSeriesTcpIpEthernetIntersystemCommunications(0x51, "192.168.2.64", this);
                if (eisc.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error("Error registering EISC IPID Reason {1}",eisc.RegistrationFailureReason);
                    CrestronConsole.PrintLine("Error registering EISC IPID Reason {1}", eisc.RegistrationFailureReason);
                
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor
        /// has finished.
        ///
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        ///
        /// Please be aware that InitializeSystem needs to exit quickly also;
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {
                remote01 = new Mlx3(0x30, this.ControllerRFGatewayDevice );  // reference the internal gateway for the paramGateway field
                
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down.
        /// Use these events to close / re-open sockets, etc.
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values
        /// such as whether it's a Link Up or Link Down event. It will also indicate
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for.
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        ///
        public void Remote_SigChange(GenericBase currentDevice, ButtonEventArgs args)
        {
            /*switch (args.Button.Name)  /// you can use the button name returned to perform actions, or you can use button number. 
            {
                case eButtonName.VolumeUp:
                    ErrorLog.Notice(string.Format(LogHeader + "Volume Up"));
                    break;
                case eButtonName.VolumeDown:
                    ErrorLog.Notice(string.Format(LogHeader + "Volume Down"));
                    break;
                case eButtonName.Mute:
                    ErrorLog.Notice(string.Format(LogHeader + "Mute"));
                    break;
                default:
                    ErrorLog.Notice(string.Format(LogHeader + "Button: {0}, {1}", args.Button.Number, args.Button.Name));
                    break; 
            }*/
            if (args.Button.State == eButtonState.Pressed) // we only care about Pressed or !Pressed 
                 
            {
                eisc.BooleanInput[args.Button.Number].BoolValue = true;  // we are doing a 1-to-1 correspondence of button number to digital on the EISC
                ErrorLog.Notice(String.Format(LogHeader + "Button {0} has state {1}",args.Button.Name, args.Button.State)); 
            }
            else
            {
                eisc.BooleanInput[args.Button.Number].BoolValue = false;
            }
            
        }
        public void Remote_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
            {
                // if it was remote01 that triggered the event
                if (currentDevice == this.remote01)
                {
                    // ErrorLog.Notice(string.Format(LogHeader + "{0} is online", tp01.Type));
                    // this.remote01.BooleanInput[11].BoolValue = args.DeviceOnLine;
                    ErrorLog.Notice(string.Format(LogHeader + "{0} is online", remote01.Type));
                }
            }
            else
            {
                // ErrorLog.Notice(string.Format(LogHeader + "{0} is offline", currentDevice.Description));
                ErrorLog.Notice(string.Format(LogHeader + "{0} is offline", remote01.Type));
            }
        }
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads.
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting.
                    //Very limited time to preform clean up and save any settings to disk.
                    ErrorLog.Warn(string.Format("Help Meeeee....."));
                    break;
            }

        }
    }
}
