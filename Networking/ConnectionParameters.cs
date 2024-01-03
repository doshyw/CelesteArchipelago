using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ConnectionParameters
    {
        public string game;
        public string server;
        public string port;
        public string name;
        public ItemsHandlingFlags flags;
        public Version version;
        public string[] tags;
        public string uuid;
        public string password;
        public bool slotData;

        public ConnectionParameters(string game, string server, string port, string name, ItemsHandlingFlags flags, Version version = null, string[] tags = null, string uuid = null, string password = null, bool slotData = true)
        {
            this.game = game;
            this.server = server;
            this.port = port;
            this.name = name;
            this.flags = flags;
            this.version = version;
            this.tags = tags;
            this.uuid = uuid;
            this.password = password;
            this.slotData = slotData;
        }

        public Task<LoginResult> DoLogin(ArchipelagoSession session)
        {
            return session.LoginAsync(
                game,
                name,
                flags,
                version,
                tags,
                uuid,
                password,
                slotData
            );
        }
    }
}
