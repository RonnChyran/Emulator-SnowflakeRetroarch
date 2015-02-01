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
        private static readonly string retroArchInputTemplateTemplate = "RetroArchInput";

        public override void StartRom(IGameInfo game)
        {
            var emulatorPath =
                Path.Combine(this.CoreInstance.EmulatorManager.GetAssemblyDirectory(this.EmulatorAssembly), this.EmulatorAssembly.MainAssembly);
            
            var configProfile = this.ConfigurationTemplates[retroArchConfigTemplate].ConfigurationStore.GetConfigurationProfile(game);
            configProfile.ConfigurationValues["input_autodetect_enable"] = false; //Force no autodetect
            var retroArchCfg = this.CompileConfiguration(this.ConfigurationTemplates[retroArchConfigTemplate], configProfile);
            var controller1 = this.CompileController(1, this.CoreInstance.LoadedPlatforms[StonePlatforms.NINTENDO_SNES], this.InputTemplates[retroArchInputTemplateTemplate]);
            var controller2 = this.CompileController(2, this.CoreInstance.LoadedPlatforms[StonePlatforms.NINTENDO_SNES], this.InputTemplates[retroArchInputTemplateTemplate]);
            

            File.WriteAllText(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"), retroArchCfg);
            File.AppendAllText(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"), Environment.NewLine + controller1);
            File.AppendAllText(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"), Environment.NewLine + controller2);
            var startInfo = new ProcessStartInfo(emulatorPath);
            startInfo.WorkingDirectory = Path.Combine(this.CoreInstance.EmulatorManager.GetAssemblyDirectory(this.EmulatorAssembly));
            startInfo.Arguments = String.Format(@"{0} --libretro ""cores/bsnes_balanced_libretro.dll"" --config retroarch.cfg.clean --appendconfig ""{1}""",
                game.FileName, Path.GetFullPath(Path.Combine(this.PluginDataPath, "retroarch.tmp.cfg"))
              );
            Console.WriteLine(startInfo.Arguments);
            
            Process.Start(startInfo);
        }

        
        public override string CompileController(int playerIndex, IPlatformInfo platformInfo, IControllerDefinition controllerDefinition, IControllerTemplate controllerTemplate, IControllerProfile controllerProfile, IInputTemplate inputTemplate){
             var controllerMappings = controllerProfile.ProfileType == ControllerProfileType.KEYBOARD_PROFILE ?
                controllerTemplate.KeyboardControllerMappings : controllerTemplate.GamepadControllerMappings;
            string deviceName = this.CoreInstance.ControllerPortsDatabase.GetDeviceInPort(platformInfo, playerIndex);
            IList<IInputDevice> devices = new InputManager().GetGamepads();
            if(deviceName == InputDeviceNames.XInputDevice1){
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

            return base.CompileController(playerIndex, platformInfo, controllerDefinition, controllerTemplate, controllerProfile, inputTemplate, controllerMappings);
        }
        public override void ShutdownEmulator()
        {

        }
        public override void HandlePrompt(string promptMessage)
        {

        }
    }
}
