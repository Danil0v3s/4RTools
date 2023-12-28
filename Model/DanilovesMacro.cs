using System;
using System.Collections.Generic;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Threading;
using _4RTools.Utils;
using System.Windows.Forms;

namespace _4RTools.Model
{
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Timers;

    public class DanilovesMacro : Action
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);

        public static string ACTION_NAME_MACRO_SWITCH = "DanilovesMacroSwitch";

        public string actionName { get; set; }
        private _4RThread thread;
        private Timer timer;
        public List<ChainConfig> chainConfigs { get; set; } = new List<ChainConfig>();

        public DanilovesMacro(string macroname, int macroLanes)
        {
            this.actionName = macroname;
            for (int i = 1; i <= macroLanes; i++)
            {
                chainConfigs.Add(new ChainConfig(i, Key.None));
            }
        }

        public void ResetMacro(int macroId)
        {
            try
            {
                chainConfigs[macroId - 1] = new ChainConfig(macroId);
            }
            catch (Exception) {}
        }

        public string GetActionName()
        {
            return this.actionName;
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        private int MacroExecutionThread(Client roClient)
        {
            foreach (ChainConfig chainConfig in this.chainConfigs)
            {
                if (chainConfig.trigger != Key.None)
                {
                    Dictionary<string, MacroKey> macro = chainConfig.macroEntries;
                    for (int i = 1; i <= macro.Count; i++)//Ensure to execute keys in Order
                    {
                        MacroKey macroKey = macro["in" + i + "mac" + chainConfig.id];
                        if (macroKey.key != Key.None)
                        {
                            Keys thisk = (Keys)Enum.Parse(typeof(Keys), macroKey.key.ToString());
                            Interop.PostMessage(roClient.process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, thisk, 0);
                            Point cursorPos = System.Windows.Forms.Cursor.Position;
                            Thread.Sleep(1);
                            Interop.mouse_event(Constants.MOUSEEVENTF_LEFTDOWN, cursorPos.X, cursorPos.Y, 0, 0);
                            Thread.Sleep(1);
                            Interop.mouse_event(Constants.MOUSEEVENTF_LEFTUP, cursorPos.X, cursorPos.Y, 0, 0);
                            Thread.Sleep(macroKey.delay);
                        }
                    }
                }
            }
            Thread.Sleep(100);
            return 0;
        }

        public void Start()
        {
            Stop();
            ApplyBuff(null,null);
            StartBuffTimer();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = new _4RThread((_) => MacroExecutionThread(roClient));
                _4RThread.Start(this.thread);
            }
        }

        private void StartBuffTimer()
        {
            timer?.Stop();
            timer = new Timer(TimeSpan.FromSeconds(120).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += ApplyBuff;
            timer.Start();
        }

        private void ApplyBuff(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                Keys thisk = (Keys)Enum.Parse(typeof(Keys), "Oem5");
                Thread.Sleep(1);
                Interop.PostMessage(roClient.process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, thisk, 0);
            }
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= ApplyBuff;
            }
            _4RThread.Stop(this.thread);
        }
    }
}