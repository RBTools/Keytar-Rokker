using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeytarRokker.x360;
using SlimDX.XInput;
using Toub.Sound.Midi;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;
using Controller = SlimDX.XInput.Controller;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;

namespace KeytarRokker
{
    public partial class frmMain : Form
    {
        private readonly NemoTools Tools;
        private readonly DTAParser Parser;
        private readonly MIDIStuff MIDITools;
        private Controller Keytar;
        private const string AppName = "Keytar Rokker";
        private static readonly Color mMenuHighlight = Color.FromArgb(135, 0, 0);
        private static readonly Color mMenuBackground = Color.Black;
        private static readonly Color mMenuText = Color.Gray;
        private static readonly Color mMenuBorder = Color.WhiteSmoke;
        private Color LCDColor1 = Color.FromArgb(157, 164, 148);
        private Color LCDColor2 = Color.WhiteSmoke;
        private Color LCDColor3 = Color.LightGoldenrodYellow;
        private Color LCDColor4 = Color.PaleGreen;
        private Color LCDColor5 = Color.LightBlue;
        private Color LCDColor6 = Color.LightPink;
        private Color LCDColor7 = Color.Thistle;
        private Color LCDColor8 = Color.RosyBrown;
        private Color RangeMarkerColor = Color.Gray;
        private Color PressedKeyRed = Color.FromArgb(127, 199, 52, 45);
        private Color PressedKeyYellow = Color.FromArgb(127, 238, 234, 77);
        private Color PressedKeyBlue = Color.FromArgb(127, 58, 133, 207);
        private Color PressedKeyGreen = Color.FromArgb(127, 78, 179, 114);
        private Color PressedKeyOrange = Color.FromArgb(127, 221, 103, 48);
        private int LCDColorIndex;
        private string SongFile;
        private string SongArtist;
        private string SongTitle;
        private string SongLength;
        private double SongLengthDouble;
        private double SongBPM = 120.0;
        private double PlaybackSeconds;
        private int BassMixer;
        private int BassStream;
        private const int BassBuffer = 100;
        public double TrackVolume = 0.5;
        private readonly string ConfigFile;
        private readonly string TempMIDI;
        private double PlaybackWindow = 1.0;
        private bool showUpdateMessage;
        private Bitmap RESOURCE_BACKGROUND;
        private Bitmap RESOURCE_TRACK;
        private Bitmap RESOURCE_TRACK_SOLO;
        private Bitmap RESOURCE_HITBOX;
        private Bitmap RESOURCE_NOTE_WHITE;
        private Bitmap RESOURCE_NOTE_BLACK;
        private Bitmap RESOURCE_NOTE_WHITE_OD;
        private Bitmap RESOURCE_NOTE_BLACK_OD;
        private Bitmap RESOURCE_KEYTAR;
        private Bitmap RESOURCE_LCD;
        private Bitmap RESOURCE_SCROLL;
        private Bitmap RESOURCE_SCROLL_ACTIVE;
        private Bitmap RESOURCE_SCROLL_ACTIVE2;
        private Bitmap RESOURCE_BUTTON_PLAY;
        private Bitmap RESOURCE_BUTTON_PAUSE;
        private Bitmap RESOURCE_BUTTON_LOAD;
        private Bitmap RESOURCE_BUTTON_LIGHT;
        private Bitmap RESOURCE_BUTTON_MUTE;
        private Bitmap RESOURCE_BUTTON_DOWN;
        private Bitmap RESOURCE_BUTTON_UP;
        private Bitmap RESOURCE_POWER_ON;
        private Bitmap RESOURCE_POWER_OFF;
        private Bitmap RESOURCE_VOLUME;
        private Bitmap RESOURCE_PEDAL_ON;
        private Bitmap RESOURCE_PEDAL_OFF;
        private bool STATE_LEFT;
        private bool STATE_RIGHT;
        private bool STATE_UP;
        private bool STATE_DOWN;
        private bool STATE_BACK;
        private bool STATE_START;
        private bool STATE_BUTTON_A;
        private bool STATE_BUTTON_B;
        private bool STATE_BUTTON_X;
        private bool STATE_BUTTON_Y;
        private bool STATE_PEDAL;
        private readonly List<bool> STATE_INPUT;
        private readonly List<int> STREAMS;
        private readonly List<byte> LastKeysState;
        private readonly List<byte> CurrentKeysState;
        private readonly List<int> NotePosX = new List<int> { 16, 46, 74, 104, 137, 179, 218, 246, 276, 304, 334, 367, 408, 444, 472, 502, 540, 583, 618, 646, 676, 704, 734, 770, 819 };
        private const string NothingLoaded = "NO SONG LOADED...";
        private const int NORMAL_HEIGHT = 480;
        private const int EXTENDED_HEIGHT = 880;
        private bool forcePedal;
        private bool forceMuteTrack;
        private int BaseOctave = 2;
        private int CurrentAudioType;
        private bool MouseIsPressed;
        private readonly List<PictureBox> KeysTops;
        private readonly List<PictureBox> KeysBottoms;
        private List<Color> PressedKeysColors;
        private bool isLoading;
        private bool MouseIsOnScroll;
        private int LastScrollY;
        private MIDITrack ActiveChart;
        private QuickAccessButton QuickAccessX = new QuickAccessButton { AudioType = 0, BaseOctave = 2};
        private QuickAccessButton QuickAccessY = new QuickAccessButton { AudioType = 1, BaseOctave = 2 };
        private QuickAccessButton QuickAccessA = new QuickAccessButton { AudioType = 3, BaseOctave = 3 };
        private QuickAccessButton QuickAccessB = new QuickAccessButton { AudioType = 6, BaseOctave = 4 };
        private readonly List<Label> KeyboardShortcutLabels;
        private readonly List<Keys> KeyboardShortcuts;
        private int ActiveKey;
        private string DefaultDebuggingText;
            
        [DllImport("xinput1_3.dll", EntryPoint = "#103")]
        private static extern void TurnOffController(int controller);

