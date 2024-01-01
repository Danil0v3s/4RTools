namespace _4RTools.Model
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;
    using System.Windows.Input;
    using _4RTools.Utils;
    using Newtonsoft.Json;
    using Cursor=System.Windows.Forms.Cursor;

    public class DanilovesMacro : Action
    {
        public static string ACTION_NAME_MACRO_SWITCH = "DanilovesMacroSwitch";

        public string actionName { get; set; }
        private _4RThread thread;

        private string _startingMap;
        private List<EffectStatusIDs> _statusList = new();

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
            if (DidCurrentMapChange(roClient))
                return 0;

            ProcessMacro(roClient);
            CheckForStatuses(roClient);

            Thread.Sleep(100);
            return 0;
        }
        private void CheckForStatuses(Client roClient)
        {
            var foundStatuses = new List<EffectStatusIDs>();
            for (var i = 0; i <= Constants.MAX_BUFF_LIST_INDEX_SIZE - 1; i++)
            {
                var currentStatus = roClient.CurrentBuffStatusCode(i);
                var status = (EffectStatusIDs)currentStatus;
                if (!Enum.IsDefined(typeof(EffectStatusIDs), status))
                    continue;

                if (!_statusList.Contains(status))
                    continue;
                
                foundStatuses.Add(status);
            }

            if (!foundStatuses.Contains(EffectStatusIDs.EFST_CLIENT_ONLY_EQUIP_ARROW))
            {
                PressKey(roClient, Keys.W); // use quiver
                PressKey(roClient, Keys.Q); // equip arrow
            }
            if (!foundStatuses.Contains(EffectStatusIDs.FOOD_DEX)) {  PressKey(roClient, Keys.Oem5); } // keyboard backslash "\"
            if (!foundStatuses.Contains(EffectStatusIDs.OVERLAPEXPUP)) {  PressKey(roClient, Keys.I); }
            if (!foundStatuses.Contains(EffectStatusIDs.CASH_RECEIVEITEM)) { PressKey(roClient, Keys.O); }
            if (!foundStatuses.Contains(EffectStatusIDs.EFST_WEIGHTOVER90)) { PressKey(roClient, Keys.Oem6); } // keyboard "]"
        }
        private bool DidCurrentMapChange(Client roClient)
        {
            if (ClientSingleton.GetClient().ReadMapName() == _startingMap)
                return false;

            Keys thisk = Keys.Insert;
            Interop.PostMessage(roClient.process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, thisk, 0);
            Thread.Sleep(200);

            return true;
        }

        private void ProcessMacro(Client roClient)
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
                            PressKey(roClient, (Keys)Enum.Parse(typeof(Keys), macroKey.key.ToString()));
                            Point cursorPos = Cursor.Position;
                            Interop.mouse_event(Constants.MOUSEEVENTF_LEFTDOWN, cursorPos.X, cursorPos.Y, 0, 0);
                            Thread.Sleep(1);
                            Interop.mouse_event(Constants.MOUSEEVENTF_LEFTUP, cursorPos.X, cursorPos.Y, 0, 0);
                            Thread.Sleep(macroKey.delay);
                        }
                    }
                }
            }
        }
        
        private static void PressKey(Client roClient, Keys thisk)
        {
            Interop.PostMessage(roClient.process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, thisk, 0);
            Thread.Sleep(1);
        }

        public void Start()
        {
            _startingMap = ClientSingleton.GetClient().ReadMapName();
            Stop();
            
            _statusList.Add(EffectStatusIDs.EFST_CLIENT_ONLY_EQUIP_ARROW);
            _statusList.Add(EffectStatusIDs.FOOD_DEX);
            _statusList.Add(EffectStatusIDs.OVERLAPEXPUP);
            _statusList.Add(EffectStatusIDs.CASH_RECEIVEITEM);
            _statusList.Add(EffectStatusIDs.EFST_WEIGHTOVER90);

            var roClient = ClientSingleton.GetClient();
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