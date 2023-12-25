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

    public class DanilovesMacro : Action
    {
        // Import the mouse_event function from the Windows API
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        
        public static string ACTION_NAME_MACRO_SWITCH = "DanilovesMacroSwitch";

        public string actionName { get; set; }
        private _4RThread thread;
        public List<ChainConfig> chainConfigs { get; set; } = new List<ChainConfig>();

        public DanilovesMacro(string macroname, int macroLanes)
        {
            this.actionName = macroname;
            for(int i = 1; i <= macroLanes; i++)
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
            catch (Exception) { }
            
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
                if (true)
                {
                    Dictionary<string, MacroKey> macro = chainConfig.macroEntries;
                    for (int i = 1; i <= macro.Count; i++)//Ensure to execute keys in Order
                    {
                        MacroKey macroKey = macro["in" + i + "mac" + chainConfig.id];
                        if (macroKey.key != Key.None)
                        {
                            Keys thisk = (Keys)Enum.Parse(typeof(Keys), macroKey.key.ToString());
                            Thread.Sleep(macroKey.delay);
                            Interop.PostMessage(roClient.process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, thisk, 0);
                            Point cursorPos = System.Windows.Forms.Cursor.Position;
                            Interop.PostMessage(roClient.process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, thisk, 0);
                            mouse_event(Constants.MOUSEEVENTF_LEFTDOWN, (uint)cursorPos.X, (uint)cursorPos.Y, 0, 0);
                            Thread.Sleep(1);
                            mouse_event(Constants.MOUSEEVENTF_LEFTUP, (uint)cursorPos.X, (uint)cursorPos.Y, 0, 0);
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
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = new _4RThread((_) => MacroExecutionThread(roClient));
                _4RThread.Start(this.thread);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
        }
    }
}
