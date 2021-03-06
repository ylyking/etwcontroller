﻿using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ETWController.ETW
{
    /// <summary>
    /// ETW Provider for window and mouse hook messages.
    /// </summary>
    [EventSource(Guid = "F0A5DA64-0FBC-4D41-B6C7-BF56A0601D7D", Name = "HookEvents")]
    public sealed class HookEvents : EventSource
    {
        /// <summary>
        /// ETW provider to log keyboard and mouse events to ETW to enable coherent analysis between user actions and the system reactions.
        /// </summary>
        public static HookEvents ETWProvider = new HookEvents();
        static string EventRegisterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "eventRegister.exe");


        /// <summary>
        /// ctor
        /// </summary>
        HookEvents() : base(true)
        {
        }

        /// <summary>
        /// Check if ETW manifest is already installed.
        /// </summary>
        /// <returns></returns>
        public static bool IsAlreadyRegistered()
        {
            string fullExeName = Process.GetCurrentProcess().MainModule.FileName;
            string manifestName = Path.ChangeExtension(fullExeName, null) + "." + typeof(HookEvents).Name + ".etwManifest.man";
            return File.Exists(manifestName);
        }


        /// <summary>
        /// Register as regular event provider in the system to make xperf happy. The pure dynamic registration less provider approach
        /// does not work out well if you need to do a full rundown to dump the manifest into the ETW stream which currently only PerfView does.
        /// By registering it in the old fashioned way WPA does also display the event type as name and not the cryptic guid. 
        /// </summary>
        public static string RegisterItself()
        {
            if (Process.GetCurrentProcess().MainModule.FileName.Contains("ETWController.exe"))
            {
                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = EventRegisterPath,
                    Arguments = String.Format("\"{0}\"", Assembly.GetExecutingAssembly().Location),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };

                var register = Process.Start(info);
                string output = register.StandardOutput.ReadToEnd();
                register.WaitForExit();
                return output;
            }
            else
                return "";

        }

        public static string UnregisterItself()
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = EventRegisterPath,
                Arguments = "-uninstall " + String.Format("\"{0}\"", Assembly.GetExecutingAssembly().Location),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            var register = Process.Start(info);
            string output = register.StandardOutput.ReadToEnd();
            register.WaitForExit();
            return output;
        }

        [Event(1,Level=EventLevel.Informational, Opcode=EventOpcode.Info, Task=Tasks.MouseButtonDown)]
        public void MouseButton(int EventNumber, string MouseButton, int x, int y)
        {
            WriteEvent(1, EventNumber, MouseButton, x, y);
        }

        /// <summary>
        /// High frequency event where all mouse move events are recorded
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [Event(2, Level = EventLevel.Verbose, Opcode = EventOpcode.Info, Task=Tasks.MouseMove)]
        public void MouseMove(int EventNumber, int x, int y)
        {
            WriteEvent(2, EventNumber, x, y);
        }

        /// <summary>
        /// Mouse wheel event where the mouse wheel is rolled forward or backward.
        /// </summary>
        /// <param name="WheelDelta"></param>
        /// <param name="x">Mouse position.</param>
        /// <param name="y">Mouse position.</param>
        [Event(3, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Task=Tasks.MouseWheel)]
        public void MouseWheel(int EventNumber, int WheelDelta, int x, int y)
        {
            WriteEvent(3, EventNumber, WheelDelta, x, y);
        }

        /// <summary>
        /// Key was pressed.
        /// </summary>
        /// <param name="Key">Stringified version of key name.</param>
        [Event(4, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Task = Tasks.KeyDown)]
        public void KeyDown(int EventNumber, string Key)
        {
            WriteEvent(4, EventNumber, Key);
        }

        /// <summary>
        /// Write a slow event which is fired when the hotkey (mouse or keyboard) is pressed to signal a slow condition noticed by the user.
        /// </summary>
        /// <param name="SlowMessage">Message describing the observed slowness</param>
        [Event(5, Level=EventLevel.LogAlways, Opcode=EventOpcode.Info, Task=Tasks.Slow)]
        public void SlowMarker(int EventNumber, string SlowMessage)
        {
            WriteEvent(5, EventNumber, SlowMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NetworkMessage"></param>
        [Event(6, Level=EventLevel.Informational, Task = Tasks.RemoteReceived)]
        public void FromNetworkReceived(int EventNumber, string NetworkMessage)
        {
            WriteEvent(6, EventNumber, NetworkMessage);
        }

        /// <summary>
        /// Write a fast event which is fired when the hotkey (mouse or keyboard) is pressed to signal a fast condition noticed by the user.
        /// </summary>
        /// <param name="SlowMessage">Message describing the observed slowness</param>
        [Event(7, Level = EventLevel.LogAlways, Opcode = EventOpcode.Info, Task = Tasks.Fast)]
        public void FastMarker(int EventNumber, string FastMesage)
        {
            WriteEvent(7, EventNumber, FastMesage);
        }

        /// <summary>
        /// Write a fast event which is fired when the hotkey (mouse or keyboard) is pressed to signal a fast condition noticed by the user.
        /// </summary>
        /// <param name="SlowMessage">Message describing the observed slowness</param>
        [Event(8, Level = EventLevel.LogAlways, Opcode = EventOpcode.Info, Task = Tasks.Screenshot)]
        public void Screenshot(string ScreenshotDescription, string ScreenshotFileName)
        {
            WriteEvent(8, ScreenshotDescription, ScreenshotFileName);
        }


        class Tasks 
        {
            public const EventTask Slow = (EventTask)0x1;
            public const EventTask MouseButtonDown = (EventTask)0x2;
            public const EventTask MouseMove = (EventTask)0x3;
            public const EventTask MouseWheel = (EventTask)0x4;

            public const EventTask KeyDown = (EventTask)        0x5;
            public const EventTask RemoteReceived = (EventTask) 0x6;
            public const EventTask Fast = (EventTask)0x7;
            public const EventTask Screenshot = (EventTask)0x8;

        }
    }


}
