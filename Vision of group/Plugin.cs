using System.IO;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace RegionDay
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public override string Author => "Lord Diogen";
        public override string Name => "Vision of Groups";

        public Plugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ServerApi.Hooks.NetSendBytes.Register(this, OnSendBytes);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetSendBytes.Deregister(this, OnSendBytes);
            }
            base.Dispose(disposing);
        }

        void OnSendBytes(SendBytesEventArgs e)
        {
            using (var r = new BinaryReader(new MemoryStream(e.Buffer, 0, e.Buffer.Length)))
            {
                r.ReadUInt16();
                var msgID = r.ReadByte();
                var playerID = r.ReadByte(); 

                if (msgID != 4)
                    return;

                var plr = TShock.Players[playerID];
                var hex = plr == null ? "ffffff" : new Color(plr.Group.R, plr.Group.G, plr.Group.B).Hex3();
                if (hex == "ffffff")
                    return;

                using (var data = new MemoryStream())
                {
                    using (var w = new BinaryWriter(data))
                    {
                        w.BaseStream.Position = 2;

                        w.Write(msgID);
                        w.Write(playerID);
                        w.Write(r.ReadBytes(2));
                        r.ReadString();
                        w.Write($"[c/{hex}:{plr.Name.Replace("]", @"\]")}]");
                        w.Write(r.ReadBytes(27));

                        var length = (ushort)w.BaseStream.Position;
                        w.BaseStream.Position = 0;
                        w.Write(length);
                    }
                    SendData(e.Socket, data.ToArray());
                    e.Handled = true;
                }
            }
        }

        void SendData(RemoteClient client, byte[] data)
        {
            if (client == null || !client.IsConnected())
                return;
            try
            {
                client.Socket.AsyncSend(data, 0, data.Length, client.ServerWriteCallBack);
            }
            catch (System.Net.Sockets.SocketException) { }
            catch (IOException) { }
        }
    }
}
