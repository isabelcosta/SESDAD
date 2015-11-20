﻿using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SESDADInterfaces;
using System.Runtime.Serialization.Formatters;
// ReSharper disable InconsistentNaming

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
        ConcurrentDictionary<string, int> topics = new ConcurrentDictionary<string, int>();


        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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
 
        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddTopic(string topic)
        {
            topics.TryAdd(topic, 1);
        }

        
        public ConcurrentDictionary<string, int> getTopicDict()
        {
            return this.topics;
        }
        
    }
    [Serializable]
    class BrokerServices : MarshalByRefObject, BrokerInterface
    {

        /* Policies*/
        string routing;
        string ordering;
        string logging;

        int myPort;
        string myIp;

        PuppetInterface localPuppetMaster;

        //Filtering
        //filteringTable{relation, list of topics to flood there}
        ConcurrentDictionary<string, TopicsTable> filteringTable = new ConcurrentDictionary<string, TopicsTable>();
        
        Dictionary<string, List<SubscriberRequestID>> delegates = new Dictionary<string, List<SubscriberRequestID>>();

        //public event MySubs E;

        Dictionary<int, SubscriberInterface> subscribers = new Dictionary<int,SubscriberInterface>();
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        //List<BrokerInterface> brokers = new List<BrokerInterface>();

        Dictionary<string, BrokerInterface> brokerTreeInterface = new Dictionary<string, BrokerInterface>();
        Dictionary<string, Tuple<string, int>> brokerTreeIpAndPort = new Dictionary<string, Tuple<string, int>>();


        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();

        //FIFO
        Dictionary<string, Tuple<int, int>> fifoManager = new Dictionary<string, Tuple<int, int>>();
        Dictionary<string, List<Tuple<int, string>>> fifoQueue = new Dictionary<string, List<Tuple<int, string>>>();

        

        /*
        
            Shared Objects
            
                - localPuppetMaster
                - filteringTable
                - delegates
                - subscribers
                - publishers
                - brokerTreeInterface
                - brokerTreeIpAndPort
                - subscribersTopics
                - fifoManager
                - fifoQueue
                



            */

        public string sourceType (string ip, int port)
        {

            lock (brokerTreeIpAndPort)
            {
                Tuple<string, int> ipAndPort = new Tuple<string, int>(ip, port);

                if (brokerTreeIpAndPort.ContainsKey(BrokerNeighbours.SONL))
                {
                    if (brokerTreeIpAndPort[BrokerNeighbours.SONL].Item2 == port)
                    {
                        return BrokerNeighbours.SONL;
                    }
                }
                if (brokerTreeIpAndPort.ContainsKey(BrokerNeighbours.SONR))
                {
                    if (brokerTreeIpAndPort[BrokerNeighbours.SONR].Item2 == port)
                    {
                        return BrokerNeighbours.SONR;
                    }
                }
                if (brokerTreeIpAndPort.ContainsKey(BrokerNeighbours.PARENT))
                {
                    if (brokerTreeIpAndPort[BrokerNeighbours.PARENT].Item2 == port)
                    {
                        return BrokerNeighbours.PARENT;
                    }
                }
            }
            return "publisher";
        }


        public bool canFilterFlood(string sourceType, string topic, string relation)
        {

            if (String.CompareOrdinal(RoutingPolicyType.FILTER, routing) == 0)
            {
                TopicsTable testTable = new TopicsTable();
                lock (filteringTable)
                {
                    if (filteringTable.TryGetValue(relation, out testTable))
                    {
                        //dictionary<string, int>
                        return filteringTable[relation].containsTopic(topic);
                    }
                    
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

            lock (brokerTreeInterface)
            {
                if ((String.CompareOrdinal(sourceType, BrokerNeighbours.SONR) != 0) &&
                                    brokerTreeInterface.TryGetValue(BrokerNeighbours.SONR, out broTest) &&
                                                        canFilterFlood(sourceType, topic, BrokerNeighbours.SONR))
                {
                    //lock broker??
                    brokerTreeInterface[BrokerNeighbours.SONR].receiveOrderToFlood(topic, message, myIp, myPort);
                }
                if ((String.CompareOrdinal(sourceType, BrokerNeighbours.SONL) != 0) &&
                                    brokerTreeInterface.TryGetValue(BrokerNeighbours.SONL, out broTest) &&
                                                        canFilterFlood(sourceType, topic, BrokerNeighbours.SONL))
                {
                    //lock broker??
                    brokerTreeInterface[BrokerNeighbours.SONL].receiveOrderToFlood(topic, message, myIp, myPort);
                }
                if ((String.CompareOrdinal(sourceType, BrokerNeighbours.PARENT) != 0) &&
                                    brokerTreeInterface.TryGetValue(BrokerNeighbours.PARENT, out broTest) &&
                                                        canFilterFlood(sourceType, topic, BrokerNeighbours.PARENT))
                {
                    //lock broker??
                    brokerTreeInterface[BrokerNeighbours.PARENT].receiveOrderToFlood(topic, message, myIp, myPort);
                }
            }

            lock (delegates)
            {
                
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
            }
            string action = "BroEvent - " + myIp + " Flooded message on topic " + topic;
            informPuppetMaster(action);
            //Console.WriteLine(action);
        }
        

        public bool checkIfIsNext(Tuple<int, int> msg, string pubName, string topic)
        {
            Tuple<int, int> msgMngmt = new Tuple<int, int>(0, 0);
            lock (fifoManager)
            {
                if (fifoManager.TryGetValue(pubName + topic, out msgMngmt))
                {
                    // Key was in dictionary; "value" contains corresponding value

                    if (fifoManager[pubName + topic].Item1 + 1 == msg.Item1)
                    {
                        fifoManager[pubName + topic] = msg;
                        return true;
                    }
                }
            }


            if (msg.Item1 == 1 )
            {

                /*
                if(fifoManager.ContainsKey(pubName+topic) && fifoQueue(pubName+topic)-> tiver enviado todas as mensagens (exemplo 12/12)) {

                    fifoManager.Remove(pubName+topic);
                    &&
                    limpar a fifoQueue(pubName+topic)?

                }
                */
                lock (fifoManager)
                {
                    
                    if (fifoManager.ContainsKey(pubName + topic))
                    {
                        fifoManager.Remove(pubName + topic);
                        if(fifoQueue.ContainsKey(pubName + topic))
                        {
                            fifoQueue.Remove(pubName + topic);
                        }
                    }
                    fifoManager.Add(pubName + topic, msg);
                }
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

            lock (fifoQueue)
            {
                
                if (!fifoQueue.TryGetValue(pubPlusTopic, out msgList))
                {

                    msgList = new List<Tuple<int, string>>();
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
        }
        public bool getFromQueue(string pubPlusTopic, int msgNumber, ref string message)

        {
            List<Tuple<int, string>> msgList = new List<Tuple<int, string>>();
            Tuple<int, string> msgNPlusMsg = new Tuple<int, string>(msgNumber, message);
            lock (fifoQueue)
            {
                if (fifoQueue.TryGetValue(pubPlusTopic, out msgList))
                {
                    // Key was in dictionary; "value" contains corresponding value
                    foreach (Tuple<int, string> currentMsg in fifoQueue[pubPlusTopic])
                    {
                        if (currentMsg.Item1 == msgNumber + 1)
                        {
                            message = currentMsg.Item2;
                            //msgList.Remove(currentMsg);
                            fifoQueue[pubPlusTopic].Remove(currentMsg);
                            if (fifoQueue[pubPlusTopic].Count == 0)
                            {
                                fifoQueue.Remove(pubPlusTopic);
                            }
                            return false;
                        }
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
            SubscriberInterface subscriber;

            lock (subscribers)
            {
                subscriber = subscribers[port];
            }

            lock (delegates)
            {
                if (!delegates.ContainsKey(topic))
                {
                    delegates.Add(topic, new List<SubscriberRequestID>());
                }

                bool alreadySubscribed = false;
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
            }

            filterSubscriptionFlood(topic, myIp, myPort);


            //string action = "BroEvent Added subscriber at port " + port + " for the topic " + topic;
            //informPuppetMaster(action);
            //Console.WriteLine(action);

        }



        public void unSubscribeRequest(string topic, int port)
        {
            lock (delegates)
            {
                foreach (SubscriberRequestID subReqIDTemp in delegates[topic])
                {
                    if (subReqIDTemp.SubID == port)
                    {
                        delegates[topic].Remove(subReqIDTemp);
                        break;
                    }
                }
            }

            filterUnsubscriptionFlood(topic, port);

            string action = "BroEvent Removed subscriber at port " + port + " for the topic " + topic;
            //informPuppetMaster(action);
            //Console.WriteLine(action);
        }

        public void filterSubscriptionFlood(string topic, string ip, int port)
        {
            string relation = sourceType(ip, port);
            BrokerInterface broTest;
            Console.WriteLine("FSF " + relation);
            lock (brokerTreeInterface)
            {
                if (String.CompareOrdinal(RoutingPolicyType.FILTER, routing) == 0)
                {
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONL, out broTest) && (String.CompareOrdinal(relation, BrokerNeighbours.SONL) != 0))
                    {
                        //if (myPort == 3333)
                        //{
                        Console.WriteLine("");
                        Console.WriteLine("myPort {0} and myIp {1}", myPort, myIp);
                        Console.WriteLine("sent to SONL " + topic);
                        Console.WriteLine("");
                        //}
                        brokerTreeInterface[BrokerNeighbours.SONL].filterSubscription(topic, ip, port);
                    }
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONR, out broTest) && (String.CompareOrdinal(relation, BrokerNeighbours.SONR) != 0))
                    {
                        //if (myPort == 3333)
                        //{
                        Console.WriteLine("");
                        Console.WriteLine("myPort {0} and myIp {1}", myPort, myIp);
                        Console.WriteLine("sent to SONR " + topic);
                        Console.WriteLine("");
                        //}
                        brokerTreeInterface[BrokerNeighbours.SONR].filterSubscription(topic, ip, port);
                    }
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.PARENT, out broTest) && (String.CompareOrdinal(relation, BrokerNeighbours.PARENT) != 0))
                    {
                        //if (myPort == 3333)
                        //{
                        Console.WriteLine("");
                        Console.WriteLine("myPort {0} and myIp {1}", myPort, myIp);
                        Console.WriteLine("sent to PARENT " + topic);
                        Console.WriteLine("");
                        //}
                        brokerTreeInterface[BrokerNeighbours.PARENT].filterSubscription(topic, ip, port);
                    }
                }
            }
        }

        /*
        
            porque 'e que nao 'e igual ao subscription???

            */
        public void filterUnsubscriptionFlood(string topic, int port)
        {
            string relation = sourceType("localhost", port);
            BrokerInterface broTest;
            lock (brokerTreeInterface)
            {
                if (String.CompareOrdinal(RoutingPolicyType.FILTER, routing) == 0)
                {
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONL, out broTest) && (string.Compare(relation, BrokerNeighbours.SONL) != 0))
                    {
                        
                        brokerTreeInterface[BrokerNeighbours.SONL].filterUnsubscription(topic, myIp, myPort);
                    }
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONR, out broTest) && (string.Compare(relation, BrokerNeighbours.SONR) != 0))
                    {
                        brokerTreeInterface[BrokerNeighbours.SONR].filterUnsubscription(topic, myIp, myPort);
                    }
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.PARENT, out broTest) && (string.Compare(relation, BrokerNeighbours.PARENT) != 0))
                    {
                        brokerTreeInterface[BrokerNeighbours.PARENT].filterUnsubscription(topic, myIp, myPort);
                    }
                }
            }
        }


        /*
        
            Methods to be executed in Threads

        */


        // flood
        public void receiveOrderToFlood(string topic, string message, string ip, int port)
        {
            
            var t = new Thread(() => RealreceiveOrderToFlood(topic, message, ip, port));
            t.Start();
            //return t;
        }
        //used for the PuppetMaster to request a broker to flood a message
        public void RealreceiveOrderToFlood(string topic, string message, string ip, int port)
        {


            // sourceType cases: {publisher, sonL, sonR, parent}
            string source = sourceType(ip, port);


            if (String.CompareOrdinal(OrderingType.FIFO, ordering) == 0)
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
            else if (String.CompareOrdinal(OrderingType.TOTAL, ordering) == 0)
            {
                //TODO 
            }
            else
            {
                flood(source, topic, message);
            }

        }

        // Subscription
        public void filterSubscription(string topic, string ip, int port)
        {
            var t = new Thread(() => RealfilterSubscription(topic, ip, port));
            t.Start();
            //return t;
        }

        public void RealfilterSubscription(string topic, string ip, int port)
        {
            string relation = sourceType(ip, port);
            Console.WriteLine("FS " + relation);

            TopicsTable testTable = new TopicsTable();

            //foreach (string t in filteringTable[relation].getTopicDict().Keys)
            //{
            //    Console.WriteLine("t:  " + t);
            //}
            lock (filteringTable)
            {
                if (filteringTable.TryGetValue(relation, out testTable))
                {

                    //foreach (string rel in filteringTable.Keys)
                    //{
                    //    foreach (var top in filteringTable[rel].getTopicDict().Keys)
                    //    {
                    //        Console.WriteLine("relation : {0} and topic {1}", rel, top );
                    //    }
                    //}

                    if (filteringTable[relation].containsTopic(topic))
                    {
                        filteringTable[relation].addSubNumber(topic);
                        //Console.WriteLine(relation + " adicionou " + topic + " Sub Number " + filteringTable[relation].getTopicDict()[topic]);
                    }
                    else
                    {
                        filteringTable[relation].AddTopic(topic);
                        filterSubscriptionFlood(topic, myIp, myPort);
                        //Console.WriteLine(relation + " novo " + topic + " LOL " + filteringTable[relation].getTopicDict()[topic]);

                    }
                }
                else
                {
                    testTable = new TopicsTable();
                    testTable.AddTopic(topic);
                    filteringTable.TryAdd(relation, testTable);
                    //filteringTable[relation].AddTopic(topic);
                    filterSubscriptionFlood(topic, myIp, myPort);
                    //Console.WriteLine(relation + " novo  " + topic + " topico " + filteringTable[relation].getTopicDict()[topic]);

                }
            }
        }

        // Unsubscription

        public void filterUnsubscription(string topic, string ip, int port)
        {
            var t = new Thread(() => RealfilterUnsubscription(topic, ip, port));
            t.Start();
        }

        private void RealfilterUnsubscription(string topic, string ip , int port)
        {
            
            string relation = sourceType(ip, port);
            TopicsTable testTable = new TopicsTable();
            lock (filteringTable)
            {
                if (filteringTable.TryGetValue(relation, out testTable))
                {
                    if (filteringTable[relation].containsTopic(topic))
                    {

                        if (filteringTable[relation].remSubNumber(topic) == 0)
                        {
                            filterUnsubscriptionFlood(topic, port);
                        }
                    }
                }
            }
        }


        public void status()
        {
            var t = new Thread(() => Realstatus());
            t.Start();
        }

        public void Realstatus()
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
        /*
        
            end of thread executed methods
        */





        // PuppetMaster envia ordem para o broker para adicionar um subscriber que esta conectado


        public void addSubscriber(int port)
        {
            var t = new Thread(() => RealaddSubscriber(port));
            t.Start();
        }
        public void RealaddSubscriber(int port)
        {
            Console.WriteLine("Subscriber adicionado " + port);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:" + port + "/sub");

            subscribers.Add(port, subscriber);
        }

        public void addPublisher(int port)
        {
            var t = new Thread(() => RealaddPublisher(port));
            t.Start();
        }
        public void RealaddPublisher(int port)
        {
            Console.WriteLine("Publisher adicionado " + port);
            PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:" + port + "/pub");
            publishers.Add(publisher);
        }

        public void addBroker (int port, string ip, string relation)
        {
            var t = new Thread(() => RealaddBroker(port, ip, relation));
            t.Start();
        }

        public void RealaddBroker(int port, string ip, string relation)
        {
            Console.WriteLine("Broker adicionado " + port);
            BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://" + ip + ":" + port + "/broker");
            lock (brokerTreeIpAndPort)
            {
                lock (brokerTreeInterface)
                {
                    switch (relation)
                    {
                        case BrokerNeighbours.SONL:
                            brokerTreeInterface.Add(BrokerNeighbours.SONL, broker);
                            Tuple<string, int> ipAndPortL = new Tuple<string, int>(ip, port);
                            brokerTreeIpAndPort.Add(BrokerNeighbours.SONL,ipAndPortL);
                            break;
                        case BrokerNeighbours.SONR:
                            brokerTreeInterface.Add(BrokerNeighbours.SONR, broker);
                            Tuple<string, int> ipAndPortR = new Tuple<string, int>(ip, port);
                            brokerTreeIpAndPort.Add(BrokerNeighbours.SONR, ipAndPortR);
                            break;
                        case BrokerNeighbours.PARENT:
                            brokerTreeInterface.Add(BrokerNeighbours.PARENT, broker);
                            Tuple<string, int> ipAndPortP = new Tuple<string, int>(ip, port);
                            brokerTreeIpAndPort.Add(BrokerNeighbours.PARENT, ipAndPortP);
                            break;
                    }
                }
            }
        }




        public void registerLocalPuppetMaster( int port)
        {
            var t = new Thread(() => RealregisterLocalPuppetMaster(port));
            t.Start();
        }

        public void RealregisterLocalPuppetMaster(int port)
        {
            PuppetInterface puppetMaster = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), "tcp://localhost:" + port + "/puppet");
            this.localPuppetMaster = puppetMaster;
            Console.WriteLine("PuppetMasterLocal adicionado " + port);
        }



        private void informPuppetMaster(string action)
        {
            var t = new Thread(() => RealinformPuppetMaster(action));
            t.Start();
        }

        private void RealinformPuppetMaster(string action)
        {
            if (string.Compare(logging,LoggingLevelType.FULL)==0)
            {

                lock (localPuppetMaster)
                {
                    localPuppetMaster.informAction(action);
                }

            }
        }


        public void policies(string routing, string ordering, string logging)
        {
            var t = new Thread(() => Realpolicies(routing, ordering, logging));
            t.Start();
        }


        public void Realpolicies(string routing, string ordering, string logging)
        {
            this.routing = routing;
            this.ordering = ordering;
            this.logging = logging;
        }

        public void giveInfo(string ip, int port)
        {
            var t = new Thread(() => RealgiveInfo(ip, port));
            t.Start();
        }

        public void RealgiveInfo(string ip, int port)
        {
            this.myPort = port;
            this.myIp = ip;
        }


    }
}
