using Celeste.Mod.CelesteArchipelago.Networking;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class CelesteArchipelagoSaveData : EverestModuleSaveData
    {
        public string Name { get; set; } = "";
        public string Server { get; set; } = "";
        public string Port { get; set; } = "";

        public int HashValue()
        {
            return (Name + Server + Port).GetHashCode();
        }

    }
}
