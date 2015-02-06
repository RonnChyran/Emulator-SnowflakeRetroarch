using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Reflection;
using Snowflake.Emulator;
using Snowflake.Emulator.Configuration;
using Snowflake.Emulator.Input;
using Snowflake.Emulator.Input.InputManager;
using Snowflake.Controller;
using Snowflake.Game;
using Snowflake.Platform;
using Snowflake.Service;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using Snowflake.InputManager;

namespace SnowflakeRA.bSNEScompatibility
{
    public class EmulatorBSNESCompatibility : EmulatorBridge
    {
        [ImportingConstructor]
        public EmulatorBSNESCompatibility([Import("coreInstance")] ICoreService coreInstance)
            : base(Assembly.GetExecutingAssembly(), coreInstance)
        {
        }

        private static readonly string retroArchConfigTemplate = "RetroArchCore";
        private static readonly string retroArchInputTemplate = "RetroArchInput";
        private Process retroArchInstance;
        public override void StartRom(IGameInfo game)
        {
            var emulatorPath =
                Path.Combine(this.CoreInstance.EmulatorManager.GetAssemblyDirectory(this.EmulatorAssembly), this.EmulatorAssembly.MainAssembly);
            
            var configProfile = this.ConfigurationTemplates[retroArchConfigTemplate].ConfigurationStore.GetConfigurationProfile(game);
            configProfile.ConfigurationValues["input_autodetect_enable"] = false; //Force no autodetect
            bool fullScreen = this.ConfigurationFlagStore.GetValue(game, "fullscreen_toggle", ConfigurationFlagTypes.BOOLEAN_FLAG);
            if(fullScreen){
                configProfile.ConfigurationValues["video_fullscreen"] = true; //Force no autodetect
            }
            var retroArchCfg = this.CompileConfiguration(this.ConfigurationTemplates[retroArchConfigTemplate], configProfile);
            var controller1 = this.CompileController(1, this.CoreInstance.LoadedPlatforms[StonePlatforms.NINTENDO_SNES], this.InputTemplates[retroArchInputTemplate]);
            var controller2 = this.CompileController(2, this.CoreInstance.LoadedPlatforms[StonePlatforms.NINTENDO_SNES], this.InputTemplates[retroArchInputTemplate]);
            

            File.WriteAllText(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"), retroArchCfg);
            File.AppendAllText(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"), Environment.NewLine + controller1);
            File.AppendAllText(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"), Environment.NewLine + controller2);
            var startInfo = new ProcessStartInfo(emulatorPath);
            startInfo.WorkingDirectory = Path.Combine(this.CoreInstance.EmulatorManager.GetAssemblyDirectory(this.EmulatorAssembly));
            startInfo.Arguments = String.Format(@"{0} --libretro ""cores/bsnes_balanced_libretro.dll"" --config retroarch.cfg.clean --appendconfig ""{1}""",
                game.FileName, Path.GetFullPath(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"))
              );

            this.retroArchInstance = Process.Start(startInfo);
            if (!fullScreen)
            {
                EmulatorBSNESCompatibility.MoveWindow(this.retroArchInstance.MainWindowHandle, 0, 0, 500, 500, true);
            }
        }
        /* Win32 Start */
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        /* Win32 End */
        public override string CompileController(int playerIndex, IPlatformInfo platformInfo, IControllerDefinition controllerDefinition, IControllerTemplate controllerTemplate, IControllerProfile controllerProfile, IInputTemplate inputTemplate){
             var controllerMappings = controllerProfile.ProfileType == ControllerProfileType.KEYBOARD_PROFILE ?
                controllerTemplate.KeyboardControllerMappings : controllerTemplate.GamepadControllerMappings;
            string deviceName = this.CoreInstance.ControllerPortsDatabase.GetDeviceInPort(platformInfo, playerIndex);
            IList<IInputDevice> devices = new InputManager().GetGamepads();
            if (devices.Count == 0)
            {
                if (deviceName == InputDeviceNames.XInputDevice1)
                {
                    controllerMappings["default"].KeyMappings["JOYPAD_INDEX"] = devices.Where(device => device.XI_IsXInput).Where(device => device.XI_GamepadIndex == 1).First().DeviceIndex.ToString();
                }
                if (deviceName == InputDeviceNames.XInputDevice2)
                {
                    controllerMappings["default"].KeyMappings["JOYPAD_INDEX"] = devices.Where(device => device.XI_IsXInput).Where(device => device.XI_GamepadIndex == 2).First().DeviceIndex.ToString();
                }
                if (deviceName == InputDeviceNames.XInputDevice3)
                {
                    controllerMappings["default"].KeyMappings["JOYPAD_INDEX"] = devices.Where(device => device.XI_IsXInput).Where(device => device.XI_GamepadIndex == 3).First().DeviceIndex.ToString();
                }
                if (deviceName == InputDeviceNames.XInputDevice4)
                {
                    controllerMappings["default"].KeyMappings["JOYPAD_INDEX"] = devices.Where(device => device.XI_IsXInput).Where(device => device.XI_GamepadIndex == 4).First().DeviceIndex.ToString();
                }
                if (devices.Select(device => device.DI_ProductName).Contains(deviceName))
                {
                    controllerMappings["default"].KeyMappings["JOYPAD_INDEX"] = devices.Where(device => device.DI_ProductName == deviceName).First().DeviceIndex.ToString();
                }
            }
            else
            {
                controllerProfile = controllerDefinition.ProfileStore[InputDeviceNames.KeyboardDevice];
                controllerMappings = controllerTemplate.KeyboardControllerMappings;
                playerIndex = 1; //Force Keyboard player 1 if no controllers detected
            }
            return base.CompileController(playerIndex, platformInfo, controllerDefinition, controllerTemplate, controllerProfile, inputTemplate, controllerMappings);
        }
        public override void ShutdownEmulator()
        {
            this.retroArchInstance.CloseMainWindow();
        }
        public override void HandlePrompt(string promptMessage)
        {
            switch (promptMessage)
            {
                case "FAST_FORWARD":
                    this.SendUdp(RANetworkCommands.FAST_FORWARD);
                    break;
                case "FAST_FORWARD_HOLD":
                    this.SendUdp(RANetworkCommands.FAST_FORWARD_HOLD);
                    break;
                case "LOAD_STATE":
                    this.SendUdp(RANetworkCommands.LOAD_STATE);
                    break;
                case "SAVE_STATE":
                    this.SendUdp(RANetworkCommands.SAVE_STATE);
                    break;
                case "FULLSCREEN_TOGGLE":
                    this.SendUdp(RANetworkCommands.FULLSCREEN_TOGGLE);
                    break;
                case "QUIT":
                    this.SendUdp(RANetworkCommands.QUIT);
                    break;
                case "STATE_SLOT_PLUS":
                    this.SendUdp(RANetworkCommands.STATE_SLOT_PLUS);
                    break;
                case "STATE_SLOT_MINUS":
                    this.SendUdp(RANetworkCommands.STATE_SLOT_MINUS);
                    break;
                case "REWIND":
                    this.SendUdp(RANetworkCommands.REWIND);
                    break;
                case "MOVIE_RECORD_TOGGLE":
                    this.SendUdp(RANetworkCommands.MOVIE_RECORD_TOGGLE);
                    break;
                case "PAUSE_TOGGLE":
                    this.SendUdp(RANetworkCommands.PAUSE_TOGGLE);
                    break;
                case "FRAMEADVANCE":
                    this.SendUdp(RANetworkCommands.FRAMEADVANCE);
                    break;
                case "RESET":
                    this.SendUdp(RANetworkCommands.RESET);
                    break;
                case "SHADER_NEXT":
                    this.SendUdp(RANetworkCommands.SHADER_NEXT);
                    break;
                case "SHADER_PREV":
                    this.SendUdp(RANetworkCommands.SHADER_PREV);
                    break;
                case "CHEAT_INDEX_PLUS":
                    this.SendUdp(RANetworkCommands.CHEAT_INDEX_PLUS);
                    break;
                case "CHEAT_INDEX_MINUS":
                    this.SendUdp(RANetworkCommands.CHEAT_INDEX_MINUS);
                    break;
                case "CHEAT_TOGGLE":
                    this.SendUdp(RANetworkCommands.CHEAT_TOGGLE);
                    break;
                case "SCREENSHOT":
                    this.SendUdp(RANetworkCommands.SCREENSHOT);
                    break;
                case "MUTE":
                    this.SendUdp(RANetworkCommands.MUTE);
                    break;
                case "NETPLAY_FLIP":
                    this.SendUdp(RANetworkCommands.NETPLAY_FLIP);
                    break;
                case "SLOWMOTION":
                    this.SendUdp(RANetworkCommands.SLOWMOTION);
                    break;
                case "VOLUME_UP":
                    this.SendUdp(RANetworkCommands.VOLUME_UP);
                    break;
                case "VOLUME_DOWN":
                    this.SendUdp(RANetworkCommands.VOLUME_DOWN);
                    break;
                case "OVERLAY_NEXT":
                    this.SendUdp(RANetworkCommands.OVERLAY_NEXT);
                    break;
                case "DISK_EJECT_TOGGLE":
                    this.SendUdp(RANetworkCommands.DISK_EJECT_TOGGLE);
                    break;
                case "DISK_NEXT":
                    this.SendUdp(RANetworkCommands.DISK_NEXT);
                    break;
                case "DISK_PREV":
                    this.SendUdp(RANetworkCommands.DISK_PREV);
                    break;
                case "GRAB_MOUSE_TOGGLE":
                    this.SendUdp(RANetworkCommands.GRAB_MOUSE_TOGGLE);
                    break;
                case "MENU_TOGGLE":
                    this.SendUdp(RANetworkCommands.MENU_TOGGLE);
                    break;
            }
        }
        internal void SendUdp(int port, string ipAddress, byte[] data)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse(ipAddress);
            IPEndPoint endPoint = new IPEndPoint(serverAddr, port);
            sock.SendTo(data, endPoint);
        }
        internal void SendUdp(int port, string ipAddress, string data)
        {
            this.SendUdp(port, ipAddress, Encoding.ASCII.GetBytes(data));
        }
        internal void SendUdp(string data)
        {
            this.SendUdp(55355, "127.0.0.1", Encoding.ASCII.GetBytes(data));
        }
        
    }
}
