using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

using SESDADInterfaces;
using System.Collections;
using System.Runtime.Serialization.Formatters;

namespace SESDAD
{
    class Publisher
    {
        [STAThread]

        static void Main(string[] args)
        {

            string publisherName = args[0];
            int publisherPort = Int32.Parse(args[1]);


            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = publisherPort;
            TcpChannel channel = new TcpChannel(props, null, provider);


            //TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublisherServices), publisherName,
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Publisher...");
            System.Console.ReadLine();
        }
    }


    class PublisherServices: MarshalByRefObject, PublisherInterface
    {

        BrokerInterface localBroker;

        public void addPupperMaster(string name, int port)
        {
            throw new NotImplementedException();
        }

        public void policies(string routing, string ordering, string logging)
        {
            throw new NotImplementedException();
        }

        public void recieveOrderToPublish(string topic, string namePlusSeqN, int numberOfEvents, int interval_x_ms)
        {
            // Formato da mensagem : PubName SeqNumber/Total
            
            /*
            for (int i = 0; i < numberOfEvents; i++)
            {
            */
                
                localBroker.recieveOrderToFlood(topic, namePlusSeqN + "/" + numberOfEvents, this);
                Console.WriteLine();
                Console.WriteLine(topic+ ":"+ namePlusSeqN);
                Console.WriteLine();

                Thread.Sleep(interval_x_ms);
            //}
        }

        public void registerLocalBroker(string BrokerName, int BrokerPort)
        {
            Console.WriteLine("Broker local registado no Publisher: " + "tcp://localhost:" + BrokerPort + "/" + BrokerName);
            localBroker =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + BrokerPort + "/" + BrokerName);
        }

        public void status()
        {
            throw new NotImplementedException();
        }
    }


}
