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

            int brokerPort = Int32.Parse(args[0]);

            

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = brokerPort;
            TcpChannel channel = new TcpChannel(props, null, provider);


            // TcpChannel channel = new TcpChannel(8088);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(BrokerServices), "broker",
                WellKnownObjectMode.Singleton);

            System.Console.ReadLine();
        }
    }




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

    class BrokerServices : MarshalByRefObject, BrokerInterface
    {

        const string PUBLISHER = "publisher";
        const string SUBSCRIBER = "subscriber";
        const string BROKER = "broker";

        const string BROKER_SONL = "sonL";
        const string BROKER_SONR = "sonR";
        const string BROKER_PARENT = "parent";

        const string UNKOWN = "unkown";

        /* Policies*/
        string routing;
        string ordering;
        string logging;

        PuppetInterface localPuppetMaster;
            //     {relation, list of topics to flood there}
        Dictionary<string, TopicsTable> filteringTable = new Dictionary<string, TopicsTable>();

        Dictionary<string, List<SubscriberRequestID>> delegates = new Dictionary<string, List<SubscriberRequestID>>();

        //public event MySubs E;

        Dictionary<int, SubscriberInterface> subscribers = new Dictionary<int,SubscriberInterface>();
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        //List<BrokerInterface> brokers = new List<BrokerInterface>();
        Dictionary<string, BrokerInterface> brokerTree = new Dictionary<string, BrokerInterface>();
        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();

        Dictionary<string, Tuple<int, int>> fifoManager = new Dictionary<string, Tuple<int, int>>();
        Dictionary<string, List<Tuple<int, string>>> fifoQueue = new Dictionary<string, List<Tuple<int, string>>>();

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


            //START
            //     ORDERING FIFO
            //
            string pubName = "";
            Tuple<int, int> msg = new Tuple<int, int>(0, 0);

            parseMessage(ref pubName, ref msg, message);


            Tuple<int, int> msgMngmt = new Tuple<int, int>(0, 0);


            if (checkIfIsNext(msg, pubName, topic))
            {
                do
                {
                    flood(sourceType, topic, message);
                    if (getFromQueue(pubName + topic, msg.Item1, ref message))
                    {
                        break;
                    }
                } while (true);
            }
            else
            {
                // Just add to queue
                addToQueue(pubName + topic, msg.Item1, message);
            }

        }

        public void flood (string sourceType, string topic, string message)
        {

            BrokerInterface broTest;

            /*
            in each if statement with check if:
                    1st - the process that we are testing isn't the source
                    2nd - if not, get the broker from the tree and order him to flood the message
                    therefore,
                            it will only enter the "if statement" if the broker in test wasn't the source of the order to flood
            */


            //                      1st                                                   2nd
            if ((string.Compare(sourceType, BROKER_SONR) != 0) && brokerTree.TryGetValue(BROKER_SONR, out broTest))
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
            string action = "Flooded message on topic " + topic;
            informPuppetMaster(action);
            Console.WriteLine(action);
        }
        

        public bool checkIfIsNext(Tuple<int, int> msg, string pubName, string topic)
        {


            Tuple<int, int> msgMngmt = new Tuple<int, int>(0, 0);
            if (fifoManager.TryGetValue(pubName + topic, out msgMngmt))
            {
                // Key was in dictionary; "value" contains corresponding value

                if (fifoManager[pubName + topic].Item1 + 1 == msg.Item1)
                {
                    fifoManager[pubName + topic] = msg;
                    return true;
                }
            }


            if (msg.Item1 == 1 )
            {
                fifoManager.Add(pubName + topic, msg);
                return true;
            }
            return false;
        }
        
        public void parseMessage(ref string pubName, ref Tuple<int, int> msg, string message)
        {
            string[] msgParsed = new string[3];

            string[] msgTemp1 = message.Split(' ');
            msgParsed[0] = msgTemp1[0];
            string[] msgTemp2 = msgTemp1[1].Split('/');
            msgParsed[1] = msgTemp2[0];
            msgParsed[2] = msgTemp2[1];

            pubName = msgParsed[0];
            msg = new Tuple<int, int>(int.Parse(msgParsed[1]), int.Parse(msgParsed[2]));
        }

        public void addToQueue(string pubPlusTopic, int msgNumber, string message)
        {

            List<Tuple<int, string>> msgList = new List<Tuple<int, string>>();
            Tuple<int, string> msgNPlusMsg = new Tuple<int, string>(msgNumber, message);
            if (!fifoQueue.TryGetValue(pubPlusTopic, out msgList))
            {
                // Key wasn't in dictionary; "value" is now 0
                msgList.Add(msgNPlusMsg);
                fifoQueue.Add(pubPlusTopic, msgList);
            }
            else
            {
                // Key was in dictionary; "value" contains corresponding value
                msgList.Add(msgNPlusMsg);
            }
        }

        public bool getFromQueue(string pubPlusTopic, int msgNumber, ref string message)
        {
            List<Tuple<int, string>> msgList = new List<Tuple<int, string>>();
            Tuple<int, string> msgNPlusMsg = new Tuple<int, string>(msgNumber, message);
            if (fifoQueue.TryGetValue(pubPlusTopic, out msgList))
            {
                // Key was in dictionary; "value" contains corresponding value
                foreach (Tuple<int, string> currentMsg in msgList)
                {
                    if (currentMsg.Item1 == msgNumber + 1)
                    {
                        message = currentMsg.Item2;
                        msgList.Remove(currentMsg);
                        if (msgList.Count == 0)
                        {
                            fifoQueue.Remove(pubPlusTopic);
                        }
                        return false;
                    }
                }
            }
            return true;
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


        public void subscribeRequest(string topic, int port)
        {

           
            //subscrito ao evento
            SubscriberInterface subscriber = subscribers[port];
            if (!delegates.ContainsKey(topic))
            {
                delegates.Add(topic, new List<SubscriberRequestID>());

            }

            bool alreadySubscribed= false;
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
           
            string action = "Added subscriber at port " + port + " for the topic " + topic;
            informPuppetMaster(action);
            Console.WriteLine(action);

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
            string action = "Removed subscriber at port " + port + " for the topic " + topic;
            informPuppetMaster(action);
            Console.WriteLine(action);


        }

        // PuppetMaster envia ordem para o broker para adicionar um subscriber que esta conectado
        public void addSubscriber(int port)
        {
            Console.WriteLine("Subscriber adicionado " + port);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:" + port + "/sub");

            subscribers.Add(port, subscriber);
        }

        public void addPublisher(int port)
        {
            Console.WriteLine("Publisher adicionado " + port );
            PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:" + port + "/pub");
            publishers.Add(publisher);
        }

        public void addBroker(int port, string ip, string relation)
        {
            Console.WriteLine("Broker adicionado " + port);
            BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://" + ip + ":" + port + "/broker");

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

        public void registerLocalPuppetMaster(string name, int port)
        {
            Console.WriteLine("PuppetMasterLocal adicionado " + port);
            PuppetInterface puppetMaster = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), "tcp://localhost:" + port + "/pub");
            localPuppetMaster = puppetMaster;
        }

        private void informPuppetMaster(string action)
        {
            if(string.Compare(logging,LoggingLevelType.FULL)==0)
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
    }
}
