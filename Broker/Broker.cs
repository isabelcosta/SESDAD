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

    public delegate void MySubs(object sender, MessageArgs m);

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

    public class SubscriberRequestID
    {
        private int subID;
        private MySubs subDelegate;

        public SubscriberRequestID(int subID)
        {
            this.subID = subID;
        }
        public int SubID { get { return subID;} }
        
        public MySubs SubDelegate {  get { return subDelegate; } }

        public void addSubscription (MySubs subscription)
        {
            subDelegate += subscription;
        }

    }

    class TopicsTable
    {
        Dictionary<string, int> topics = new Dictionary<string, int>();


        public int addSubNumber(string topic)
        {
            if (topics.ContainsKey(topic))
            {
                topics[topic]++;
                return topics[topic];
            }

            else
            {
                Console.WriteLine("Cant increment topic that doesnt exist");
                return -1;
            }
        }
        public int remSubNumber(string topic)
        {
            if (topics.ContainsKey(topic))
            {
                topics[topic]--;
                return topics[topic];
            }
            else
            {
                Console.WriteLine("Cant decrement topic that doesnt exist");
                return -1;
            }
        }
 
        public bool containsTopic (string topicNew)
        {
            foreach (string topicList in topics.Keys)
            {
                if (topicNew.Contains(topicList))
                {
                    return true;
                }
            }
            return false;
        }
        public void AddTopic(string topic)
        {
            topics.Add(topic, 1);
        }

    }

    class PublisherServices : MarshalByRefObject, BrokerInterface
    {

        const string PUBLISHER = "publisher";
        const string SUBSCRIBER = "subscriber";
        const string BROKER = "broker";

        const string BROKER_SONL = "sonL";
        const string BROKER_SONR = "sonR";
        const string BROKER_PARENT = "parent";

        const string UNKOWN = "unkown";



            //     {relation, list of topics to flood there}
        Dictionary<string, TopicsTable> filteringTable = new Dictionary<string, TopicsTable>();



        Dictionary<string, List<SubscriberRequestID>> delegates = new Dictionary<string, List<SubscriberRequestID>>();

        //public event MySubs E;

        Dictionary<Tuple<string, string>, SubscriberInterface> subscribers = new Dictionary<Tuple<string, string>,SubscriberInterface>();
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        //List<BrokerInterface> brokers = new List<BrokerInterface>();
        Dictionary<string, BrokerInterface> brokerTree = new Dictionary<string, BrokerInterface>();
        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();


        public bool isBroker(object source)
        {
            BrokerInterface broTest;
            try
            {
                broTest = (BrokerInterface)source;
                Console.WriteLine("It's a broker");
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("It's not a broker");
                return false;
            }
        }


        public string brokerType(object source)
        {
            BrokerInterface broTest;

            // check if broker is in the tree (parent, sonL ou sonR) 
            //      and
            //             get the type of broker

            if ((brokerTree.TryGetValue(BROKER_SONL, out broTest)) && BrokerInterface.ReferenceEquals(broTest, brokerTree[BROKER_SONL]))
            {
                return BROKER_SONL;
            }
            else if ((brokerTree.TryGetValue(BROKER_SONR, out broTest)) && BrokerInterface.ReferenceEquals(broTest, brokerTree[BROKER_SONR]))
            {
                return BROKER_SONR;
            }
            else if ((brokerTree.TryGetValue(BROKER_PARENT, out broTest)) && BrokerInterface.ReferenceEquals(broTest, brokerTree[BROKER_PARENT]))
            {
                return BROKER_PARENT;
            }
            // should NEVER get here! (weird behavior)
            Console.WriteLine("WEIRD, maybe the broker isn't registred in the local tree (brokers DC to this broker)");
            return UNKOWN;
        }

        public string getSourceType(object source)
        {

            // if it not a broker it is a publisher
            if (!isBroker(source)) 
            {
                return PUBLISHER;
            }

            return brokerType(source);



        }

        //used for the PuppetMaster to request a broker to flood a message
        public void recieveOrderToFlood(string topic, string message, object source)
        {


            BrokerInterface broTest;
            
            // sourceType cases: {publisher, sonL, sonR, parent}
            string sourceType = getSourceType(source);

            Console.WriteLine("Sender: {0}", sourceType);
            
            /*
            in each if statement with check if:
                    1st - the process that we are testing isn't the source
                    2nd - if not, get the broker from the tree and order him to flood the message
                    therefore,
                            it will only enter the "if statement" if the broker in test wasn't the source of the order to flood
            */


            //                      1st                                                   2nd
            if ((string.Compare(sourceType, BROKER_SONR)!=0) && brokerTree.TryGetValue(BROKER_SONR, out broTest))
            {
                broTest.recieveOrderToFlood(topic, message, this);
            }
            if ((string.Compare(sourceType, BROKER_SONL) != 0) && brokerTree.TryGetValue(BROKER_SONL, out broTest))
            {
                broTest.recieveOrderToFlood(topic, message, this);
            }
            if ((string.Compare(sourceType, BROKER_PARENT) != 0) && brokerTree.TryGetValue(BROKER_PARENT, out broTest))
            {
                broTest.recieveOrderToFlood(topic, message, this);
            }
            

            //callback
            foreach (string subTopic in delegates.Keys)
            {
                    // checks if the TOPIC BEING PUBLISHED is INCLUDED in the TOPIC SUBSCRIBED
                if (topicsMatch(topic, subTopic))
                {
                    foreach (SubscriberRequestID subReqID in delegates[subTopic])
                    {
                        subReqID.SubDelegate(this, new MessageArgs(topic, message));
                    }

                }
            }
            Console.WriteLine();
            Console.WriteLine("Flooded: " + message);
            Console.WriteLine();
        }

        public bool topicsMatch (string topicPub, string topicSub)
        {

            /*
                    compare the topic published with the topic subscribed without the * character
                
            */
            
            if (topicSub.EndsWith("*"))
            {
                topicSub = topicSub.Remove(topicSub.Length - 1);
            }
            return topicPub.Contains(topicSub);
            
            
        }


        public void subscribeRequest(string topic, string subscriberName, int port)
        {

           
            //subscrito ao evento

            Tuple<string, string> nameAndPort = new Tuple<string, string>(subscriberName, port.ToString());

            SubscriberInterface subscriber = subscribers[nameAndPort];
            if (!delegates.ContainsKey(topic))
            {
                delegates.Add(topic, new List<SubscriberRequestID>());

            }

            bool alreadySubscribed= false;
            Console.WriteLine("topic in delegate {0}", delegates[topic]);
            foreach (SubscriberRequestID subReqIDTemp in delegates[topic])
            {
                if (subReqIDTemp.SubID == port)
                {
                    alreadySubscribed = true;
                    break;
                }
            }

            // subscriber has already a subscription of some topic
            if (!alreadySubscribed)
            {
                SubscriberRequestID subReqID = new SubscriberRequestID(port);
                subReqID.addSubscription(new MySubs(subscriber.Callback));
                delegates[topic].Add(subReqID);
            }
           
            Console.WriteLine();
            Console.WriteLine("Added subscriber {1} at port {0} to the topic {2}", port, subscriberName, topic);
            Console.WriteLine();

        }

        public void unSubscribeRequest(string topic, int port)
        {
        
            foreach (SubscriberRequestID subReqIDTemp in delegates[topic])
            {
                if (subReqIDTemp.SubID == port)
                {
                    delegates[topic].Remove(subReqIDTemp);
                    break;
                }
            }

            Console.WriteLine("Removed subscriber at port {0} to the topic {1}", port, topic);


        }

        // PuppetMaster envia ordem para o broker para adicionar um subscriber que esta conectado
        public void addSubscriber(string name, int port)
        {
            Console.WriteLine("Subscriber adicionado " + name + " " + port);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:" + port + "/" + name);
            Tuple<string,string> nameAndPort = new Tuple<string, string>(name, port.ToString());

            subscribers.Add(nameAndPort, subscriber);
        }

        public void addPublisher(string name, int port)
        {
            Console.WriteLine("Publisher adicionado " + name + " " + port );
            PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:" + port + "/" + name);
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

        public void status()
        {
            throw new NotImplementedException();
        }

        public void addPupperMaster(string name, int port)
        {
            throw new NotImplementedException();
        }

        public void policies(string routing, string ordering, string logging)
        {
            throw new NotImplementedException();
        }
    }
}
