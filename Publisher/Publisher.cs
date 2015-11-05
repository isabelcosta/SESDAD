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

            int publisherPort = Int32.Parse(args[0]);


            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = publisherPort;
            TcpChannel channel = new TcpChannel(props, null, provider);


            //TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublisherServices), "pub",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Publisher...");
            System.Console.ReadLine();
        }
    }


    class PublisherServices : MarshalByRefObject, PublisherInterface
    {

        BrokerInterface localBroker;
        string name;
        /*
        message
            */
        string topic;
        int numberOfEvents;
        int interval_x_ms;



        public void addPupperMaster(string name, int port)
        {
            throw new NotImplementedException();
        }

        public void policies(string routing, string ordering, string logging)
        {
            throw new NotImplementedException();
        }

        public void recieveOrderToPublish(string topic, int numberOfEvents, int interval_x_ms)
        {
            // Formato da mensagem : PubName SeqNumber/Total
            this.numberOfEvents = numberOfEvents;
            this.topic = topic;
            this.interval_x_ms = interval_x_ms;

            ThreadStart ts = new ThreadStart(this.publish);
            Thread t = new Thread(ts);
            t.Start();


        }

        public void publish()
        {
            int numOfEv = this.numberOfEvents;
            int intv_x_ms = this.interval_x_ms;
            string topicLocal = this.topic;
            string content;

            for (int i = 1; i <= numOfEv; i++)
            {
                content = this.name + " " + i + "/" + numOfEv;

                // Exe: Publisher1 1/10
                localBroker.recieveOrderToFlood(topicLocal, content, this);

                Console.WriteLine();
                Console.WriteLine(topicLocal + " : " + content);
                Console.WriteLine();

                Thread.Sleep(intv_x_ms);
            }

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

        public void giveName(string name)
        {
            this.name = name;
        }
    }


}
