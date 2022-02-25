﻿using System;
using System.Threading;
using System.Windows.Input;
using System.Windows.Forms;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class AutoBuff : Action
    {
        private string ACTION_NAME = "STATUS_EFFECTS";
        private Thread statusEffectsThread;
        private int autoBuffDelay { get; set; } = 1;
        private int maxBuffListIndexSize { get; set; } = 40;
        public Key effectStatusKey { get; set; }

        public AutoBuff()
        {

        }

        public void Start()
        {
            Console.WriteLine("StatusEffects: Start");
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                Thread statusEffectsThread = new Thread(() =>
                {
                    while (true)
                    {
                        for (int i = 0; i <= this.maxBuffListIndexSize; i++)
                        {
                            uint currentStatus = roClient.CurrentBuffStatusCode(i);
                            if (this.effectStatusKey != Key.None && Enum.IsDefined(typeof(EffectStatusIDs), currentStatus))
                            {
                                this.useStatusRecovery();
                            }
                        }
                        Thread.Sleep(this.autoBuffDelay);
                    }
                });

                this.statusEffectsThread = statusEffectsThread;
                statusEffectsThread.SetApartmentState(ApartmentState.STA);
                statusEffectsThread.Start();
            }
        }

        public void Stop()
        {
            if (this.statusEffectsThread != null && this.statusEffectsThread.IsAlive)
            {
                this.statusEffectsThread.Abort();
            }
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return ACTION_NAME;
        }

        private void useStatusRecovery()
        {
            Interop.PostMessage(ClientSingleton.GetClient().process.MainWindowHandle, Constants.WM_KEYDOWN_MSG_ID, (Keys)Enum.Parse(typeof(Keys), this.effectStatusKey.ToString()), 0);
        }
    }
}