using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;

using SESDADInterfaces;
using System.Runtime.Serialization.Formatters;

namespace SESDAD
{
    class Subscriber
    {
        [STAThread]
        static void Main(string[] args)
        {
            int subscriberPort = Int32.Parse(args[0]);

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = subscriberPort;
            TcpChannel channel = new TcpChannel(props, null, provider);

          //  TcpChannel channel = new TcpChannel(8087);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(SubscriberServices), "sub",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Subscriber...");
            System.Console.ReadLine();
        }
    }


    class SubscriberServices : MarshalByRefObject, SubscriberInterface
    {
        BrokerInterface localBroker;
        PuppetInterface localPuppetMaster;

        /* Policies*/
        string routing;
        string ordering;
        string logging;

        int myPort;

        List<string> subscriptions = new List<string>();
        List<Tuple<string,string>> messages = new List<Tuple<string, string>>();


        //invocado pelo PuppetMaster para subscrever a um topico e informar o local broker
        public void recieveOrderToSubscribe(string topic)
        {
            if (topic == null || topic.Equals(""))
                throw new Exception("topic is empty");
            //adicionar as subscricoes a lista
            subscriptions.Add(topic);

            //informar o local broker que subscreveu
            localBroker.subscribeRequest(topic, myPort);
            string action = "Subscribed to " + topic;
            informPuppetMaster(action);
            Console.WriteLine(action);

        }

        public void recieveOrderToUnSubscribe(string topic)
        {
            //adicionar as subscricoes a lista
            subscriptions.Remove(topic);

            //informar o local broker que subscreveu
            localBroker.unSubscribeRequest(topic, myPort);

            string action = "Unsubscribed to " + topic;
            informPuppetMaster(action);
            Console.WriteLine(action);

        }

        public void Callback(object sender, MessageArgs m)
        {
            Console.WriteLine();
            Console.WriteLine("Recieved " + m.Topic+ ":" + m.Body);
            Console.WriteLine();

            string action = "Recieved " + m.Topic + " : " + m.Body;
            informPuppetMaster(action);
            Console.WriteLine(action);

            messages.Add(new Tuple<string, string>(m.Topic, m.Body));

        }



        //  invocado pelo PuppetMaster para registar o broker local 
        public void registerLocalBroker(int brokerPort) 
        {
            Console.WriteLine("Broker local registado em no Subscriber: " + "tcp://localhost:" + brokerPort + "/broker");
             localBroker =
                (BrokerInterface)Activator.GetObject(
                       typeof(BrokerInterface), "tcp://localhost:" + brokerPort + "/broker");
            
        }

        public void printRecievedMessages()
        {

            foreach (Tuple<string, string> msg in messages) {
                Console.WriteLine("--");
                Console.WriteLine("Topic: {0}", msg.Item1);
                Console.WriteLine("Message: {0}", msg.Item2);
            }
                Console.WriteLine("--");
        }

        public void status()
        {
            throw new NotImplementedException();
        }

        public void registerLocalPuppetMaster(string name, int port)
        {
            Console.WriteLine("PuppetMasterLocal adicionado " + port);
            PuppetInterface puppetMaster = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), "tcp://localhost:" + port + "/pub");
            localPuppetMaster = puppetMaster;
        }

        private void informPuppetMaster(string action)
        {
            if (string.Compare(logging, LoggingLevelType.FULL) == 0)
            {
                localPuppetMaster.informAction(action);
            }
        }

        public void policies(string routing, string ordering, string logging)
        {
            this.routing = routing;
            this.ordering = ordering;
            this.logging = logging;
        }

        public void giveInfo(int port)
        {
            myPort = port;
        }
    }
}

