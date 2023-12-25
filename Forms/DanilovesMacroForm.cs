namespace _4RTools.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Windows.Input;
    using _4RTools.Model;
    using _4RTools.Utils;
    using KeyEventHandler=System.Windows.Forms.KeyEventHandler;

    public partial class DanilovesMacroForm : Form, IObserver
    {
        private readonly Subject _subject;
        
        public const int TOTAL_MACRO_LANES = 1;
        
        private Keys lastKey;
        private bool _isHotkeysRunning;
        
        public DanilovesMacroForm(Subject subject)
        {
            _subject = subject;
            subject.Attach(this);
            InitializeComponent();

            KeyboardHook.Enable();
            this.txtStatusToggleKey.Text = ProfileSingleton.GetCurrent().UserPreferences.toggleDanilovesKey;
            this.txtStatusToggleKey.KeyDown += new KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusToggleKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusToggleKey.TextChanged += new EventHandler(this.onStatusToggleKeyChange);
            
            ConfigureMacroLanes();
        }
        
        private void onStatusToggleKeyChange(object sender, EventArgs e)
        {
            //Get last key from profile before update it in json
            Keys currentToggleKey = (Keys)Enum.Parse(typeof(Keys), this.txtStatusToggleKey.Text);
            KeyboardHook.Remove(lastKey);
            KeyboardHook.Add(currentToggleKey, new KeyboardHook.KeyPressed(this.ToggleStatus));
            ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey = currentToggleKey.ToString(); //Update profile key
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);

            lastKey = currentToggleKey; //Refresh lastKey to update 
        }
        
        private bool ToggleStatus()
        {
            bool isOn = this.btnStatusToggle.Text == "ON";
            if (!_isHotkeysRunning)
            {
                ProfileSingleton.GetCurrent().DanilovesSwitch.Stop();
                return true;
            }
            if (isOn)
            {
                this.btnStatusToggle.BackColor = Color.Red;
                this.btnStatusToggle.Text = "OFF";
                ProfileSingleton.GetCurrent().DanilovesSwitch.Stop();
            }
            else
            {
                Client client = ClientSingleton.GetClient();
                if (client != null)
                {
                    this.btnStatusToggle.BackColor = Color.Green;
                    this.btnStatusToggle.Text = "ON";
                    ProfileSingleton.GetCurrent().DanilovesSwitch.Start();
                }
            }

            return true;
        }

        public void Update(ISubject subject)
        {
            switch ((subject as Subject).Message.code)
            {
                case MessageCode.PROFILE_CHANGED:
                    UpdatePanelData(1);
                    UpdateHotkey();
                    break;
                case MessageCode.TURN_ON:
                    _isHotkeysRunning = true;
                    break;
                case MessageCode.TURN_OFF:
                    _isHotkeysRunning = false;
                    break;
            }
        }
        
        private void UpdateHotkey()
        {
            Keys currentToggleKey = (Keys)Enum.Parse(typeof(Keys), ProfileSingleton.GetCurrent().UserPreferences.toggleDanilovesKey);
            KeyboardHook.Remove(lastKey); //Remove last key hook to prevent toggle with last profile key used.

            this.txtStatusToggleKey.Text = currentToggleKey.ToString();
            KeyboardHook.Add(currentToggleKey, new KeyboardHook.KeyPressed(this.ToggleStatus));
            lastKey = currentToggleKey;
        }

        private void UpdatePanelData(int id)
        {
            try
            {
                GroupBox group = (GroupBox)this.Controls.Find("chainGroup" + id, true)[0];
                ChainConfig chainConfig = new ChainConfig(ProfileSingleton.GetCurrent().DanilovesSwitch.chainConfigs[id - 1]);
                FormUtils.ResetForm(group);

                List<string> names = new List<string>(chainConfig.macroEntries.Keys);
                foreach (string cbName in names)
                {
                    Control[] controls = group.Controls.Find(cbName, true); // Keys
                    if (controls.Length > 0)
                    {
                        TextBox textBox = (TextBox)controls[0];
                        textBox.Text = chainConfig.macroEntries[cbName].key.ToString();
                    }

                    Control[] d = group.Controls.Find($"{cbName}delay", true); // Delays
                    if (d.Length > 0)
                    {
                        NumericUpDown delayInput = (NumericUpDown)d[0];
                        delayInput.Value = chainConfig.macroEntries[cbName].delay;
                    }
                }
            }
            catch { };
        }
        
        private void ConfigureMacroLanes()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES; i++)
            {
                InitializeLane(i);
            }
        }

        private void InitializeLane(int id)
        {
            try
            {
                GroupBox p = (GroupBox)this.Controls.Find("chainGroup" + id, true)[0];
                foreach (Control control in p.Controls)
                {
                    if (control is TextBox)
                    {
                        TextBox textBox = (TextBox)control;
                        textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                        textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                        textBox.TextChanged += new EventHandler(this.OnTextChange);
                    }

                    if (control is NumericUpDown)
                    {
                        NumericUpDown delayInput = (NumericUpDown)control;
                        delayInput.ValueChanged += new System.EventHandler(onDelayChange);
                    }
                }
            }
            catch { }
        }
        
        private void OnTextChange(object sender, EventArgs e)
        {                 
            TextBox textBox = (TextBox)sender;
            int chainID = Int16.Parse(textBox.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            GroupBox group = (GroupBox)this.Controls.Find("chainGroup" + chainID, true)[0];
            ChainConfig chainConfig = ProfileSingleton.GetCurrent().DanilovesSwitch.chainConfigs.Find(config => config.id == chainID);

            Key key = (Key)Enum.Parse(typeof(Key), textBox.Text.ToString());
            NumericUpDown delayInput = (NumericUpDown)group.Controls.Find($"{textBox.Name}delay", true)[0];
            chainConfig.macroEntries[textBox.Name] = new MacroKey(key, decimal.ToInt16(delayInput.Value));

            bool isFirstInput = Regex.IsMatch(textBox.Name, $"in1mac{chainID}");
            if (isFirstInput) { chainConfig.trigger = key; }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().DanilovesSwitch);
        }

        private static void onDelayChange(object sender, EventArgs e)
        {
            NumericUpDown delayInput = (NumericUpDown)sender;
            int chainID = Int16.Parse(delayInput.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = ProfileSingleton.GetCurrent().DanilovesSwitch.chainConfigs.Find(config => config.id == chainID);

            String cbName = delayInput.Name.Split(new[] { "delay" }, StringSplitOptions.None)[0];
            chainConfig.macroEntries[cbName].delay = decimal.ToInt16(delayInput.Value);

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().DanilovesSwitch);
        }
    }
}