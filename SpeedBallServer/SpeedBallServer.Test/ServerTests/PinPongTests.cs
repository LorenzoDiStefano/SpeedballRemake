﻿using System;
using NUnit.Framework;

namespace SpeedBallServer.Test.ServerTests
{
    public class PinPongTests
    {
        private GameServer server;
        private FakeClock clock;
        private FakeTransport transport;
        private FakeEndPoint firstClient;

        [SetUp]
        public void SetUpTest()
        {
            //initializing objects needed for the server
            transport = new FakeTransport();
            clock = new FakeClock();
            transport.Bind("127.0.0.1", 5000);

            //initializing server
            server = new GameServer(transport, clock,2);

            //initializinng client
            firstClient = new FakeEndPoint("192.168.1.1", 5001);

            //initializing join packet
            FakeData packet = new FakeData();
            packet.data = new Packet(PacketsCommands.Join).GetData();
            packet.endPoint = firstClient;

            //sending packet
            transport.ClientEnqueue(packet);

            //server reads join
            server.SingleStep();
        }

        [Test]
        public void ReceivePingPacket()
        {
            byte[] pingPacket = (transport.ClientDequeue()).data;

            Assert.That(pingPacket[0], Is.EqualTo((byte)PacketsCommands.Ping));
        }

        [Test]
        public void ReceiveSamePingPacketAfterTimeout()
        {
            clock.IncreaseTimeStamp(.5f);
            server.SingleStep();

            byte[] pingPacket = (transport.ClientDequeue()).data;
            uint pingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            clock.IncreaseTimeStamp(1f);
            server.SingleStep();

            //dequeueing welcome
            transport.ClientDequeue();

            //dequeueing obstacle spawn packet
            transport.ClientDequeue();

            //dequeueing first player spawn
            transport.ClientDequeue();

            //dequeueing second player spawn
            transport.ClientDequeue();

            //dequeueing first player spawn of the second team
            transport.ClientDequeue();

            byte[] secondPingPacket = (transport.ClientDequeue()).data;
            uint secondPingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            Assert.That(secondPingPacketId, Is.EqualTo(pingPacketId));
        }

        [Test]
        public void ReceiveAPingPacketAfterTimeout()
        {
            clock.IncreaseTimeStamp(.5f);
            server.SingleStep();

            //dequeue first ping packet
            transport.ClientDequeue();

            clock.IncreaseTimeStamp(1f);
            server.SingleStep();

            //dequeueing welcome
            transport.ClientDequeue();

            //dequeueing obstacle spawn packet
            transport.ClientDequeue();

            //dequeueing first player spawn
            transport.ClientDequeue();

            //dequeueing second player spawn
            transport.ClientDequeue();

            //dequeueing first player spawn of the second team
            transport.ClientDequeue();

            //dequeueing updates
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();

            byte[] secondPingPacket = (transport.ClientDequeue()).data;

            Assert.That(secondPingPacket[0], Is.EqualTo((byte)PacketsCommands.Ping));
        }

        [Test]
        public void RespondPingPacketAfterTimeout()
        {
            clock.IncreaseTimeStamp(.5f);
            server.SingleStep();

            byte[] pingPacket = (transport.ClientDequeue()).data;
            uint pingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            clock.IncreaseTimeStamp(1f);
            server.SingleStep();

            //dequeueing welcome
            transport.ClientDequeue();

            //dequeueing obstacle spawn packet
            transport.ClientDequeue();

            //dequeueing first player spawn
            transport.ClientDequeue();

            //dequeueing second player spawn
            transport.ClientDequeue();

            //dequeueing first player spawn of the second team
            transport.ClientDequeue();

            byte[] secondPingPacket = (transport.ClientDequeue()).data;
            uint secondPingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            FakeData pongPacket = new FakeData();
            pongPacket.endPoint = firstClient;
            pongPacket.data = (new Packet(PacketsCommands.Pong, false, pingPacketId)).GetData();

            transport.ClientEnqueue(pongPacket);
            clock.IncreaseTimeStamp(.5f);
            server.SingleStep();

            Assert.That(server.GetClientLastPing(firstClient), Is.EqualTo(.5f));
        }

        [Test]
        public void DefaultClientPing()
        {
            Assert.That(server.GetClientLastPing(firstClient),Is.EqualTo(-1f));
        }

        [Test]
        public void SendPongPacket()
        {
            byte[] pingPacket = (transport.ClientDequeue()).data;

            uint pingPacketId = BitConverter.ToUInt32(pingPacket,1);

            FakeData pongPacket=new FakeData();
            pongPacket.endPoint = firstClient;
            pongPacket.data = (new Packet(PacketsCommands.Pong,false,pingPacketId)).GetData();

            clock.IncreaseTimeStamp(.5f);
            transport.ClientEnqueue(pongPacket);
            clock.IncreaseTimeStamp(.4f);
            server.SingleStep();

            Assert.That(server.GetClientLastPing(firstClient), Is.EqualTo(.9f));
            Assert.That(server.GetClientPingAverage(firstClient), Is.EqualTo(.9f));
        }

