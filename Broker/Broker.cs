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

    class Broker
    {
        [STAThread]             
        static void Main(string[] args)
        {
            

            string brokerName = args[0];
            int brokerPort = Int32.Parse(args[1]);

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = brokerPort;
            TcpChannel channel = new TcpChannel(props, null, provider);


            // TcpChannel channel = new TcpChannel(8088);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublisherServices), brokerName,
                WellKnownObjectMode.Singleton);



            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }
    }

 
    /*
    public class Subscribed
    {
        public void Callback(object sender, MessageArgs m)
        {
            Console.WriteLine("Fired {0} : {1}", m.Topic, m.Body);
        }

    }
    */

    class PublisherServices : MarshalByRefObject, BrokerInterface
    {

        public delegate void MySubs(object sender, MessageArgs m);

        public Dictionary<string, MySubs> delegates = new Dictionary<string, MySubs>();

        //public event MySubs E;


        
        Dictionary<Tuple<string, string>, SubscriberInterface> subscribers = new Dictionary<Tuple<string, string>,SubscriberInterface>();
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        //List<BrokerInterface> brokers = new List<BrokerInterface>();
        Dictionary<string, BrokerInterface> brokerTree = new Dictionary<string, BrokerInterface>();
        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();



        //used for the PuppetMaster to request a broker to flood a message
        public void recieveOrderToFlood(string topic, string message, object source)
        {


            BrokerInterface broTest;
            bool isBroker = true;
            Console.WriteLine("Is sonL: " + (brokerTree.TryGetValue("sonL", out broTest)));
            Console.WriteLine("Is sonR: " + (brokerTree.TryGetValue("sonR", out broTest)));
            Console.WriteLine("Is parent: " + (brokerTree.TryGetValue("parent", out broTest)));

            try
            {
                Console.WriteLine("Is broker "+brokerTree.ContainsValue(((BrokerInterface)source)));
            }
            catch (Exception)
            {
                isBroker = false;
                Console.WriteLine("Is not a broker");
            }

            
            
            if (!isBroker)
            {
                Console.WriteLine("Is publisher: " + publishers.Contains((PublisherInterface)source));
                if (brokerTree.TryGetValue("sonR", out broTest))
                {
                    broTest.recieveOrderToFlood(topic, message, this);
                }
                if (brokerTree.TryGetValue("parent", out broTest))
                {
                    broTest.recieveOrderToFlood(topic, message, this);
                }
                if (brokerTree.TryGetValue("sonL", out broTest))
                {
                    broTest.recieveOrderToFlood(topic, message, this);
                }
            }
            
            if (isBroker) {

                if ((brokerTree.TryGetValue("sonL", out broTest)) && BrokerInterface.ReferenceEquals(broTest, brokerTree["sonL"]))
                {
                    if (brokerTree.TryGetValue("sonR", out broTest))
                    {
                        broTest.recieveOrderToFlood(topic, message, this);
                    }
                    if (brokerTree.TryGetValue("parent", out broTest))
                    {
                        broTest.recieveOrderToFlood(topic, message, this);
                    }

                } else if ((brokerTree.TryGetValue("sonR", out broTest)) && BrokerInterface.ReferenceEquals(broTest, brokerTree["sonR"]))
                {
                    if (brokerTree.TryGetValue("sonL", out broTest))
                    {
                        broTest.recieveOrderToFlood(topic, message, this);
                    }
                    if (brokerTree.TryGetValue("parent", out broTest))
                    {
                        broTest.recieveOrderToFlood(topic, message, this);
                    }

                } else if ((brokerTree.TryGetValue("parent", out broTest)) && BrokerInterface.ReferenceEquals(broTest, brokerTree["parent"]))
                {
                    if (brokerTree.TryGetValue("sonR", out broTest))
                    {
                        broTest.recieveOrderToFlood(topic, message, this);
                    }
                    if (brokerTree.TryGetValue("sonL", out broTest))
                    {
                        broTest.recieveOrderToFlood(topic, message, this);
                    }

                }
        }

            //callback
            foreach (string subTopic in delegates.Keys)
            {
                if (String.Compare(subTopic,topic)==0)
                {
                    delegates[subTopic](this, new MessageArgs(topic, message));

                }
            }
            Console.WriteLine();
            Console.WriteLine("Flooded: " + message);
            Console.WriteLine();
        }

        public void subscribeRequest(string topic, string subscriberName, int port)
        {

           
            //subscrito ao evento

            Tuple<string, string> nameAndPort = new Tuple<string, string>(subscriberName, port.ToString());

            SubscriberInterface subscriber = subscribers[nameAndPort];
            if (!delegates.ContainsKey(topic))
                delegates.Add(topic, null);
            delegates[topic] += new MySubs(subscriber.Callback);
            Console.WriteLine();
            Console.WriteLine("Added subscriber {1} at port {0} to the topic {2}", port, subscriberName, topic);
            Console.WriteLine();

        }

        public void unSubscribeRequest(string topic, string subscriberName, int port)
        {


            //subscrito ao evento

            Tuple<string, string> nameAndPort = new Tuple<string, string>(subscriberName, port.ToString());

            SubscriberInterface subscriber = subscribers[nameAndPort];
            if (!delegates.ContainsKey(topic))
                delegates.Add(topic, null);
            delegates[topic] -= new MySubs(subscriber.Callback);
            Console.WriteLine("Added subscriber {1} at port {0} to the topic {2}", port, subscriberName, topic);


        }

        // PuppetMaster envia ordem para o broker para adicionar um subscriber que esta conectado
        public void addSubscriber(string name, int port)
        {
            Console.WriteLine("Subscriber adicionado " + name + " " + port);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:" + port + "/" + name);
            //subscriber.registerLocalBroker(name, port);
            Tuple<string,string> nameAndPort = new Tuple<string, string>(name, port.ToString());

            subscribers.Add(nameAndPort, subscriber);
        }

        public void addPublisher(string name, int port)
        {
            Console.WriteLine("Publisher adicionado " + name + " " + port );
            PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:" + port + "/" + name);
            //publisher.registerLocalBroker(name, port);
            publishers.Add(publisher);
        }

        public void addBroker(string name, int port, string relation)
        {
            Console.WriteLine("Broker adicionado " + name + " " + port);
            BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://localhost:" + port + "/" + name);

            switch (relation)
            {
                case "sonL":
                    brokerTree.Add("sonL", broker);
                    break;
                case "sonR":
                    brokerTree.Add("sonR", broker);
                    break;
                case "parent":
                    brokerTree.Add("parent", broker);
                    break;
            }
        }
    }
}
