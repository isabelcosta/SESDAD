﻿using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

using SESDADInterfaces;

namespace SESDAD
{
    class Publisher
    {
        [STAThread]

        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublisherServices), "Publisher",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Publisher...");
            System.Console.ReadLine();
        }
    }


    class PublisherServices: MarshalByRefObject, PublisherInterface
    {

        BrokerInterface localBroker;
        public void recieveOrderToPublish(string topic, string message)
        {
           // while(true)
            //{
                localBroker.recieveOrderToFlood(topic, message);
           
                Console.WriteLine(topic+ ":"+ message);
                //Thread.Sleep(4000);
            //}
        }

        public void registerLocalBroker(string BrokerName, int BrokerPort)
        {
            Console.WriteLine("Broker local registado no Publisher: " + "tcp://localhost:" + BrokerPort + "/" + BrokerName);
            localBroker =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + BrokerPort + "/" + BrokerName);
        }
    }


}
