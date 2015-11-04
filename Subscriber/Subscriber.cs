using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = args[0];
            TcpChannel channel = new TcpChannel(props, null, provider);

            //  TcpChannel channel = new TcpChannel(8087);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublisherServices), "Subscriber",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Subscriber...");
            System.Console.ReadLine();
        }
    }


    class PublisherServices : MarshalByRefObject, SubscriberInterface
    {
        BrokerInterface localBroker;
        List<string> subscriptions = new List<string>();
        List<Tuple<string, string>> messages = new List<Tuple<string, string>>();


        //invocado pelo PuppetMaster para subscrever a um topico e informar o local broker
        public void recieveOrderToSubscribe(string topic, string subName, int subPort)
        {
            if (topic == null || topic.Equals(""))
                throw new Exception("topic is empty");
            //adicionar as subscricoes a lista
            subscriptions.Add(topic);

            //informar o local broker que subscreveu
            localBroker.subscribeRequest(topic, subName, subPort);

            Console.WriteLine("Subscribed to: " + topic);
        }
        public void Callback(object sender, MessageArgs m)
        {
            Console.WriteLine("Recieved " + m.Topic + ":" + m.Body);
            messages.Add(new Tuple<string, string>(m.Topic, m.Body));

        }



        //  invocado pelo PuppetMaster para registar o broker local 
        public void registerLocalBroker(string brokerName, int brokerPort)
        {
            Console.WriteLine("Broker local registado em no Subscriber: " + "tcp://localhost:" + brokerPort + "/" + brokerName);
            localBroker =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + brokerPort + "/" + brokerName);

        }


        public void printRecievedMessages()
        {

            foreach (Tuple<string, string> msg in messages)
            {
                Console.WriteLine("Topic " + msg.Item1 + " - " + msg.Item2);

            }
        }

    }
}