        public frmMain()
        {
            InitializeComponent();
            isLoading = true;
            menuStrip1.Renderer = new DarkRenderer();
            Tools = new NemoTools();
            Parser = new DTAParser();
            MIDITools = new MIDIStuff();
            MidiPlayer.OpenMidi();
            ConfigFile = Application.StartupPath + "\\bin\\keytar.config";
            if (!Directory.Exists(Application.StartupPath + "\\bin\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\bin\\");
            }
            if (!Directory.Exists(Application.StartupPath + "\\samples\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\samples\\");
            }
            TempMIDI = Application.StartupPath + "\\bin\\temp.mid";
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, Handle))
            {
                MessageBox.Show("Error initializing BASS.NET:\n" + Bass.BASS_ErrorGetCode() + "\nWon't be able to play any audio!", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, BassBuffer);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 5);
            STATE_INPUT = new List<bool>();
            CurrentKeysState = new List<byte>();
            LastKeysState = new List<byte>();
            STREAMS = new List<int>();
            for (var i = 0; i < 25; i++)
            {
                STATE_INPUT.Add(false);
                CurrentKeysState.Add(0);
                LastKeysState.Add(0);
                STREAMS.Add(0);
            }
            KeyboardShortcutLabels = new List<Label>
            {
                lblC2, lblD2b, lblD2, lblE2b, lblE2, lblF2, lblG2b, lblG2, 
                lblA2b, lblA2, lblB2b, lblB2, lblC3, lblD3b, lblD3, lblE3b, 
                lblE3, lblF3, lblG3b, lblG3, lblA3b, lblA3, lblB3b, lblB3, lblC4
            };
            KeyboardShortcuts = new List<Keys>
            {
                Keys.A, Keys.W, Keys.S, Keys.E, Keys.D, Keys.F, Keys.T, Keys.G,
                Keys.Y, Keys.H, Keys.U, Keys.J, Keys.K, Keys.O, Keys.L, Keys.P,
                Keys.OemSemicolon, Keys.OemQuotes, Keys.OemOpenBrackets,
                Keys.NumPad1, Keys.NumPad4, Keys.NumPad2, Keys.NumPad5, Keys.NumPad3, Keys.Add
            };
            //grab all the keys stuff now to make it faster later
            KeysTops = new List<PictureBox>
            {
                picC3t,picFlatD3,picD3t,picFlatE3,picE3t,//red
                picF3t,picFlatG3,picG3t,picFlatA3,picA3t,picFlatB3,picB3t,//yellow
                picC4t,picFlatD4,picD4t,picFlatE4,picE4t,//blue
                picF4t,picFlatG4,picG4t,picFlatA4,picA4t,picFlatB4,picB4t,//green
                picC5//orange
            };
            KeysBottoms = new List<PictureBox>
            {
                picC3b,null,picD3b,null,picE3b,//red
                picF3b,null,picG3b,null,picA3b,null,picB3b,//yellow
                picC4b,null,picD4b,null, picE4b,//blue
                picF4b,null,picG4b,null,picA4b,null,picB4b,//green
                null//orange
            };
            LoadImages();
            CheckLCDFontIsAvailable();
            SelectController(UserIndex.One);
            MouseWheel += frmMain_MouseWheel;
        }

        private void frmMain_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta < 0 && PlaybackSeconds > 1.0)
            {
                PlaybackSeconds -= 1.0;
            }
            else if (e.Delta > 0 && PlaybackSeconds < SongLengthDouble - 1.0)
            {
                PlaybackSeconds += 1.0;
            }
            UpdateTime();
            if (Bass.BASS_ChannelIsActive(BassMixer) != BASSActive.BASS_ACTIVE_PAUSED &&
                Bass.BASS_ChannelIsActive(BassMixer) != BASSActive.BASS_ACTIVE_PLAYING)
            {
                return;
            }
            BassMix.BASS_Mixer_ChannelSetPosition(BassStream, Bass.BASS_ChannelSeconds2Bytes(BassStream, PlaybackSeconds));
            ResetPlayedNotes();
        }
        
        private void UpdateScrollSpeed()
        {
            if (PlaybackWindow < 0.5)
            {
                PlaybackWindow = 0.5;
            }
            else if (PlaybackWindow > 5.0)
            {
                PlaybackWindow = 5.0;
            }
            lblScroll.Text = PlaybackWindow.ToString("0.0");
        }

        private void SaveConfig()
        {
            var sw = new StreamWriter(ConfigFile, false);
            sw.WriteLine("//Created by " + AppName + " " + GetAppVersion());
            sw.WriteLine("KeyColorRed=#" + GetColorHex(PressedKeyRed));
            sw.WriteLine("KeyColorYellow=#" + GetColorHex(PressedKeyYellow));
            sw.WriteLine("KeyColorBlue=#" + GetColorHex(PressedKeyBlue));
            sw.WriteLine("KeyColorGreen=#" + GetColorHex(PressedKeyGreen));
            sw.WriteLine("KeyColorOrange=#" + GetColorHex(PressedKeyOrange));
            sw.WriteLine("PlaybackWindow=" + PlaybackWindow);
            sw.WriteLine("SoundType=" + CurrentAudioType);
            sw.WriteLine("BaseOctave=" + BaseOctave);
            sw.WriteLine("TrackVolume=" + TrackVolume);
            sw.WriteLine("ForceMute=" + forceMuteTrack);
            sw.WriteLine("ForcePedal=" + forcePedal);
            sw.WriteLine("LCDColorIndex=" + LCDColorIndex);
            sw.WriteLine("LCDColor1=#" + GetColorHex(LCDColor1));
            sw.WriteLine("LCDColor2=#" + GetColorHex(LCDColor2));
            sw.WriteLine("LCDColor3=#" + GetColorHex(LCDColor3));
            sw.WriteLine("LCDColor4=#" + GetColorHex(LCDColor4));
            sw.WriteLine("LCDColor5=#" + GetColorHex(LCDColor5));
            sw.WriteLine("LCDColor6=#" + GetColorHex(LCDColor6));
            sw.WriteLine("LCDColor7=#" + GetColorHex(LCDColor7));
            sw.WriteLine("LCDColor8=#" + GetColorHex(LCDColor8));
            sw.WriteLine("PlayAlongMode=" + playAlongMode.Checked);
            sw.WriteLine("ShowRangeMarker=" + showRangeMarker.Checked);
            sw.WriteLine("RangeMarkerColor=#" + GetColorHex(RangeMarkerColor));
            sw.WriteLine("SilenceKeysTrack=" + silenceKeysTrack.Checked);
            sw.WriteLine("AutoPlayWithChart=" + autoPlayWithChart.Checked);
            sw.WriteLine("ShowScrollControl=" + showScrollSpeedControl.Checked);
            sw.WriteLine("ShowChartSelection=" + showScrollSpeedControl.Checked);
            sw.WriteLine("QuickAccessA=" + QuickAccessA.AudioType + "," + QuickAccessA.BaseOctave);
            sw.WriteLine("QuickAccessB=" + QuickAccessB.AudioType + "," + QuickAccessB.BaseOctave);
            sw.WriteLine("QuickAccessX=" + QuickAccessX.AudioType + "," + QuickAccessX.BaseOctave);
            sw.WriteLine("QuickAccessY=" + QuickAccessY.AudioType + "," + QuickAccessY.BaseOctave);
            for (var i = 0; i < KeyboardShortcuts.Count; i++)
            {
                sw.WriteLine("KeyboardShortcut" + KeyboardShortcutLabels[i].Name.Replace("lbl","") + "=" + KeyboardShortcuts[i]);
            }
            sw.WriteLine("CustomHeight=" + Height);
            sw.WriteLine("ShowKeyboardShortcuts=" + showKeyboardShortcuts.Checked);
            sw.Dispose();
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                var sr = new StreamReader(ConfigFile);
                try
                {
                    sr.ReadLine();
                    PressedKeyRed = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    PressedKeyYellow = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    PressedKeyBlue = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    PressedKeyGreen = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    PressedKeyOrange = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    PlaybackWindow = Convert.ToDouble(Tools.GetConfigString(sr.ReadLine()));
                    CurrentAudioType = Convert.ToInt16(Tools.GetConfigString(sr.ReadLine()));
                    BaseOctave = Convert.ToInt16(Tools.GetConfigString(sr.ReadLine()));
                    TrackVolume = Convert.ToDouble(Tools.GetConfigString(sr.ReadLine()));
                    forceMuteTrack = sr.ReadLine().Contains("True");
                    forcePedal = sr.ReadLine().Contains("True");
                    LCDColorIndex = Convert.ToInt16(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor1 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor2 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor3 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor4 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor5 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor6 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor7 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    LCDColor8 = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    playAlongMode.Checked = sr.ReadLine().Contains("True");
                    showRangeMarker.Checked = sr.ReadLine().Contains("True");
                    RangeMarkerColor = ColorTranslator.FromHtml(Tools.GetConfigString(sr.ReadLine()));
                    silenceKeysTrack.Checked = sr.ReadLine().Contains("True");
                    autoPlayWithChart.Checked = sr.ReadLine().Contains("True");
                    showScrollSpeedControl.Checked = sr.ReadLine().Contains("True");
                    showChartSelection.Checked = sr.ReadLine().Contains("True");
                    var A = Tools.GetConfigString(sr.ReadLine()).Split(',');
                    QuickAccessA = new QuickAccessButton {AudioType = Convert.ToInt16(A[0]), BaseOctave = Convert.ToInt16(A[1])};
                    var B = Tools.GetConfigString(sr.ReadLine()).Split(',');
                    QuickAccessB = new QuickAccessButton { AudioType = Convert.ToInt16(B[0]), BaseOctave = Convert.ToInt16(B[1]) };
                    var X = Tools.GetConfigString(sr.ReadLine()).Split(',');
                    QuickAccessX = new QuickAccessButton { AudioType = Convert.ToInt16(X[0]), BaseOctave = Convert.ToInt16(X[1]) };
                    var Y = Tools.GetConfigString(sr.ReadLine()).Split(',');
                    QuickAccessY = new QuickAccessButton { AudioType = Convert.ToInt16(Y[0]), BaseOctave = Convert.ToInt16(Y[1]) };
                    for (var i = 0; i < KeyboardShortcuts.Count; i++)
                    {
                        KeyboardShortcuts[i] = (Keys)Enum.Parse(typeof(Keys), Tools.GetConfigString(sr.ReadLine()));
                    }
                    Height = Convert.ToInt16(Tools.GetConfigString(sr.ReadLine()));
                    showKeyboardShortcuts.Checked = sr.ReadLine().Contains("True");
                }
                catch (Exception)
                {}
                sr.Dispose();
            }
            UpdateShortcutlabelsVisibility(showKeyboardShortcuts.Checked);
            CenterToScreen();
            UpdatePedal();
            UpdateLCDBacklight();
            UpdateScrollSpeed();
            UpdateAudioType(true);
            UpdateOctave();
            UpdateTrackVolume();
        }

        private void UpdateOctave()
        {
            lblOctave.Text = "C" + (BaseOctave + 1);
            if (CurrentAudioType == 0)
            {
                LoadPianoSamples();
            }
        }

        private static string GetColorHex(Color color)
        {
            return color.A.ToString("X2") + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private void LoadImages()
        {
            var res = Application.StartupPath + "\\res\\";
            if (!Directory.Exists(res))
            {
                MessageBox.Show("Missing \\res\\ folder, no images will be loaded", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                RESOURCE_BACKGROUND = (Bitmap)Tools.NemoLoadImage(res + "background.jpg");
                RESOURCE_TRACK = (Bitmap)Tools.NemoLoadImage(res + "track.jpg");
                RESOURCE_TRACK_SOLO = (Bitmap)Tools.NemoLoadImage(res + "solo.png");
                RESOURCE_HITBOX = (Bitmap)Tools.NemoLoadImage(res + "hitbox.png");
                RESOURCE_NOTE_WHITE = (Bitmap)Tools.NemoLoadImage(res + "note_white.png");
                RESOURCE_NOTE_BLACK = (Bitmap)Tools.NemoLoadImage(res + "note_black.png");
                RESOURCE_NOTE_WHITE_OD = (Bitmap)Tools.NemoLoadImage(res + "note_white_od.png");
                RESOURCE_NOTE_BLACK_OD = (Bitmap)Tools.NemoLoadImage(res + "note_black_od.png");
                RESOURCE_KEYTAR = (Bitmap)Tools.NemoLoadImage(res + "keytar.png");
                RESOURCE_LCD = (Bitmap)Tools.NemoLoadImage(res + "lcd.png");
                RESOURCE_SCROLL = (Bitmap)Tools.NemoLoadImage(res + "scroll.png");
                RESOURCE_SCROLL_ACTIVE = (Bitmap)Tools.NemoLoadImage(res + "scroll_act1.png");
                RESOURCE_SCROLL_ACTIVE2 = (Bitmap)Tools.NemoLoadImage(res + "scroll_act2.png");
                RESOURCE_BUTTON_PLAY = (Bitmap)Tools.NemoLoadImage(res + "button_play.png");
                RESOURCE_BUTTON_PAUSE = (Bitmap)Tools.NemoLoadImage(res + "button_pause.png");
                RESOURCE_BUTTON_LOAD = (Bitmap)Tools.NemoLoadImage(res + "button_load.png");
                RESOURCE_BUTTON_MUTE = (Bitmap)Tools.NemoLoadImage(res + "button_mute.png");
                RESOURCE_BUTTON_LIGHT = (Bitmap)Tools.NemoLoadImage(res + "button_light.png");
                RESOURCE_BUTTON_UP = (Bitmap)Tools.NemoLoadImage(res + "button_up.png");
                RESOURCE_BUTTON_DOWN = (Bitmap)Tools.NemoLoadImage(res + "button_down.png");
                RESOURCE_POWER_ON = (Bitmap)Tools.NemoLoadImage(res + "power_on.png");
                RESOURCE_POWER_OFF = (Bitmap)Tools.NemoLoadImage(res + "power_off.png");
                RESOURCE_VOLUME = (Bitmap)Tools.NemoLoadImage(res + "volume.png");
                RESOURCE_PEDAL_ON = (Bitmap)Tools.NemoLoadImage(res + "pedal_on.png");
                RESOURCE_PEDAL_OFF = (Bitmap)Tools.NemoLoadImage(res + "pedal_off.png");
            }
            catch (Exception)
            {
                MessageBox.Show("Some of the resource images were missing so I couldn't load them\n" + AppName + " includes all the necessary images when you download it, " +
                            "if you're going to modify it, you need to make sure your modified files have the exact same name as the original files", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            BackgroundImage = RESOURCE_BACKGROUND;
            picTrack.BackgroundImage = RESOURCE_TRACK;
            panelKeytar.BackgroundImage = RESOURCE_KEYTAR;
            panelLCD.BackgroundImage = RESOURCE_LCD;
            panelAudio.BackgroundImage = RESOURCE_LCD;
            panelScroll.BackgroundImage = RESOURCE_LCD;
            picScroll.Image = RESOURCE_SCROLL;
            picLoad.Image = RESOURCE_BUTTON_LOAD;
            picPlayPause.Image = RESOURCE_BUTTON_PLAY;
            picMute.Image = RESOURCE_BUTTON_MUTE;
            picLight.Image = RESOURCE_BUTTON_LIGHT;
            picVolume.Image = RESOURCE_VOLUME;
            picPedal.Image = RESOURCE_PEDAL_OFF;
            picAudioDown.Image = RESOURCE_BUTTON_DOWN;
            picOctaveDown.Image = RESOURCE_BUTTON_DOWN;
            picAudioUp.Image = RESOURCE_BUTTON_UP;
            picOctaveUp.Image = RESOURCE_BUTTON_UP;
        }
        
        private void LoadPianoSamples()
        {
            var SamplePath = Application.StartupPath + "\\samples\\";
            var missing = new List<int>();
            for (var i = 0; i < 25; i++)
            {
                var actualSample = (BaseOctave * 12) + i + 3; //we're always going from C1, first three samples can't be used
                try
                {
                    if (File.Exists(SamplePath + actualSample + ".ogg"))
                    {
                        STREAMS[i] = Bass.BASS_StreamCreateFile(SamplePath + actualSample + ".ogg", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
                    }
                    else if (File.Exists(SamplePath + actualSample + ".wav"))
                    {
                        STREAMS[i] = Bass.BASS_StreamCreateFile(SamplePath + actualSample + ".wav", 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
                    }
                    else
                    {
                        missing.Add(actualSample);
                        STREAMS[i] = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading sample: '" + actualSample + "':\n" + ex.Message + "\nSample will NOT play.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if (!missing.Any()) return;
            var samples = missing.Aggregate("", (current, t) => current + ("'" + t + "'\n"));
            MessageBox.Show("The following " + (missing.Count > 1 ? "samples are" : "sample is") + " missing:\n" + samples + "You can use .ogg and .wav files only\nMissing " + (missing.Count > 1 ? "samples" : "sample") + " will NOT play.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void minimizeToTray_Click(object sender, EventArgs e)
        {
            NotifyTray_MouseDoubleClick(sender, null);
        }

        private void NotifyTray_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }

        private void player1_Click(object sender, EventArgs e)
        {
            player1.Checked = true;
            player2.Checked = false;
            player3.Checked = false;
            player4.Checked = false;
            SelectController(UserIndex.One);
        }

        private void player2_Click(object sender, EventArgs e)
        {
            player1.Checked = false;
            player2.Checked = true;
            player3.Checked = false;
            player4.Checked = false;
            SelectController(UserIndex.Two);
        }

        private void player3_Click(object sender, EventArgs e)
        {
            player1.Checked = false;
            player2.Checked = false;
            player3.Checked = true;
            player4.Checked = false;
            SelectController(UserIndex.Three);
        }

        private void player4_Click(object sender, EventArgs e)
        {
            player1.Checked = false;
            player2.Checked = false;
            player3.Checked = false;
            player4.Checked = true;
            SelectController(UserIndex.Four);
        }

        private void SelectController(UserIndex index)
        {
            try
            {
                Keytar = new Controller(index);
                ConnectionTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                ConnectionTimer.Enabled = false;
                MessageBox.Show("Error creating drums controller:\n" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConnectionTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                player1.Enabled = new Controller(UserIndex.One).IsConnected;
                player2.Enabled = new Controller(UserIndex.Two).IsConnected;
                player3.Enabled = new Controller(UserIndex.Three).IsConnected;
                player4.Enabled = new Controller(UserIndex.Four).IsConnected;
                turnOffActive.Enabled = player1.Enabled | player2.Enabled | player3.Enabled | player4.Enabled;
                turnOffAll.Enabled = turnOffActive.Enabled;
                debugInput.Enabled = turnOffActive.Enabled;
                customizeQuickAccess.Enabled = turnOffActive.Enabled;
            }
            catch (Exception)
            { }
            try
            {
                if (Keytar.IsConnected)
                {
                    picPower.Image = RESOURCE_POWER_ON;
                    toolTip1.SetToolTip(picPower, "Connected");
                    KeysTimer.Enabled = true;
                    lblDebug.Visible = debugInput.Checked;
                    if (Keytar.IsConnected && string.IsNullOrEmpty(DefaultDebuggingText))
                    {
                        DefaultDebuggingText = GetDebugData(Keytar.GetState().Gamepad);
                    }
                    return;
                }
                customizeQuickAccess.Checked = false;
            }
            catch (Exception)
            {}
            picPower.Image = RESOURCE_POWER_OFF;
            toolTip1.SetToolTip(picPower, "Disconnected");
            KeysTimer.Enabled = false;
            debugInput.Checked = false;
            lblDebug.Visible = false;
        }
        
        private void turnOffActive_Click(object sender, EventArgs e)
        {
            try
            {
                if (player1.Checked)
                {
                    TurnOffController(0);
                }
                else if (player2.Checked)
                {
                    TurnOffController(1);
                }
                else if (player3.Checked)
                {
                    TurnOffController(2);
                }
                else if (player4.Checked)
                {
                    TurnOffController(3);
                }
            }
            catch (Exception)
            { }
        }

        private void turnOffAll_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < 4; i++)
            {
                try
                {
                    TurnOffController(i);
                }
                catch (Exception)
                { }
            }
        }

        private sealed class DarkRenderer : ToolStripProfessionalRenderer
        {
            public DarkRenderer() : base(new DarkColors()) { }
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected
            {
                get { return mMenuHighlight; }
            }
            public override Color MenuItemSelectedGradientBegin
            {
                get { return mMenuHighlight; }
            }
            public override Color MenuItemSelectedGradientEnd
            {
                get { return mMenuHighlight; }
            }
            public override Color MenuBorder
            {
                get { return mMenuBorder; }
            }
            public override Color MenuItemBorder
            {
                get { return mMenuBorder; }
            }
            public override Color MenuItemPressedGradientBegin
            {
                get { return mMenuHighlight; }
            }
            public override Color MenuItemPressedGradientEnd
            {
                get { return mMenuHighlight; }
            }
            public override Color MenuItemPressedGradientMiddle
            {
                get { return mMenuHighlight; }
            }
            public override Color CheckBackground
            {
                get { return mMenuHighlight; }
            }
            public override Color CheckPressedBackground
            {
                get { return mMenuHighlight; }
            }
            public override Color CheckSelectedBackground
            {
                get { return mMenuHighlight; }
            }
            public override Color ButtonSelectedBorder
            {
                get { return mMenuHighlight; }
            }
            public override Color SeparatorDark
            {
                get { return mMenuText; }
            }
            public override Color SeparatorLight
            {
                get { return mMenuText; }
            }
            public override Color ImageMarginGradientBegin
            {
                get { return mMenuBackground; }
            }
            public override Color ImageMarginGradientEnd
            {
                get { return mMenuBackground; }
            }
            public override Color ImageMarginGradientMiddle
            {
                get { return mMenuBackground; }
            }
            public override Color ToolStripDropDownBackground
            {
                get { return mMenuBackground; }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var version = GetAppVersion();
            var message = AppName + "\nVersion: " + version + "\n© TrojanNemo, 2015\nDedicated to the C3 community\nCreated for the hell of it, don't expect too much from it!\n\n";
            var credits = Tools.ReadHelpFile("credits");
            MessageBox.Show(message + credits, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string GetAppVersion()
        {
            var vers = Assembly.GetExecutingAssembly().GetName().Version;
            return "v" + String.Format("{0}.{1}.{2}", vers.Major, vers.Minor, vers.Build);
        }

        private void c3ForumsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://customscreators.com/index.php?/topic/13240-keytar-rokker-v120-9615-play-your-rock-band-3-keytar-on-pc/");
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showUpdateMessage = true;
            updater.RunWorkerAsync();
        }

        private void updater_DoWork(object sender, DoWorkEventArgs e)
        {
            var path = Application.StartupPath + "\\bin\\update.txt";
            Tools.DeleteFile(path);
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile("http://www.keepitfishy.com/rb3/keytarrokker/update.txt", path);
                }
                catch (Exception)
                { }
            }
        }

        private void updater_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var path = Application.StartupPath + "\\bin\\update.txt";
            if (!File.Exists(path))
            {
                if (showUpdateMessage)
                {
                    MessageBox.Show("Unable to check for updates", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                return;
            }
            var thisVersion = GetAppVersion();
            var newVersion = "v";
            string newName;
            string releaseDate;
            string link;
            var changeLog = new List<string>();
            var sr = new StreamReader(path);
            try
            {
                var line = sr.ReadLine();
                if (line.ToLowerInvariant().Contains("html"))
                {
                    sr.Dispose();
                    if (showUpdateMessage)
                    {
                        MessageBox.Show("Unable to check for updates", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    return;
                }
                newName = Tools.GetConfigString(line);
                newVersion += Tools.GetConfigString(sr.ReadLine());
                releaseDate = Tools.GetConfigString(sr.ReadLine());
                link = Tools.GetConfigString(sr.ReadLine());
                sr.ReadLine();//ignore Change Log header
                while (sr.Peek() >= 0)
                {
                    changeLog.Add(sr.ReadLine());
                }
            }
            catch (Exception ex)
            {
                if (showUpdateMessage)
                {
                    MessageBox.Show("Error parsing update file:\n" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                sr.Dispose();
                return;
            }
            sr.Dispose();
            Tools.DeleteFile(path);
            if (thisVersion.Equals(newVersion))
            {
                if (showUpdateMessage)
                {
                    MessageBox.Show("You have the latest version", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            var newInt = Convert.ToInt16(newVersion.Replace("v", "").Replace(".", "").Trim());
            var thisInt = Convert.ToInt16(thisVersion.Replace("v", "").Replace(".", "").Trim());
            if (newInt <= thisInt)
            {
                if (showUpdateMessage)
                {
                    MessageBox.Show("You have a newer version (" + thisVersion + ") than what's on the server (" + newVersion + ")\nNo update needed!", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            var updaterForm = new Updater();
            updaterForm.SetInfo(AppName, thisVersion, newName, newVersion, releaseDate, link, changeLog);
            updaterForm.ShowDialog();
        }

        private void viewChangeLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string changelog = "keytarrokker_changelog.txt";
            if (!File.Exists(Application.StartupPath + "\\" + changelog))
            {
                MessageBox.Show("Changelog file is missing!", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process.Start(Application.StartupPath + "\\" + changelog);
        }
        
        public void UpdateTrackVolume()
        {
            if (TrackVolume < 0.00)
            {
                TrackVolume = 0.00;
            }
            else if (TrackVolume > 1.00)
            {
                TrackVolume = 1.00;
            }
            lblVolume.Invoke(new MethodInvoker(() => lblVolume.Text = forceMuteTrack ? "X" : ((int)(100 * TrackVolume)).ToString(CultureInfo.InvariantCulture)));
            if (Bass.BASS_ChannelIsActive(BassMixer) != BASSActive.BASS_ACTIVE_PAUSED &&
                Bass.BASS_ChannelIsActive(BassMixer) != BASSActive.BASS_ACTIVE_PLAYING)
            {
                return;
            }
            Bass.BASS_ChannelSetAttribute(BassMixer, BASSAttribute.BASS_ATTRIB_VOL, forceMuteTrack ? 0 : (float)TrackVolume);
        }

        private void PressKey(int key)
        {
            PlaySample(key, 50);
        }

        private void ReleaseKey(int key)
        {
            if (!STATE_PEDAL && !forcePedal)
            {
                StopSample(key);
            }
        }

        private void PlaySample(int key, int velocity)
        {
            const int PADDING = 12; //seems this player is an octave lower than our piano samples, let's compensate
            var MIDI_NOTE = PADDING + key + (BaseOctave*12);
            if (CurrentAudioType > 0)
            {
                MidiPlayer.Play(new NoteOn(0, 1, (byte)MIDI_NOTE, 127));
            }
            else
            {
                Bass.BASS_ChannelSetAttribute(STREAMS[key], BASSAttribute.BASS_ATTRIB_VOL, (float)(velocity / 127.0));
                Bass.BASS_ChannelPlay(STREAMS[key], true);
            }
            STATE_INPUT[key] = true;
            UpdatePressedKeys(key);
        }

        private void StopSample(int key)
        {
            const int PADDING = 12; //seems this player is an octave lower than our piano samples, let's compensate
            var MIDI_NOTE = PADDING + key + (BaseOctave * 12);
            if (CurrentAudioType > 0)
            {
                MidiPlayer.Play(new NoteOff(0, 1, (byte)MIDI_NOTE, 127));
            }
            else
            {
                Bass.BASS_ChannelStop(STREAMS[key]);
            }
            if (key > STATE_INPUT.Count - 1) return;
            STATE_INPUT[key] = false;
            UpdatePressedKeys(key);
        }

        private void UpdatePressedKeys(int key)
        {
            KeysTops[key].BackColor = STATE_INPUT[key] ? PressedKeysColors[key] : Color.Transparent;
            if (KeysBottoms[key] == null) return;
            KeysBottoms[key].BackColor = KeysTops[key].BackColor;
        }

        private void UpdatePedal()
        {
            picPedal.Image = forcePedal || STATE_PEDAL ? RESOURCE_PEDAL_ON : RESOURCE_PEDAL_OFF;
            if (forcePedal || STATE_PEDAL || isLoading) return;
            StopAllSamples();
        }

        private void picFlatB4_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ((PictureBox) sender).Capture = false;
            MouseIsPressed = true;
            ActiveKey = Convert.ToInt16(((PictureBox) sender).Tag);
            if (customizeKeyboardShortcuts.Checked)
            {
                StopAllSamples();
            }
            PressKey(ActiveKey);
        }

        private void picFlatB4_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            MouseIsPressed = false;
            if (STATE_PEDAL || forcePedal || customizeKeyboardShortcuts.Checked) return;
            StopAllSamples();
            ActiveKey = -1;
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            LoadConfig();
            PressedKeysColors = new List<Color>
            {
                PressedKeyRed,PressedKeyRed,PressedKeyRed,PressedKeyRed,PressedKeyRed,
                PressedKeyYellow,PressedKeyYellow,PressedKeyYellow,PressedKeyYellow,PressedKeyYellow,PressedKeyYellow,PressedKeyYellow,
                PressedKeyBlue,PressedKeyBlue,PressedKeyBlue,PressedKeyBlue,PressedKeyBlue,
                PressedKeyGreen,PressedKeyGreen,PressedKeyGreen,PressedKeyGreen,PressedKeyGreen,PressedKeyGreen,PressedKeyGreen,
                PressedKeyOrange
            };
            UpdateShortcutLabelsText();
            isLoading = false;
            updater.RunWorkerAsync();
        }

        private static Byte LoByte(Int16 nValue)
        {
            return (Byte)(nValue & 0xFF);
        }

        private static Byte HiByte(Int16 nValue)
        {
            return (Byte)(nValue >> 8);
        }

        private static string GetDebugData(Gamepad gamepad)
        {
            var Velocity = HiByte(gamepad.LeftThumbX) & 127;
            var debug = "DEBUG = Buttons: " + gamepad.Buttons +
                        "   |   LThumb (x,y): " + gamepad.LeftThumbX + "," + gamepad.LeftThumbY +
                        "   |   RThumb (x,y): " + gamepad.RightThumbX + "," + gamepad.RightThumbY +
                        "   |   LTrigger: " + gamepad.LeftTrigger +
                        "   |   RTrigger: " + gamepad.RightTrigger +
                        "   |   Vel.: " + Velocity +
                        "   |   Hash: " + gamepad.GetHashCode();
            return debug;
        }

        private void KeysTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var gamepad = Keytar.GetState().Gamepad;
                var Buttons = Keytar.GetState().Gamepad.Buttons;
                var Velocity = HiByte(gamepad.LeftThumbX) & 127;
                if (debugInput.Checked)
                {
                    var debug = GetDebugData(Keytar.GetState().Gamepad);
                    if (debug != DefaultDebuggingText)
                    {
                        lblDebug.Text = debug;
                    }
                }
                if (Buttons.ToString() == "None")
                {
                    STATE_START = false;
                    STATE_BACK = false;
                    STATE_RIGHT = false;
                    STATE_LEFT = false;
                    STATE_UP = false;
                    STATE_DOWN = false;
                    STATE_BUTTON_X = false;
                    STATE_BUTTON_Y = false;
                    STATE_BUTTON_A = false;
                    STATE_BUTTON_B = false;
                }
                else if (Buttons == GamepadButtonFlags.DPadLeft && !STATE_LEFT)
                {
                    STATE_LEFT = true;
                    picAudioUp_MouseClick(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
                else if (Buttons == GamepadButtonFlags.DPadRight && !STATE_RIGHT)
                {
                    STATE_RIGHT = true;
                    picAudioDown_MouseClick(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
                else if (Buttons == GamepadButtonFlags.DPadUp && !STATE_UP)
                {
                    STATE_UP = true;
                    picOctaveUp_MouseClick(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
                else if (Buttons == GamepadButtonFlags.DPadDown && !STATE_DOWN)
                {
                    STATE_DOWN = true;
                    picOctaveDown_MouseClick(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
                else if (Buttons == GamepadButtonFlags.Start && !STATE_START)
                {
                    STATE_START = true;
                    picPlayPause_MouseClick(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
                else if (Buttons == GamepadButtonFlags.Back && !STATE_BACK)
                {
                    STATE_BACK = true;
                    picLoad_MouseClick(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
                else if (Buttons == GamepadButtonFlags.X && !STATE_BUTTON_X)
                {
                    STATE_BUTTON_X = true;
                    if (customizeQuickAccess.Checked)
                    {
                        QuickAccessX.AudioType = CurrentAudioType;
                        QuickAccessX.BaseOctave = BaseOctave;
                        MessageBox.Show("Got it", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        CurrentAudioType = QuickAccessX.AudioType;
                        UpdateAudioType();
                        BaseOctave = QuickAccessX.BaseOctave;
                        UpdateOctave();
                    }
                }
                else if (Buttons == GamepadButtonFlags.Y && !STATE_BUTTON_Y)
                {
                    STATE_BUTTON_Y = true;
                    if (customizeQuickAccess.Checked)
                    {
                        QuickAccessY.AudioType = CurrentAudioType;
                        QuickAccessY.BaseOctave = BaseOctave;
                        MessageBox.Show("Got it", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        CurrentAudioType = QuickAccessY.AudioType;
                        UpdateAudioType();
                        BaseOctave = QuickAccessY.BaseOctave;
                        UpdateOctave();
                    }
                }
                else if (Buttons == GamepadButtonFlags.A && !STATE_BUTTON_A)
                {
                    STATE_BUTTON_A = true;
                    if (customizeQuickAccess.Checked)
                    {
                        QuickAccessA.AudioType = CurrentAudioType;
                        QuickAccessA.BaseOctave = BaseOctave;
                        MessageBox.Show("Got it", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        CurrentAudioType = QuickAccessA.AudioType;
                        UpdateAudioType();
                        BaseOctave = QuickAccessA.BaseOctave;
                        UpdateOctave();
                    }
                }
                else if (Buttons == GamepadButtonFlags.B && !STATE_BUTTON_B)
                {
                    STATE_BUTTON_B = true;
                    if (customizeQuickAccess.Checked)
                    {
                        QuickAccessB.AudioType = CurrentAudioType;
                        QuickAccessB.BaseOctave = BaseOctave;
                        MessageBox.Show("Got it", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        CurrentAudioType = QuickAccessB.AudioType;
                        UpdateAudioType();
                        BaseOctave = QuickAccessB.BaseOctave;
                        UpdateOctave();
                    }
                }

                //                                keytar                          MPA                       keytar pedal
                var hasPedal = gamepad.RightThumbY == 32640 || gamepad.RightThumbY == 128 || gamepad.RightThumbY == -256;
                if (hasPedal && !STATE_PEDAL)
                {
                    STATE_PEDAL = true;
                    UpdatePedal();
                }
                else if (!hasPedal && STATE_PEDAL)
                {
                    STATE_PEDAL = false;
                    UpdatePedal();
                }
                STATE_PEDAL = hasPedal;

                //code provided by david of the Phase Shift team - thanks!
                //Generate The Current State Of All The Keys
                //1-8
                for (var i = 0; i < 8; i++)
                {
                    CurrentKeysState[i] = (byte)(LoByte(gamepad.LeftTrigger) & (128 >> i));
                }
                //9-16
                for (var i = 0; i < 8; i++)
                {
                    CurrentKeysState[i + 8] = (byte)(LoByte(gamepad.RightTrigger) & (128 >> i));
                }
                //17-24
                for (var i = 0; i < 8; i++)
                {
                    CurrentKeysState[i + 16] = (byte)(LoByte(gamepad.LeftThumbX) & (128 >> i));
                }
                //25
                CurrentKeysState[24] = (byte)(HiByte(gamepad.LeftThumbX) & 128);

                //Compare The Current States To Last States To Generate Change Events
                for (var i = 0; i < 25; i++)
                {
                    if (CurrentKeysState[i] != LastKeysState[i])
                    {
                        if (!STATE_INPUT[i])
                        {
                            PlaySample(i, Velocity);
                        }
                        else
                        {
                            if (!STATE_PEDAL && !forcePedal)
                            {
                                StopSample(i);
                            }
                            else
                            {
                                STATE_INPUT[i] = false;
                                UpdatePressedKeys(i);
                            }
                        }
                    }
                    LastKeysState[i] = CurrentKeysState[i];
                }
                picPedal.Image = forcePedal || STATE_PEDAL ? RESOURCE_PEDAL_ON : RESOURCE_PEDAL_OFF;
            }
            catch (Exception)
            { }
        }
        
        private void debugInput_Click(object sender, EventArgs e)
        {
            lblDebug.Visible = debugInput.Checked && KeysTimer.Enabled;
            lblDebug.Text = "DEBUG = ";
        }
        
        private void songPreparer_DoWork(object sender, DoWorkEventArgs e)
        {
            cboCharts.Invoke(new MethodInvoker(() => cboCharts.Visible = false));
            cboCharts.Invoke(new MethodInvoker(() => cboCharts.Items.Clear()));
            loadCON();
        }

        private void songPreparer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            picWorking.Visible = false;
            if (string.IsNullOrEmpty(SongTitle) || string.IsNullOrEmpty(SongArtist)) return;
            cboCharts.Visible = showChartSelection.Checked;
            cboCharts.Items.Add("Expert (" + MIDITools.MIDI_Chart.ProKeysX.ChartedNotes.Count + " notes)");
            cboCharts.Items.Add("Hard (" + MIDITools.MIDI_Chart.ProKeysH.ChartedNotes.Count + " notes)");
            cboCharts.Items.Add("Medium (" + MIDITools.MIDI_Chart.ProKeysM.ChartedNotes.Count + " notes)");
            cboCharts.Items.Add("Easy (" + MIDITools.MIDI_Chart.ProKeysE.ChartedNotes.Count + " notes)");
            cboCharts.Items.Add("RH Anim (" + MIDITools.MIDI_Chart.ProKeysRH.ChartedNotes.Count + " notes)");
            cboCharts.Items.Add("LH Anim (" + MIDITools.MIDI_Chart.ProKeysLH.ChartedNotes.Count + " notes)");
            cboCharts.SelectedIndex = 0;
            picTrack.Focus();
            lblStatus.Text = "[Ready] \"" + SongTitle + "\" by " + SongArtist;
            lblTime.Text = "0:00 / " + SongLength;
            lblForBPM.Visible = true;
            lblBPM.Text = SongBPM.ToString("0.0");
            lblBPM.Visible = true;
            picPlayPause.Enabled = true;
            if (playAlongMode.Checked) return;
            playAlongMode.Checked = true;
            Height = playAlongMode.Checked ? EXTENDED_HEIGHT : NORMAL_HEIGHT;
        }

        private void loadCON()
        {
            StopEverything();
            SongArtist = "";
            SongTitle = "";
            SongLength = "";
            SongLengthDouble = 0.0;
            if (string.IsNullOrEmpty(SongFile) || !File.Exists(SongFile)) return;
            Tools.DeleteFile(TempMIDI);
            byte[] xMogg;
            if (!Parser.ExtractDTA(SongFile))
            {
                MessageBox.Show("Something went wrong extracting the songs.dta file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Parser.ReadDTA(Parser.DTA) || !Parser.Songs.Any())
            {
                MessageBox.Show("Something went wrong reading the songs.dta file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (Parser.Songs.Count > 1)
            {
                MessageBox.Show("It looks like this is a pack but I can only work with single songs\nUse Quick Pack Editor in C3 CON Tools to split your pack into individual files", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            var xPackage = new STFSPackage(SongFile);
            if (!xPackage.ParseSuccess)
            {
                MessageBox.Show("There was an error parsing that song file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var internal_name = Parser.Songs[0].InternalName;
            try
            {
                var xFile = xPackage.GetFile("songs/" + internal_name + "/" + internal_name + ".mid");
                if (xFile == null || !xFile.Extract(TempMIDI))
                {
                    MessageBox.Show("There was an error extracting the MIDI file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    xPackage.CloseIO();
                    return;
                }
                xFile = xPackage.GetFile("songs/" + internal_name + "/" + internal_name + ".mogg");
                if (xFile == null)
                {
                    MessageBox.Show("There was an error extracting the audio file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    xPackage.CloseIO();
                    return;
                }
                xMogg = xFile.Extract();
            }
            catch (Exception)
            {
                MessageBox.Show("There was an error parsing that song file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                xPackage.CloseIO();
                return;
            }
            xPackage.CloseIO();
            if (xMogg == null || xMogg.Length == 0)
            {
                MessageBox.Show("There was an error extracting the audio file, can't play that song", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Tools.DecM(xMogg, true, false, false, DecryptMode.ToMemory))
            {
                MessageBox.Show("That song is encrypted and I can't play it", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!loadMIDI()) return;
            SongArtist = Parser.Songs[0].Artist;
            SongTitle = Parser.Songs[0].Name;
            SongLengthDouble = ProcessMogg();
            SongLength = Parser.GetSongDuration(SongLengthDouble/ 1000.0);
        }

        private void StopEverything()
        {
            StopPlayback();
            Tools.ReleaseStreamHandle(false);
            Tools.PlayingSongOggData = new byte[0];
            PlaybackSeconds = 0;
            SongLengthDouble = 0.0;
            SongLength = "";
            SongArtist = "";
            SongTitle = "";
            UpdateTime();
            picTrack.Invalidate();
            picPlayPause.Invoke(new MethodInvoker(() => picPlayPause.Enabled = false));
            lblStatus.Invoke(new MethodInvoker(() => lblStatus.Text = NothingLoaded));
            lblSection.Invoke(new MethodInvoker(() => lblSection.Text = ""));
            lblBPM.Invoke(new MethodInvoker(() => lblBPM.Visible = false));
            lblForBPM.Invoke(new MethodInvoker(() => lblForBPM.Visible = false));
            MIDITools.Initialize();
        }

        private void StopPlayback(bool Pause = false)
        {
            try
            {
                PlaybackTimer.Enabled = false;
                if (Pause)
                {
                    if (!Bass.BASS_ChannelPause(BassMixer))
                    {
                        MessageBox.Show("Error pausing playback\n" + Bass.BASS_ErrorGetCode());
                    }
                    lblStatus.Invoke(new MethodInvoker(() => lblStatus.Text = "[Paused] \"" + SongTitle + "\" by " + SongArtist));
                }
                else
                {
                    StopBASS();
                    PlaybackSeconds = 0;
                    lblStatus.Invoke(new MethodInvoker(() => lblStatus.Text = "[Ready] \"" + SongTitle + "\" by " + SongArtist));
                    lblSection.Invoke(new MethodInvoker(() => lblSection.Text = ""));
                    ResetPlayedNotes();
                }
            }
            catch (Exception)
            { }
            picPlayPause.Invoke(new MethodInvoker(() => picPlayPause.Image = RESOURCE_BUTTON_PLAY));
            picPlayPause.BeginInvoke(new Action(() => toolTip1.SetToolTip(picPlayPause, "Play")));
            StopAllSamples();
        }

        private void ResetPlayedNotes()
        {
            foreach (var note in ActiveChart.ChartedNotes)
            {
                note.Played = false;
                note.Stopped = false;
            }
        }

        private void StopBASS()
        {
            try
            {
                Bass.BASS_ChannelStop(BassMixer);
                Bass.BASS_StreamFree(BassMixer);
            }
            catch (Exception)
            { }
        }

        private void UpdateTime()
        {
            if (string.IsNullOrEmpty(SongLength))
            {
                lblTime.Invoke(new MethodInvoker(() => lblTime.Text = "0:00 / 0:00"));
                return;
            }
            string time;
            if (PlaybackSeconds >= 3600)
            {
                var hours = (int)(PlaybackSeconds / 3600);
                var minutes = (int)(PlaybackSeconds - (hours * 3600));
                var seconds = (int)(PlaybackSeconds - (minutes * 60));
                time = hours + ":" + (minutes < 10 ? "0" : "") + minutes + ":" + (seconds < 10 ? "0" : "") + seconds;
            }
            else if (PlaybackSeconds >= 60)
            {
                var minutes = (int)(PlaybackSeconds / 60);
                var seconds = (int)(PlaybackSeconds - (minutes * 60));
                time = minutes + ":" + (seconds < 10 ? "0" : "") + seconds;
            }
            else
            {
                time = "0:" + (PlaybackSeconds < 10 ? "0" : "") + (int)PlaybackSeconds;
            }
            if (lblTime.InvokeRequired)
            {
                lblTime.Invoke(new MethodInvoker(() => lblTime.Text = time + " / " + SongLength));
            }
            else
            {
                lblTime.Text = lblTime.Text = time + " / " + SongLength;
            }
        }

        private long ProcessMogg()
        {
            try
            {
                var stream = Bass.BASS_StreamCreateFile(Tools.GetOggStreamIntPtr(false), 0L, Tools.PlayingSongOggData.Length, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
                var len = Bass.BASS_ChannelGetLength(stream);
                var totaltime = Bass.BASS_ChannelBytes2Seconds(stream, len); // the total time length
                return (int)(totaltime * 1000);
            }
            catch (Exception)
            { }
            return 0;
        }

        private bool loadMIDI()
        {
            if (!File.Exists(TempMIDI)) return false;
            MIDITools.Initialize();
            if (!MIDITools.ReadMIDIFile(TempMIDI))
            {
                MessageBox.Show("Couldn't read that MIDI file, won't be able to play the pro keys chart", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Tools.DeleteFile(TempMIDI);
                StopEverything();
                return false;
            }
            Tools.DeleteFile(TempMIDI);
            if (MIDITools.MIDI_Chart.ProKeysX.ChartedNotes.Count > 0)
            {
                SongBPM = Math.Round(MIDITools.MIDI_Chart.AverageBPM, 1);
                return true;
            }
            MessageBox.Show("That song doesn't have a pro keys chart, nothing to play", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            StopEverything();
            return false;
        }
        
        private static int GetRangePos(int range)
        {
            switch (range)
            {
                default:
                    return 54;
                case 2:
                    return 111;
                case 4:
                    return 168;
                case 5:
                    return 225;
                case 7:
                    return 283;
                case 9:
                    return 340;
            }
        }

        private void DoPracticeSessions()
        {
            if (!MIDITools.PracticeSessions.Any())
            {
                lblSection.Text = "";
                return;
            }
            lblSection.Text = GetCurrentSection(GetCorrectedTime());
        }

        private string GetCurrentSection(double time)
        {
            var curr_session = "";
            foreach (var session in MIDITools.PracticeSessions.TakeWhile(session => session.SectionStart <= time))
            {
                curr_session = session.SectionName;
            }
            return curr_session;
        }

        private double GetCorrectedTime()
        {
            return PlaybackSeconds - ((double)BassBuffer / 1000);
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Bass.BASS_ChannelIsActive(BassMixer) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    // the stream is still playing...
                    var pos = Bass.BASS_ChannelGetPosition(BassStream); // position in bytes
                    PlaybackSeconds = Bass.BASS_ChannelBytes2Seconds(BassStream, pos); // the elapsed time length
                    DrawVisuals();
                    UpdateTime();
                    DoPracticeSessions();
                }
                else
                {
                    StopPlayback();
                }
            }
            catch (Exception)
            { }
        }

        private void DrawVisuals()
        {
            if (MIDITools.MIDI_Chart == null || MIDITools.MIDI_Chart.ProKeysX.ChartedNotes.Count == 0) return;
            picTrack.Invalidate();
        }
        
        private void DrawNotes(Graphics graphics)
        {
            if (ActiveChart.ChartedNotes.Count == 0) return;
            const int diff = 48;
            var track = ActiveChart;
            var correctedTime = GetCorrectedTime();
            var goalY = panelHitBox.Top;
            for (var z = 0; z < track.ChartedNotes.Count(); z++)
            {
                var note = track.ChartedNotes[z];
                var key = note.NoteNumber - diff;
                if (note.NoteEnd <= correctedTime)
                {
                    if (autoPlayWithChart.Checked && note.NoteEnd + 0.5 <= correctedTime && note.Played && !note.Stopped)
                    {
                        if (!STATE_PEDAL && !forcePedal)
                        {
                            StopSample(key);
                        }
                        else
                        {
                            STATE_INPUT[key] = false;
                            UpdatePressedKeys(key);
                        }
                        note.Stopped = true;
                    }
                    continue;
                }
                if (note.NoteStart > correctedTime + (PlaybackWindow*2)) break;
                
                //play along
                if (autoPlayWithChart.Checked && note.NoteStart <= correctedTime && !note.Played)
                {
                    PlaySample(key, 50);
                    note.Played = true;
                }
                
                var img = note.NoteName.Contains("#") ? (note.hasOD ? RESOURCE_NOTE_BLACK_OD : RESOURCE_NOTE_BLACK) : (note.hasOD ? RESOURCE_NOTE_WHITE_OD : RESOURCE_NOTE_WHITE);
                var posX = NotePosX[note.NoteNumber - diff];
                var posY = goalY - (((note.NoteStart - correctedTime) / PlaybackWindow) * goalY);

                //draw tail first
                if (note.NoteLength > note.TicksPerQuarterNote / 4.0) //longer than a 1/16 note
                {
                    var posY2 = goalY - (((note.NoteEnd - correctedTime) / PlaybackWindow) * goalY);
                    var tail_height = posY2 - posY;
                    if (tail_height < 0)
                    {
                        tail_height *= -1;
                    }
                    const int tail_width = 4;
                    using (var solidBrush = new SolidBrush(note.hasOD ? Color.LightGoldenrodYellow : (note.NoteName.Contains("#") ? Color.DimGray : Color.WhiteSmoke)))
                    {
                        graphics.FillRectangle(solidBrush, posX + (img.Width / 2) - (tail_width / 2), (int)(posY - tail_height), tail_width, (int)tail_height);
                    }
                }
                
                //draw note on top of tail
                graphics.DrawImage(img, posX, (int)posY - img.Height, img.Width, img.Height);
            }
        }
        
        private void StartPlayback()
        {
            if (Tools.PlayingSongOggData.Count() == 0)
            {
                MessageBox.Show("Couldn't play that song, sorry", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                StopPlayback();
                return;
            }

            // create a decoder for the OGG file
            BassStream = Bass.BASS_StreamCreateFile(Tools.GetOggStreamIntPtr(false), 0L,
                Tools.PlayingSongOggData.Length, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
            var channel_info = Bass.BASS_ChannelGetInfo(BassStream);

            // create a stereo mixer with same frequency rate as the input file
            BassMixer = BassMix.BASS_Mixer_StreamCreate(channel_info.freq, 2, BASSFlag.BASS_MIXER_END);
            BassMix.BASS_Mixer_StreamAddChannel(BassMixer, BassStream, BASSFlag.BASS_MIXER_MATRIX);

            //get and apply channel matrix
            var matrix = Tools.GetChannelMatrix(Parser.Songs[0], channel_info.chans,
                (silenceKeysTrack.Checked ? "" : "keys|") + "bass|guitar|vocals|drums|backing");
            BassMix.BASS_Mixer_ChannelSetMatrix(BassStream, matrix);

            //set location
            BassMix.BASS_Mixer_ChannelSetPosition(BassStream,
                Bass.BASS_ChannelSeconds2Bytes(BassStream, PlaybackSeconds));

            //apply volume correction to entire track
            Bass.BASS_ChannelSetAttribute(BassMixer, BASSAttribute.BASS_ATTRIB_VOL,
                forceMuteTrack ? 0 : (float)TrackVolume);

            //start mix playback
            Bass.BASS_ChannelPlay(BassMixer, true);

            PlaybackTimer.Enabled = true;
            picPlayPause.Image = RESOURCE_BUTTON_PAUSE;
            toolTip1.SetToolTip(picPlayPause, "Pause");
            lblStatus.Text = "[Playing] \"" + SongTitle + "\" by " + SongArtist;
        }
        
        private void silenceKeysTrack_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SongArtist) || string.IsNullOrEmpty(SongTitle)) return;
            var wasPlaying = PlaybackTimer.Enabled;
            var time = PlaybackSeconds;
            StopPlayback();
            if (!wasPlaying) return;
            PlaybackSeconds = time;
            StartPlayback();
        }

        private void HandleDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
        }

        private void HandleDragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Environment.CurrentDirectory = Path.GetDirectoryName(files[0]);
            if (VariousFunctions.ReadFileType(files[0]) != XboxFileType.STFS)
            {
                MessageBox.Show("That's not a valid file to drop here", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (!playAlongMode.Checked)
            {
                playAlongMode.Checked = true;
                playAlongMode_Click(null, null);
            }
            SongFile = files[0];
            picWorking.Visible = true;
            StopAllSamples();
            songPreparer.RunWorkerAsync();
        }

        private void playAlongMode_Click(object sender, EventArgs e)
        {
            Height = playAlongMode.Checked ? EXTENDED_HEIGHT : NORMAL_HEIGHT;
            StopEverything();
        }

        private void playAlongMode_CheckedChanged(object sender, EventArgs e)
        {
            var visible = playAlongMode.Checked;
            picTrack.Visible = visible;
            picVolume.Visible = visible;
            picPlayPause.Visible = visible;
            picMute.Visible = visible;
            lblTime.Visible = visible;
            lblVolume.Visible = visible;
            lblForVolume.Visible = visible;
            silenceKeysTrack.Enabled = visible;
            autoPlayWithChart.Enabled = visible;
            showRangeMarker.Enabled = visible;
            showScrollSpeedControl.Enabled = visible;
            panelScroll.Visible = visible && showScrollSpeedControl.Checked;
            picScroll.Visible = panelScroll.Visible;
            showChartSelection.Enabled = visible;
            if (!playAlongMode.Checked)
            {
                cboCharts.Visible = false;
            }
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) return;
            NotifyTray.ShowBalloonTip(250);
            Hide();
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Space && !customizeKeyboardShortcuts.Checked)
            {
                STATE_PEDAL = true;
                UpdatePedal();
                return;
            }
            if (e.KeyData == Keys.Escape)
            {
                customizeKeyboardShortcuts.Checked = false;
                customizeKeyboardShortcuts_Click(null,null);
                customizeQuickAccess.Checked = false;
                return;
            }
            if (customizeKeyboardShortcuts.Checked)
            {
                if (ActiveKey == -1)
                {
                    MessageBox.Show("Please click on the keytar key you want to assign a keyboard shortcut to first, then press the keyboard key to assign to it",
                        AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                KeyboardShortcuts[ActiveKey] = e.KeyData;
                UpdateShortcutLabelsText();
                StopAllSamples();
                ActiveKey = -1;
                return;
            }
            if (!KeyboardShortcuts.Contains(e.KeyData)) return;
            for (var i = 0; i < KeyboardShortcuts.Count; i++)
            {
                if (KeyboardShortcuts[i] != e.KeyData) continue;
                if (STATE_INPUT[i]) break;
                PressKey(i);
                break;
            }
        }

        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Space && !customizeKeyboardShortcuts.Checked)
            {
                STATE_PEDAL = false;
                UpdatePedal();
                return;
            }
            if (!KeyboardShortcuts.Contains(e.KeyData)) return;
            for (var i = 0; i < KeyboardShortcuts.Count; i++)
            {
                if (KeyboardShortcuts[i] != e.KeyData) continue;
                if (forcePedal || STATE_PEDAL)
                {
                    STATE_INPUT[i] = false;
                    UpdatePressedKeys(i);
                }
                else
                {
                    StopSample(i);
                }
                break;
            }
        }

        private void picVolume_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var Volume = new Volume(this, Cursor.Position);
            Volume.Show();
        }

        private void picPedal_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || customizeKeyboardShortcuts.Checked) return;
            forcePedal = !forcePedal;
            UpdatePedal();
        }

        private void StopAllSamples()
        {
            for (var i = 0; i < 127; i++)
            {
                MidiPlayer.Play(new NoteOff(0, 1, (byte) i, 127));
            }
            for (var i = 0; i < 25; i++)
            {
                StopSample(i);
                CurrentKeysState[i] = new byte();
                LastKeysState[i] = new byte();
            }
        }

        private void howToUseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var message = Tools.ReadHelpFile("keytar");
            var help = new HelpForm(AppName + " - Help", message, true);
            help.ShowDialog();
        }
        
        private void picLight_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (LCDColorIndex == 7)
            {
                LCDColorIndex = 0;
            }
            else
            {
                LCDColorIndex++;
            }
            UpdateLCDBacklight();
        }

        private void UpdateLCDBacklight()
        {
            switch (LCDColorIndex)
            {
                case 1:
                    panelLCD.BackColor = LCDColor2;
                    break;
                case 2:
                    panelLCD.BackColor = LCDColor3;
                    break;
                case 3:
                    panelLCD.BackColor = LCDColor4;
                    break;
                case 4:
                    panelLCD.BackColor = LCDColor5;
                    break;
                case 5:
                    panelLCD.BackColor = LCDColor6;
                    break;
                case 6:
                    panelLCD.BackColor = LCDColor7;
                    break;
                case 7:
                    panelLCD.BackColor = LCDColor8;
                    break;
                default:
                    panelLCD.BackColor = LCDColor1;
                    break;
            }
            panelAudio.BackColor = panelLCD.BackColor;
            panelScroll.BackColor = panelLCD.BackColor;
        }

        private void picMute_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            forceMuteTrack = !forceMuteTrack;
            UpdateTrackVolume();
        }

        private void picLoad_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var ofd = new OpenFileDialog
            {
                Title = "Open song file",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (string.IsNullOrEmpty(ofd.FileName) || !File.Exists(ofd.FileName)) return;
            if (VariousFunctions.ReadFileType(ofd.FileName) != XboxFileType.STFS) return;
            Environment.CurrentDirectory = Path.GetDirectoryName(ofd.FileName);
            SongFile = ofd.FileName;
            picWorking.Visible = true;
            StopAllSamples();
            songPreparer.RunWorkerAsync();
        }

        private void picPlayPause_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (string.IsNullOrEmpty(SongArtist) || string.IsNullOrEmpty(SongTitle))
            {
                picPlayPause.Enabled = false;
                return;
            }
            try
            {
                switch (Bass.BASS_ChannelIsActive(BassMixer))
                {
                    case BASSActive.BASS_ACTIVE_PLAYING:
                        StopPlayback(true);
                        UpdateTime();
                        break;
                    case BASSActive.BASS_ACTIVE_PAUSED:
                        Bass.BASS_ChannelPlay(BassMixer, false);
                        PlaybackTimer.Enabled = true;
                        picPlayPause.Image = RESOURCE_BUTTON_PAUSE;
                        toolTip1.SetToolTip(picPlayPause, "Pause");
                        lblStatus.Text = "[Playing] \"" + SongTitle + "\" by " + SongArtist;
                        break;
                    default:
                        StartPlayback();
                        break;
                }
            }
            catch (Exception)
            {
                StartPlayback();
            }
        }
        
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!picWorking.Visible && !songPreparer.IsBusy)
            {
                MidiPlayer.CloseMidi();
                SaveConfig();
                Tools.DeleteFile(TempMIDI);
                return;
            }
            MessageBox.Show("Please wait for the current process to finish", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            e.Cancel = true;
        }

        private void picOctaveUp_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (BaseOctave == 5) return;
            BaseOctave++;
            UpdateOctave();
        }

        private void picOctaveDown_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (BaseOctave == 0) return;
            BaseOctave--;
            UpdateOctave();
        }

        private void picAudioUp_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            CurrentAudioType--;
            UpdateAudioType();
        }

        private void picAudioDown_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            CurrentAudioType++;
            UpdateAudioType();
        }

        private void UpdateAudioType(bool loading = false)
        {
            if (CurrentAudioType > 40)
            {
                CurrentAudioType = 0;
            }
            else if (CurrentAudioType < 0)
            {
                CurrentAudioType = 40;
            }
            if (!loading)
            {
                StopAllSamples();
            }

            GeneralMidiInstruments instrumentType;
            string instrumentName;
            switch (CurrentAudioType)
            {
                case 1:
                    instrumentType = GeneralMidiInstruments.AcousticGrand;
                    instrumentName = "Grand Piano";
                    break;
                case 2:
                    instrumentType = GeneralMidiInstruments.HonkyTonk;
                    instrumentName = "Honky Tonk";
                    break;
                case 3:
                    instrumentType = GeneralMidiInstruments.Harpsichord;
                    instrumentName = "Harpsichord";
                    break;
                case 4:
                    instrumentType = GeneralMidiInstruments.Clav;
                    instrumentName = "Clavichord";
                    break;
                case 5:
                    instrumentType = GeneralMidiInstruments.ElectricPiano1;
                    instrumentName = "Electric Piano";
                    break;
                case 6:
                    instrumentType = GeneralMidiInstruments.ChurchOrgan;
                    instrumentName = "Church Organ";
                    break;
                case 7:
                    instrumentType = GeneralMidiInstruments.ReedOrgan;
                    instrumentName = "Reed Organ";
                    break;
                case 8:
                    instrumentType = GeneralMidiInstruments.RockOrgan;
                    instrumentName = "Rock Organ";
                    break;
                case 9:
                    instrumentType = GeneralMidiInstruments.Glockenspiel;
                    instrumentName = "Glockenspiel";
                    break;
                case 10:
                    instrumentType = GeneralMidiInstruments.Dulcimer;
                    instrumentName = "Dulcimer";
                    break;
                case 11:
                    instrumentType = GeneralMidiInstruments.Xylophone;
                    instrumentName = "Xylophone";
                    break;
                case 12:
                    instrumentType = GeneralMidiInstruments.Marimba;
                    instrumentName = "Marimba";
                    break;
                case 13:
                    instrumentType = GeneralMidiInstruments.TubularBells;
                    instrumentName = "Tub. Bells";
                    break;
                case 14:
                    instrumentType = GeneralMidiInstruments.BrassSection;
                    instrumentName = "Brass";
                    break;
                case 15:
                    instrumentType = GeneralMidiInstruments.Recorder;
                    instrumentName = "Recorder";
                    break;
                case 16:
                    instrumentType = GeneralMidiInstruments.Flute;
                    instrumentName = "Flute";
                    break;
                case 17:
                    instrumentType = GeneralMidiInstruments.Clarinet;
                    instrumentName = "Clarinet";
                    break;
                case 18:
                    instrumentType = GeneralMidiInstruments.AltoSax;
                    instrumentName = "Alto Sax";
                    break;
                case 19:
                    instrumentType = GeneralMidiInstruments.Oboe;
                    instrumentName = "Oboe";
                    break;
                case 20:
                    instrumentType = GeneralMidiInstruments.Trumpet;
                    instrumentName = "Trumpet";
                    break;
                case 21:
                    instrumentType = GeneralMidiInstruments.Trombone;
                    instrumentName = "Trombone";
                    break;
                case 22:
                    instrumentType = GeneralMidiInstruments.Tuba;
                    instrumentName = "Tuba";
                    break;
                case 23:
                    instrumentType = GeneralMidiInstruments.EnglishHorn;
                    instrumentName = "Eng. Horn";
                    break;
                case 24:
                    instrumentType = GeneralMidiInstruments.Bassoon;
                    instrumentName = "Bassoon";
                    break;
               case 25:
                    instrumentType = GeneralMidiInstruments.StringEnsemble1;
                    instrumentName = "Strings";
                    break;
               case 26:
                    instrumentType = GeneralMidiInstruments.Violin;
                    instrumentName = "Violin";
                    break;
               case 27:
                    instrumentType = GeneralMidiInstruments.Viola;
                    instrumentName = "Viola";
                    break;
                case 28:
                    instrumentType = GeneralMidiInstruments.Cello;
                    instrumentName = "Cello";
                    break;
                case 29:
                    instrumentType = GeneralMidiInstruments.NylonAcousticGuitar;
                    instrumentName = "Ac. Guitar";
                    break;
                case 30:
                    instrumentType = GeneralMidiInstruments.CleanElectricGuitar;
                    instrumentName = "El. Guitar";
                    break;
                case 31:
                    instrumentType = GeneralMidiInstruments.AcousticBass;
                    instrumentName = "Ac. Bass";
                    break;
                case 32:
                    instrumentType = GeneralMidiInstruments.FingerElectricBass;
                    instrumentName = "El. Bass";
                    break;
                case 33:
                    instrumentType = GeneralMidiInstruments.Banjo;
                    instrumentName = "Banjo";
                    break;
                case 34:
                    instrumentType = GeneralMidiInstruments.Sitar;
                    instrumentName = "Sitar";
                    break;
                case 35:
                    instrumentType = GeneralMidiInstruments.Accoridan;
                    instrumentName = "Accordion";
                    break;
                case 36:
                    instrumentType = GeneralMidiInstruments.Bagpipe;
                    instrumentName = "Bagpipe";
                    break;
                case 37:
                    instrumentType = GeneralMidiInstruments.Harmonica;
                    instrumentName = "Harmonica";
                    break;
                case 38:
                    instrumentType = GeneralMidiInstruments.VoiceOohs;
                    instrumentName = "Voices";
                    break;
                case 39:
                    instrumentType = GeneralMidiInstruments.Ocarina;
                    instrumentName = "Ocarina";
                    break;
                case 40:
                    instrumentType = GeneralMidiInstruments.SciFi;
                    instrumentName = "SciFi";
                    break;
                default:
                    if (!loading)
                    {
                        LoadPianoSamples();
                    }
                    instrumentType = GeneralMidiInstruments.AcousticGrand;
                    instrumentName = "Real Piano";
                    break;
            }
            MidiPlayer.Play(new ProgramChange(0, 1, instrumentType));
            lblSound.Text = instrumentName;
        }

        private void CheckLCDFontIsAvailable()
        {
            const string fontName = "Quartz MS";
            try
            {
                const float fontSize = 10;
                using (var fontTester = new Font(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    if (fontTester.Name == fontName) return;
                }
            }
            catch (Exception)
            {}
            const string message = AppName + " uses " + fontName + " for the text in the LCD displays - and it looks like you don't have it " +
                                   "installed!\n Click OK to open the /res/ folder and locate the included font file - install that and then run " +
                                   AppName + " again";
            if (MessageBox.Show(message, "Missing Font", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK) return;
            Process.Start("explorer.exe", "/select," + Application.StartupPath + "\\res\\font.ttf");
            Dispose();
        }

        private void picFlatB4_MouseMove(object sender, MouseEventArgs e)
        {
            if (!MouseIsPressed) return;
            ((PictureBox)sender).Capture = false;
            var key = Convert.ToInt16(((PictureBox) sender).Tag);
            if (!STATE_INPUT[key])
            {
                PlaySample(key, 100);
            }
        }

        private void picPedal_MouseEnter(object sender, EventArgs e)
        {
            MouseIsPressed = false;
        }

        private void autoPlayWithChart_Click(object sender, EventArgs e)
        {
            if (!autoPlayWithChart.Checked)
            {
                StopAllSamples();
            }
        }

        private void panelKeytar_MouseUp(object sender, MouseEventArgs e)
        {
            picTrack.Focus();
            MouseIsPressed = false;
            if (STATE_PEDAL || forcePedal) return;
            StopAllSamples();
        }

        private void showScrollSpeedControl_CheckedChanged(object sender, EventArgs e)
        {
            picScroll.Visible = showScrollSpeedControl.Checked && playAlongMode.Checked;
            panelScroll.Visible = picScroll.Visible;
        }
        
        private void picScroll_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            MouseIsOnScroll = true;
            picScroll.Image = RESOURCE_SCROLL_ACTIVE;
        }

        private void picScroll_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            MouseIsOnScroll = false;
            picScroll.Image = RESOURCE_SCROLL;
        }

        private void picScroll_MouseMove(object sender, MouseEventArgs e)
        {
            if (!MouseIsOnScroll) return;
            if (MousePosition.Y < LastScrollY && PlaybackWindow < 5.0)
            {
                PlaybackWindow += 0.1;
            }
            else if (MousePosition.Y > LastScrollY && PlaybackWindow > 0.5)
            {
                PlaybackWindow -= 0.1;
            }
            LastScrollY = MousePosition.Y;
            picScroll.Image = LastScrollY%2 == 0 ? RESOURCE_SCROLL_ACTIVE : RESOURCE_SCROLL_ACTIVE2;
            UpdateScrollSpeed();
        }
        
        private void picTrack_Paint(object sender, PaintEventArgs e)
        {
            if (PlaybackSeconds == 0) return;
            //draw solo overlay
            var isSolo = MIDITools.MIDI_Chart.ProKeysX.Solos != null && MIDITools.MIDI_Chart.ProKeysX.Solos.Any(solo => solo.MarkerBegin <= PlaybackSeconds && solo.MarkerEnd > PlaybackSeconds);
            if (isSolo)
            {
                var height = 0;
                do
                {
                    e.Graphics.DrawImage(RESOURCE_TRACK_SOLO, 0, height, picTrack.Width, RESOURCE_TRACK_SOLO.Height);
                    height += RESOURCE_TRACK_SOLO.Height;
                } while (height < picTrack.Height);

            }
            //draw hitbox
            e.Graphics.DrawImage(RESOURCE_HITBOX, panelHitBox.Left - picTrack.Left, panelHitBox.Top - picTrack.Top, panelHitBox.Width, panelHitBox.Height);
            //draw range marker
            if (showRangeMarker.Checked && cboCharts.SelectedIndex < 4)
            {
                var correctedTime = GetCorrectedTime();
                var range = MIDITools.MIDI_Chart.RangeShifts[0].ShiftNote;
                if (MIDITools.MIDI_Chart.RangeShifts.Any())
                {
                    foreach (var marker in MIDITools.MIDI_Chart.RangeShifts.TakeWhile(marker => marker.ShiftBegin <= correctedTime))
                    {
                        range = marker.ShiftNote;
                    }
                }
                using (var solidBrush = new SolidBrush(RangeMarkerColor))
                {
                    e.Graphics.FillRectangle(solidBrush, GetRangePos(range) - picTrack.Left, panelHitBox.Top - picTrack.Top + panelHitBox.Height, 571, 10);
                }
            }
            //draw chart notes
            DrawNotes(e.Graphics);
        }

        private void cboCharts_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboCharts.SelectedIndex)
            {
                case 0:
                    ActiveChart = MIDITools.MIDI_Chart.ProKeysX;
                    break;
                case 1:
                    ActiveChart = MIDITools.MIDI_Chart.ProKeysH;
                    break;
                case 2:
                    ActiveChart = MIDITools.MIDI_Chart.ProKeysM;
                    break;
                case 3:
                    ActiveChart = MIDITools.MIDI_Chart.ProKeysE;
                    break;
                case 4:
                    ActiveChart = MIDITools.MIDI_Chart.ProKeysRH;
                    break;
                case 5:
                    ActiveChart = MIDITools.MIDI_Chart.ProKeysLH;
                    break;
                    default:
                    ActiveChart = new MIDITrack();
                    break;
            }
            if (ActiveChart.ChartedNotes.Count > 0) return;
            MessageBox.Show("No notes to draw, select a different chart", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            cboCharts.SelectedIndex = 0;
        }
        
        private void cboCharts_MouseLeave(object sender, EventArgs e)
        {
            picTrack.Focus();
        }

        private void picTrack_MouseClick(object sender, MouseEventArgs e)
        {
            picTrack.Focus();
        }

        private void frmMain_MouseClick(object sender, MouseEventArgs e)
        {
            picTrack.Focus();
        }

        private void showChartSelection_Click(object sender, EventArgs e)
        {
            cboCharts.Visible = showChartSelection.Checked && playAlongMode.Checked;
        }

        private void customizeQuickAccess_Click(object sender, EventArgs e)
        {
            if (customizeQuickAccess.Checked)
            {
                MessageBox.Show("Use your mouse to change the sound type and octave on the right LCD panel, then press the QuickAccess button you want to assign to them\n" +
                                "Repeat as needed, then uncheck this option or press the Esc key", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void changeMarkerColor_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = RangeMarkerColor;
            colorDialog1.CustomColors = new[] { ColorTranslator.ToOle(Color.Gray) };
            colorDialog1.SolidColorOnly = true;
            colorDialog1.ShowDialog();
            RangeMarkerColor = colorDialog1.Color;
        }

        private void resetHeight_Click(object sender, EventArgs e)
        {
            Height = playAlongMode.Checked ? EXTENDED_HEIGHT : NORMAL_HEIGHT;
        }

        private void showKeyboardShortcuts_Click(object sender, EventArgs e)
        {
            UpdateShortcutlabelsVisibility(showKeyboardShortcuts.Checked);
        }

        private void UpdateShortcutlabelsVisibility(bool visible)
        {
            foreach (var label in KeyboardShortcutLabels)
            {
                label.Visible = visible;
            }
        }

        private void UpdateShortcutLabelsText()
        {
            for (var i = 0; i < KeyboardShortcuts.Count; i++)
            {
                var key = CleanedShortcutKey(KeyboardShortcuts[i]);
                KeyboardShortcutLabels[i].Text = key;
                toolTip1.SetToolTip(KeyboardShortcutLabels[i], "Press " + key + " to play this key");
            }
        }

        private static string CleanedShortcutKey(Keys key)
        {
            string label;
            switch (key)
            {
                default:
                    label = key.ToString();
                    break;
                case Keys.OemBackslash:
                    label = "/";
                    break;
                case Keys.OemCloseBrackets:
                    label = "]";
                    break;
                case Keys.OemOpenBrackets:
                    label = "[";
                    break;
                case Keys.Subtract:
                case Keys.OemMinus:
                    label = "-";
                    break;
                case Keys.Decimal:
                case Keys.OemPeriod:
                    label = ".";
                    break;
                case Keys.OemPipe:
                    label = "|";
                    break;
                case Keys.OemQuestion:
                    label = "?";
                    break;
                case Keys.OemQuotes:
                    label = "\"";
                    break;
                case Keys.OemSemicolon:
                    label = ";";
                    break;
                case Keys.Oemcomma:
                    label = ",";
                    break;
                case Keys.Oemplus:
                case Keys.Add:
                    label = "+";
                    break;
                case Keys.Oemtilde:
                    label = "~";
                    break;
                case Keys.Divide:
                    label = "/";
                    break;
                case Keys.Multiply:
                    label = "*";
                    break;
            }
            return label.Replace("NumPad", "").Trim();
        }

        private void customizeKeyboardShortcuts_Click(object sender, EventArgs e)
        {
            if (customizeKeyboardShortcuts.Checked)
            {
                MessageBox.Show("Use your mouse to click the keytar key you want to assign a keyboard shortcut to, then press the keyboard key to assign to it\n" +
                                "Repeat as needed, then uncheck this option or press the Esc key", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateShortcutlabelsVisibility(true);
                STATE_PEDAL = false;
                forcePedal = false;
                UpdatePedal();
            }
            else
            {
                UpdateShortcutlabelsVisibility(showKeyboardShortcuts.Checked);
            }
            ActiveKey = -1;
        }

        private void lblDebug_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Clipboard.SetText(lblDebug.Text);
            MessageBox.Show("Debugging info copied to clipboard", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class QuickAccessButton
    {
        public int AudioType;
        public int BaseOctave;
    }

    public class KeytarKeys
    {
        public const int C2 = 0;
        public const int D2b = 1;
        public const int D2 = 2;
        public const int E2b = 3;
        public const int E2 = 4;
        public const int F2 = 5;
        public const int G2b = 6;
        public const int G2 = 7;
        public const int A2b = 8;
        public const int A2 = 9;
        public const int B2b = 10;
        public const int B2 = 11;
        public const int C3 = 12;
        public const int D3b = 13;
        public const int D3 = 14;
        public const int E3b = 15;
        public const int E3 = 16;
        public const int F3 = 17;
        public const int G3b = 18;
        public const int G3 = 19;
        public const int A3b = 20;
        public const int A3 = 21;
        public const int B3b = 22;
        public const int B3 = 23;
        public const int C4 = 24;
    }
}
