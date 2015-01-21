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
using Snowflake.Game;
using Snowflake.Platform;
using Snowflake.Service;


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

        }
        public override void ShutdownEmulator()
        {

        }
        public override void HandlePrompt(string promptMessage)
        {

        }
    }
}
