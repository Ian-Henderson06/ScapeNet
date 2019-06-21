﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Lidgren.Network;


namespace ScapeNetLib
{
    public class Networker_Client_Unity : INetworker
    {
        NetClient client;
        NetPeerConfiguration config;

        int player_id = -1;
        bool isConnectedToServer = false;

        public void Setup(string network_title)
        {
            player_id = -1;
            isConnectedToServer = false;

            config = new NetPeerConfiguration(network_title);

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.Data);
        }

        public void Setup(string network_title, int port)
        {
            Setup(network_title);
        }

        public void StartClient(string ip, int port, string connection_approval_string)
        {
            client = new NetClient(config);
            client.Start();

            AddDefaultPacketReceives();

            NetOutgoingMessage approval = client.CreateMessage();
            approval.Write(connection_approval_string);
            client.Connect(ip, port, approval);
        }

        public bool IsConnected()
        {
            return isConnectedToServer;
        }

        public int GetPlayerID()
        {
            return player_id;
        }

        private void AddDefaultPacketReceives()
        {
            Packet_Register.Instance.clientPacketReceivedRegister.Add("D_Connection", packetObj => {
                ConnectionPacket connectionPacket = (ConnectionPacket)packetObj[0];

                player_id = connectionPacket.player_id;
                isConnectedToServer = true;

                OnConnectPacket ocp = new OnConnectPacket("D_OnConnect");
                SendPacketToServer(ocp);

                return false;
            });
        }

        public void SendPacketToServer<T>(T packet) where T : Packet<T>
        {
            NetOutgoingMessage msg = client.CreateMessage();

            msg = PacketHelper.AddDefaultInformationToPacketWithId(msg, packet.Get_PacketName(), player_id);
            msg = packet.PackPacketIntoMessage(msg,  packet);
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }


        public void OnReceive(string packet_name, Func<object[], bool> function)
        {
            Packet_Register.Instance.clientPacketReceivedRegister.Add(packet_name, function);
        }

        public void OnConnected()
        {
            ConnectionPacket conPacket = new ConnectionPacket("D_Connection");
            conPacket.player_id = -1;

            SendPacketToServer(conPacket);
        }

        public void Update()
        {

            NetIncomingMessage msg;
            while ((msg = client.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        // Debug.Log(msg.ReadString());
                        break;
                    case NetIncomingMessageType.Data:
                        string packet_name = msg.ReadString();
                        int player_id = msg.ReadInt32();

                        if (Packet_Register.Instance.clientPacketReceivedRegister.ContainsKey(packet_name))
                        {
                            System.Object instance = Activator.CreateInstance(Packet_Register.Instance.packetTypes[packet_name], packet_name);
                            MethodInfo openMethod = Packet_Register.Instance.packetTypes[packet_name].GetMethod("OpenPacketFromMessage");
                            object packet = openMethod.Invoke(instance, new object[] { msg });
                            bool shouldSendBack;
                 
                            shouldSendBack = Packet_Register.Instance.clientPacketReceivedRegister[packet_name].Invoke(new object[] { packet, player_id, msg.SenderConnection });
                         
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        if ((NetConnectionStatus)msg.ReadByte() == NetConnectionStatus.Connected)
                            OnConnected();
                        break;
                    default:
                        //  Debug.Log("Unhandled type: " + msg.MessageType);
                        break;
                }
                client.Recycle(msg);
            }
        }

      
    }
}