        [Test]
        public void CheckPingPacketAfterPong()
        {
            byte[] pingPacket = (transport.ClientDequeue()).data;

            uint pingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            FakeData pongPacket = new FakeData();
            pongPacket.endPoint = firstClient;
            pongPacket.data = (new Packet(PacketsCommands.Pong, false, pingPacketId)).GetData();

            transport.ClientEnqueue(pongPacket);
            clock.IncreaseTimeStamp(.5f);
            server.SingleStep();
            clock.IncreaseTimeStamp(1.5f);
            server.SingleStep();

            //dequeueing welcome
            transport.ClientDequeue();

            //dequeueing obstacle spawn packet
            transport.ClientDequeue();

            //dequeueing first player spawn
            transport.ClientDequeue();

            //dequeueing second player spawn
            transport.ClientDequeue();

            //dequeueing first player spawn of the second team
            transport.ClientDequeue();

            //dequeueing updates
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();

            pingPacket = (transport.ClientDequeue()).data;

            Assert.That(pingPacket[0], Is.EqualTo((byte)PacketsCommands.Ping));
        }

        [Test]
        public void ClientSendingPingPacket()
        {
            FakeData pingPacket = new FakeData();
            pingPacket.endPoint = firstClient;
            pingPacket.data = (new Packet(PacketsCommands.Ping)).GetData();

            transport.ClientEnqueue(pingPacket);

            server.SingleStep();

            //dequeue ping packet
            transport.ClientDequeue();

            byte[] pongPacket = (transport.ClientDequeue()).data;

            Assert.That(pongPacket[0], Is.EqualTo((byte)PacketsCommands.Pong));
        }

        [Test]
        public void ClientSendingPingPacketId()
        {
            FakeData pingPacket = new FakeData();
            pingPacket.endPoint = firstClient;
            pingPacket.data = (new Packet(PacketsCommands.Ping)).GetData();
            uint pingPacketId = BitConverter.ToUInt32(pingPacket.data, 1);

            transport.ClientEnqueue(pingPacket);

            server.SingleStep();

            //dequeue ping packet
            transport.ClientDequeue();

            byte[] pongPacket = (transport.ClientDequeue()).data;
            uint pongResponseId = BitConverter.ToUInt32(pingPacket.data, 1);

            Assert.That(pingPacketId, Is.EqualTo(pongResponseId));
        }

        [Test]
        public void MultipleJoinPings()
        {
            FakeEndPoint secondClient = new FakeEndPoint("192.168.1.2", 5002);

            //initializing join packet
            FakeData packet = new FakeData();
            packet.data = new Packet(PacketsCommands.Join).GetData();
            packet.endPoint = secondClient;

            //sending packet
            transport.ClientEnqueue(packet);

            //server reads join
            server.SingleStep();

            Assert.That(transport.GetSendQueueCount, Is.EqualTo(2));
        }

        [Test]
        public void SendPongPacketCheckAverage()
        {
            byte[] pingPacket = (transport.ClientDequeue()).data;

            uint pingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            FakeData pongPacket = new FakeData();
            pongPacket.endPoint = firstClient;
            pongPacket.data = (new Packet(PacketsCommands.Pong, false, pingPacketId)).GetData();

            clock.IncreaseTimeStamp(2f);
            transport.ClientEnqueue(pongPacket);
            server.SingleStep();

            //dequeue 3 update 4 spawn 1 welcome
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();

            server.SingleStep();

            pingPacket = (transport.ClientDequeue()).data;

            pingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            pongPacket = new FakeData();
            pongPacket.endPoint = firstClient;
            pongPacket.data = (new Packet(PacketsCommands.Pong, false, pingPacketId)).GetData();

            clock.IncreaseTimeStamp(1f);
            transport.ClientEnqueue(pongPacket);

            //dequeue 3 update 4 spawn 1 welcome
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();

            server.SingleStep();

            //dequeue 3 update 4 spawn 1 welcome
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();

            server.SingleStep();

            pingPacket = (transport.ClientDequeue()).data;

            pingPacketId = BitConverter.ToUInt32(pingPacket, 1);

            pongPacket = new FakeData();
            pongPacket.endPoint = firstClient;
            pongPacket.data = (new Packet(PacketsCommands.Pong, false, pingPacketId)).GetData();

            clock.IncreaseTimeStamp(1f);
            transport.ClientEnqueue(pongPacket);

            //dequeue 3 updates
            transport.ClientDequeue();
            transport.ClientDequeue();
            transport.ClientDequeue();

            server.SingleStep();

            Assert.That(server.GetClientLastPing(firstClient), Is.EqualTo(1f));
            Assert.That(server.GetClientPingAverage(firstClient), Is.EqualTo(4f/3f));
        }
    }
}
