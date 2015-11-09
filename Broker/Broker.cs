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

            //System.Console.WriteLine(brokerPort.ToString());

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

            System.Console.WriteLine("Press <enter> to terminate Broker...");
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

        public Dictionary<string, int> getTopicDict()
        {
            return topics;
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

        string myName;
        int myPort;

        PuppetInterface localPuppetMaster;

        //Filtering
        //filteringTable{relation, list of topics to flood there}
        Dictionary<string, TopicsTable> filteringTable = new Dictionary<string, TopicsTable>();

        Dictionary<string, List<SubscriberRequestID>> delegates = new Dictionary<string, List<SubscriberRequestID>>();

        //public event MySubs E;

        Dictionary<int, SubscriberInterface> subscribers = new Dictionary<int,SubscriberInterface>();
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        //List<BrokerInterface> brokers = new List<BrokerInterface>();

        Dictionary<string, BrokerInterface> brokerTreeInterface = new Dictionary<string, BrokerInterface>();
        Dictionary<string, Dictionary<string, int>> brokerTreeIpAndPort = new Dictionary<string, Dictionary<string, int>>();


        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();

        //FIFO
        Dictionary<string, Tuple<int, int>> fifoManager = new Dictionary<string, Tuple<int, int>>();
        Dictionary<string, List<Tuple<int, string>>> fifoQueue = new Dictionary<string, List<Tuple<int, string>>>();

        

        /*
        public string brokerType(string ip, int port)
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
            //Console.WriteLine("WEIRD, maybe the broker isn't registred in the local tree (brokers DC to this broker)");
            return UNKOWN;
        }
        */

        public string sourceType (string ip, int port)
        {
            if (brokerTreeIpAndPort.ContainsKey("sonL"))
            {
                if(brokerTreeIpAndPort["sonL"].ContainsKey(ip))
                {
                    if(brokerTreeIpAndPort["sonL"][ip] == port)
                    {
                        return "sonL";
                    }
                }
            }
            if (brokerTreeIpAndPort.ContainsKey("sonR"))
            {
                if (brokerTreeIpAndPort["sonR"].ContainsKey(ip))
                {
                    if (brokerTreeIpAndPort["sonR"][ip] == port)
                    {
                        return "sonR";
                    }
                }
            }
            if (brokerTreeIpAndPort.ContainsKey("parent"))
            {
                if (brokerTreeIpAndPort["parent"].ContainsKey(ip))
                {
                    if (brokerTreeIpAndPort["parent"][ip] == port)
                    {
                        return "parent";
                    }
                }
            }
            return "publisher";
        }
       

        public bool canFilterFlood(string sourceType, string topic, string relation)
        {
            Console.WriteLine("Inicio do canFilterFlood");

            if (string.Compare(RoutingPolicyType.FILTER, routing) == 0)
            {
                TopicsTable testTable = new TopicsTable();
                
                if (filteringTable.TryGetValue(relation, out testTable))
                {
                    Console.WriteLine("canFilterFlood: access filtering table");
                    //dictionary<string, int>
                    return filteringTable[relation].containsTopic(topic);
                }

                return false;
            }
            return true;
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
            if ((string.Compare(sourceType, BROKER_SONR) != 0) &&
                                    brokerTreeInterface.TryGetValue(BROKER_SONR, out broTest) &&
                                                    canFilterFlood(sourceType, topic, BROKER_SONR))
            {
                broTest.receiveOrderToFlood(topic, message, myName, myPort);
            }
            if ((string.Compare(sourceType, BROKER_SONL) != 0) &&
                                    brokerTreeInterface.TryGetValue(BROKER_SONL, out broTest) &&
                                                    canFilterFlood(sourceType, topic, BROKER_SONL))
            {
                broTest.receiveOrderToFlood(topic, message, myName, myPort);
            }
            if ((string.Compare(sourceType, BROKER_PARENT) != 0) &&
                                    brokerTreeInterface.TryGetValue(BROKER_PARENT, out broTest) &&
                                                    canFilterFlood(sourceType, topic, BROKER_PARENT))
            {
                broTest.receiveOrderToFlood(topic, message, myName, myPort);
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
            string action = "BroEvent - " + myName + " Flooded message on topic " + topic;
            informPuppetMaster(action);
            //Console.WriteLine(action);
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
                fifoQueue[pubPlusTopic].Add(msgNPlusMsg);
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
            //MessageBox.Show(port.ToString());
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

            filterSubscriptionFlood(topic, myName, myPort);


            string action = "BroEvent Added subscriber at port " + port + " for the topic " + topic;
            //informPuppetMaster(action);
            //Console.WriteLine(action);

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

            filterUnsubscriptionFlood(topic);

            string action = "BroEvent Removed subscriber at port " + port + " for the topic " + topic;
            //informPuppetMaster(action);
            //Console.WriteLine(action);


        }
        public void filterSubscriptionFlood(string topic, string ip, int port)
        {
            string relation = sourceType(ip, port);

            BrokerInterface broTest;
            if (string.Compare(RoutingPolicyType.FILTER, routing) == 0)
            {
                if (brokerTreeInterface.TryGetValue(BROKER_SONL, out broTest) && (string.Compare(relation, BROKER_SONL) != 0))
                {
                    broTest.filterSubscription(topic, myName, myPort);
                }
                if (brokerTreeInterface.TryGetValue(BROKER_SONR, out broTest) && (string.Compare(relation, BROKER_SONR) != 0))
                {
                    broTest.filterSubscription(topic, myName, myPort);
                }
                if (brokerTreeInterface.TryGetValue(BROKER_PARENT, out broTest) && (string.Compare(relation, BROKER_PARENT) != 0))
                {
                    broTest.filterSubscription(topic, myName, myPort);
                }
            }
        }


        public void filterUnsubscriptionFlood(string topic)
        {
            BrokerInterface broTest;
            if (string.Compare(RoutingPolicyType.FILTER, routing) == 0)
            {
                if (brokerTreeInterface.TryGetValue(BROKER_SONL, out broTest))
                {
                    broTest.filterUnsubscription(topic, myName, myPort);
                }
                if (brokerTreeInterface.TryGetValue(BROKER_SONR, out broTest))
                {
                    broTest.filterUnsubscription(topic, myName, myPort);
                }
                if (brokerTreeInterface.TryGetValue(BROKER_PARENT, out broTest))
                {
                    broTest.filterUnsubscription(topic, myName, myPort);
                }
            }
        }


        /*
        
            Methods to be executed in Threads

        */


        // flood
        public Thread receiveOrderToFlood(string topic, string message, string ip, int port)
        {
            var t = new Thread(() => RealreceiveOrderToFlood(topic, message, ip, port));
            t.Start();
            return t;
        }

        //used for the PuppetMaster to request a broker to flood a message
        public void RealreceiveOrderToFlood(string topic, string message, string ip, int port)
        {


            // sourceType cases: {publisher, sonL, sonR, parent}
            string source = sourceType(ip, port);


            if (string.Compare(OrderingType.FIFO, ordering) == 0)
            {
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
                        flood(source, topic, message);
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
            else if (string.Compare(OrderingType.TOTAL, ordering) == 0)
            {
                //TODO 
            }
            else
            {
                flood(source, topic, message);
            }

        }

        // Subscription
        public Thread filterSubscription(string topic, string ip, int port)
        {
            var t = new Thread(() => RealfilterSubscription(topic, ip, port));
            t.Start();
            return t;
        }

        public void RealfilterSubscription(string topic, string ip, int port)
        {
            string relation = sourceType(ip, port);
            TopicsTable testTable = new TopicsTable();

            //foreach (string t in filteringTable[relation].getTopicDict().Keys)
            //{
            //    Console.WriteLine("t:  " + t);
            //}

            if (filteringTable.TryGetValue(relation, out testTable))
            {
                if (filteringTable[relation].containsTopic(topic))
                {
                    filteringTable[relation].addSubNumber(topic);
                }
                else
                {
                    filteringTable[relation].AddTopic(topic);
                    filterSubscriptionFlood(topic, myName, myPort);
                }
            }
            else
            {
                filteringTable.Add(relation, testTable);
                filteringTable[relation].AddTopic(topic);
                filterSubscriptionFlood(topic, myName, myPort);
            }
        }

        // Unsubscription

        public Thread filterUnsubscription(string topic, string ip, int port)
        {
            var t = new Thread(() => RealfilterUnsubscription(topic, ip, port));
            t.Start();
            return t;
        }

        private void RealfilterUnsubscription(string topic, string ip , int port)
        {
            string relation = sourceType(ip, port);
            TopicsTable testTable = new TopicsTable();

            if (filteringTable.TryGetValue(relation, out testTable))
            {
                if (filteringTable[relation].containsTopic(topic))
                {

                    if (filteringTable[relation].remSubNumber(topic) == 0)
                    {
                        filterUnsubscriptionFlood(topic);
                    }
                }
            }
        }


        /*
        
            end of thread executed methods
        */


        


        // PuppetMaster envia ordem para o broker para adicionar um subscriber que esta conectado
        public void addSubscriber(int port)
        {
            Console.WriteLine("Subscriber adicionado " + port);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:" + port + "/sub");

            subscribers.Add(port, subscriber);
        }

        public void addPublisher(int port)
        {
            Console.WriteLine("Publisher adicionado " + port);
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
                    brokerTreeInterface.Add("sonL", broker);
                    Dictionary<string, int> ipAndPortL = new Dictionary<string, int>();
                    ipAndPortL.Add(ip, port);
                    brokerTreeIpAndPort.Add("sonL",ipAndPortL);
                    break;
                case "sonR":
                    brokerTreeInterface.Add("sonR", broker);
                    Dictionary<string, int> ipAndPortR = new Dictionary<string, int>();
                    ipAndPortR.Add(ip, port);
                    brokerTreeIpAndPort.Add("sonR", ipAndPortR);
                    break;
                case "parent":
                    brokerTreeInterface.Add("parent", broker);
                    Dictionary<string, int> ipAndPortP = new Dictionary<string, int>();
                    ipAndPortP.Add(ip, port);
                    brokerTreeIpAndPort.Add("sonL", ipAndPortP);
                    break;
            }
        }

        public void status()
        {
            Console.WriteLine("- Status:");
            Console.WriteLine("I'm Alive");
            foreach (int sub in subscribers.Keys)
            {
                Console.WriteLine("Subscribers in port: " + sub);
            }
            Console.WriteLine("Has " + publishers.Count + " Publishers");
            Console.WriteLine("- End of Status.");
        }

        public void registerLocalPuppetMaster(int port)
        {
            PuppetInterface puppetMaster = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), "tcp://localhost:" + port + "/puppet");
            localPuppetMaster = puppetMaster;
            Console.WriteLine("PuppetMasterLocal adicionado " + port);
        }

        private void informPuppetMaster(string action)
        {
            if (string.Compare(logging,LoggingLevelType.FULL)==0)
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

        public void giveInfo(string name, int port)
        {
            myName = name;
            myPort = port;
        }

       
    }
}
