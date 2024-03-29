﻿using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Configuration;
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
        public int SubID { get { return subID; } }
        
        public MySubs SubDelegate { get { return subDelegate; } }

        public void addSubscription(MySubs subscription)
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
        public bool containsTopic(string topicNew)
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
        string myName;

        bool freezeFlag = false;
        private List<Tuple<string, List<string>>> myFrozenOrders = new List<Tuple<string, List<string>>>();

        private seqNumber seqNub = new seqNumber();


        PuppetInterface localPuppetMaster;
        BrokerInterface rootBroker;

        //Filtering
        //filteringTable{relation, list of topics to flood there}
        ConcurrentDictionary<string, TopicsTable> filteringTable = new ConcurrentDictionary<string, TopicsTable>();
        
        //Total Order
        // { relation , number to decrement }
        ConcurrentDictionary<string, int> seqNbToDecrement = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<string, int> totalMessages = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<int, int> notSentMessages = new ConcurrentDictionary<int, int>();


        Dictionary<string, List<SubscriberRequestID>> delegates = new Dictionary<string, List<SubscriberRequestID>>();

        //public event MySubs E;

        ConcurrentDictionary<int, SubscriberInterface> subscribers = new ConcurrentDictionary<int, SubscriberInterface>();
        ConcurrentDictionary<int, PublisherInterface> publishers = new ConcurrentDictionary<int, PublisherInterface>();
        //List<BrokerInterface> brokers = new List<BrokerInterface>();

        ConcurrentDictionary<string, BrokerInterface> brokerTreeInterface = new ConcurrentDictionary<string, BrokerInterface>();
        Dictionary<string, Tuple<string, int>> brokerTreeIpAndPort = new Dictionary<string, Tuple<string, int>>();


        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();

        //FIFO
        Dictionary<string, int> fifoManager = new Dictionary<string, int>();
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

        public string sourceType(string ip, int port)
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

            lock (subscribers)
            {
                foreach (KeyValuePair<int, SubscriberInterface> pair in subscribers)
                {
                    if (pair.Key == port)
                    {
                        return ProcessType.SUBSCRIBER;
        }
                }
            }

            foreach (KeyValuePair<int, PublisherInterface> pair in publishers)
            {
                if (pair.Key == port)
                {
                    return ProcessType.PUBLISHER;
                }
            }


            return ProcessType.PUBLISHER;
        }


        public bool canFilterFlood(string topic, string relation)
        {

            if (String.CompareOrdinal(RoutingPolicyType.FILTER, routing) == 0)
            {
                TopicsTable testTable = new TopicsTable();
               
                if (filteringTable.TryGetValue(relation, out testTable))
                {
                    //dictionary<string, int>
                    return filteringTable[relation].containsTopic(topic);
                 }
                    

                return false;
            }
            return true;
        }

        public void flood(string sourceType, string topic, string message)
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

            //lock (brokerTreeInterface)
            {
                if ((String.CompareOrdinal(sourceType, BrokerNeighbours.SONR) != 0) &&
                                    brokerTreeInterface.TryGetValue(BrokerNeighbours.SONR, out broTest) &&
                                                        canFilterFlood(topic, BrokerNeighbours.SONR))
                {
                    //lock broker??
                    brokerTreeInterface[BrokerNeighbours.SONR].receiveOrderToFlood(topic, message, myIp, myPort);
                }
                if ((String.CompareOrdinal(sourceType, BrokerNeighbours.SONL) != 0) &&
                                    brokerTreeInterface.TryGetValue(BrokerNeighbours.SONL, out broTest) &&
                                                        canFilterFlood(topic, BrokerNeighbours.SONL))
                {
                    //lock broker??
                    brokerTreeInterface[BrokerNeighbours.SONL].receiveOrderToFlood(topic, message, myIp, myPort);
                }
                if ((String.CompareOrdinal(sourceType, BrokerNeighbours.PARENT) != 0) &&
                                    brokerTreeInterface.TryGetValue(BrokerNeighbours.PARENT, out broTest) &&
                                                        canFilterFlood(topic, BrokerNeighbours.PARENT))
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
            string action = "BroEvent - " + myName + " Flooded message on topic " + topic;
            informPuppetMaster(action);
            //Console.WriteLine(action);
        }
        

        public bool checkIfIsNext(int msg, string pubName, string topic)
        {
            lock (fifoManager)
            {
                int msgMngmt;
                if (fifoManager.TryGetValue(pubName, out msgMngmt))
                {
                    // Key was in dictionary; "value" contains corresponding value

                    if (fifoManager[pubName] + 1 == msg)
                    {
                        fifoManager[pubName] = msg;
                        return true;
                    }
                }
            }


            if (msg == 1 )
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
                    //
                    // Não faço ideia porque é que se está a verificar isto
                    //
                    if (fifoManager.ContainsKey(pubName))
                    {
                        fifoManager.Remove(pubName);
                        if(fifoQueue.ContainsKey(pubName))
                        {
                            fifoQueue.Remove(pubName);
                        }
                    }
                    fifoManager.Add(pubName, msg);
                }
                    return true;
            }
            return false;
        }
        
        public void parseMessage(ref string pubName, ref int msg, string message)
        {
            if (pubName == null) throw new ArgumentNullException("pubName");

            string[] msgTemp1 = message.Split(' ');

            pubName = msgTemp1[0];
            msg = int.Parse(msgTemp1[1]);
        }

        public void addToQueue(string pubPlusTopic, int msgNumber, string message)
        {
            Tuple<int, string> msgNPlusMsg = new Tuple<int, string>(msgNumber, message);

            lock (fifoQueue)
            {
                List<Tuple<int, string>> msgList;
                if (!fifoQueue.TryGetValue(pubPlusTopic, out msgList))
                {

                    msgList = new List<Tuple<int, string>> { msgNPlusMsg };
                    // Key wasn't in dictionary; "value" is now 0
                    fifoQueue.Add(pubPlusTopic, msgList);
                }
                else
                {
                    // Key was in dictionary; "value" contains corresponding value
                    fifoQueue[pubPlusTopic].Add(msgNPlusMsg);
                }
            }
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool getFromQueue(string pubName, ref string message, ref int msg)

        {
            //Console.WriteLine("MESSAGE NUMBER : {0}", msg.Item1);
            List<Tuple<int, string>> msgList = new List<Tuple<int, string>>();
            Tuple<int, string> msgNPlusMsg = new Tuple<int, string>(msg, message);
            lock (fifoQueue)
            {
                if (fifoQueue.TryGetValue(pubName, out msgList))
                {
                    // Key was in dictionary; "value" contains corresponding value
                    foreach (Tuple<int, string> currentMsg in fifoQueue[pubName])
                    {
                        if (currentMsg.Item1 == msg + 1)
                        {
                            message = currentMsg.Item2;
                            int msgMngmt;
                            lock (fifoManager)
                            {
                                if (fifoManager.TryGetValue(pubName, out msgMngmt))
                                {
                                  msgMngmt = currentMsg.Item1;
                                   fifoManager[pubName] = msgMngmt;
                                }
                            }
                            //msgList.Remove(currentMsg);
                            fifoQueue[pubName].Remove(currentMsg);
                            if (fifoQueue[pubName].Count == 0)
                            {
                                //Console.WriteLine("Entrei aqui para remover");
                                fifoQueue.Remove(pubName);
                            }
                            msg = msgMngmt;
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        public bool topicsMatch(string topicPub, string topicSub)
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

            //lock (brokerTreeInterface)
            {
                if (String.CompareOrdinal(RoutingPolicyType.FILTER, routing) == 0)
                {
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONL, out broTest) && (String.CompareOrdinal(relation, BrokerNeighbours.SONL) != 0))
                    {
                        brokerTreeInterface[BrokerNeighbours.SONL].filterSubscription(topic, myIp, myPort);
                    }
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONR, out broTest) && (String.CompareOrdinal(relation, BrokerNeighbours.SONR) != 0))
                    {
                        brokerTreeInterface[BrokerNeighbours.SONR].filterSubscription(topic, myIp, myPort);
                    }
                    if (brokerTreeInterface.TryGetValue(BrokerNeighbours.PARENT, out broTest) && (String.CompareOrdinal(relation, BrokerNeighbours.PARENT) != 0))
                    {
                        brokerTreeInterface[BrokerNeighbours.PARENT].filterSubscription(topic, myIp, myPort);
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
           // lock (brokerTreeInterface)
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
            if (this.amIFrozen())
            {
                List<string> args = new List<string>();
                args.Add(topic);
                args.Add(message);
                args.Add(ip);
                args.Add(port.ToString());
            } else
            {
            var t = new Thread(() => RealreceiveOrderToFlood(topic, message, ip, port));
            t.Start();
            }
            //return t;
        }

        private string totalOrderMessageFilter(string message, string relation)
        {


            // { pubName , SeqNb.SeqN }
            string[] msgTemp1 = message.Split(' ');

            string newMessage;
            int outUse;
            int numToDec = int.Parse(msgTemp1[1]);

            if (string.CompareOrdinal("broker0", myName) != 0 && string.CompareOrdinal("broker00", myName) != 0)   // nao e' a root
            {
                lock (seqNub)
                {
                    if (seqNbToDecrement.TryGetValue(relation, out outUse))
                    {
                        numToDec -= seqNbToDecrement[relation];
                    }
                }

                newMessage = msgTemp1[0] + " " + numToDec;
            }
            else // e' a root
            {
                lock (seqNub)
                {
                    numToDec = seqNub.getSeqN();
                    if (seqNbToDecrement.TryGetValue(relation, out outUse))
                    {
                        numToDec -= seqNbToDecrement[relation];
                    }

                    newMessage = msgTemp1[0] + " " + numToDec;
                    seqNub.increaseSeqN();
                }
            }

            return newMessage;
        }
        
        private string totalOrderMessageFlooding(string message, int subPort, bool subscription)
        {
        
            
            // { pubName , SeqNb.SeqN }
            string[] msgTemp1 = message.Split(' ');

            string newMessage = message;

            int numToDec;
            //lock (brokerTreeInterface)
            {
                
                if (string.CompareOrdinal("broker0", myName) == 0 || string.CompareOrdinal("broker00", myName) == 0)   // 'e a root
                {

                    lock (seqNub)
                    {
                        Console.WriteLine("seqNub " + seqNub.getSeqN());
                        newMessage = msgTemp1[0] + " " + seqNub.getSeqN();
                        seqNub.increaseSeqN();
                    }
                }
                else if (subscription) // entrega da mensagem ao subscriber
                {

                    numToDec = int.Parse(msgTemp1[1]);
                    int tempCount;
                    if (notSentMessages.TryGetValue(subPort, out tempCount))
                    {
                        numToDec -= notSentMessages[subPort];
                    }
                
                    newMessage = msgTemp1[0] + " " + numToDec;
                }
            }

            return newMessage;
        }

        public void totalOrderFlood(string topic, string message)
        /*{

            var t = new Thread(() => RealtotalOrderFlood(topic, message));
            t.Start();
            //return t;
        }

        public void RealtotalOrderFlood(string topic, string message)
        */{
            // will be updated -> with the real sequence number, wich is set by the root and updated by the other brokers
            BrokerInterface broTest;
            string newMessage = message;
            bool sentSonL = false;
            bool sentSonR = false;
            /*
                Incrementar o numero total de mensagens recebidas
            */
            if (String.CompareOrdinal(routing, RoutingPolicyType.FLOODING) == 0)
            {
                int tempCount = 0;
                if (totalMessages.TryGetValue("TOTAL", out tempCount))
                {
                    totalMessages["TOTAL"] += 1;
                }
                else
                {
                    totalMessages.TryAdd("TOTAL", 1);
                }
            }



            /*
                       Enviar as mensagens para os filhos
            */

            if (String.CompareOrdinal(routing, RoutingPolicyType.FLOODING) == 0)
            {
                newMessage = totalOrderMessageFlooding(message, 0, false);
                //FIFO
            }

            //lock (brokerTreeInterface)
            {
                if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONR, out broTest) &&
                                        canFilterFlood(topic, BrokerNeighbours.SONR))
                {
                    if (String.CompareOrdinal(routing, RoutingPolicyType.FILTER) == 0)
                    {
                        newMessage = totalOrderMessageFilter(message, BrokerNeighbours.SONR);
                    }
                    //Console.WriteLine("New message ----- "
                    //    +newMessage);

                    brokerTreeInterface[BrokerNeighbours.SONR].totalOrderFlood(topic, newMessage);
                    sentSonR = true;
                }

                if (brokerTreeInterface.TryGetValue(BrokerNeighbours.SONL, out broTest) &&
                                        canFilterFlood(topic, BrokerNeighbours.SONL))
                {
                    if (String.CompareOrdinal(routing, RoutingPolicyType.FILTER) == 0)
                    {
                        if (!sentSonR)
                        {
                            newMessage = totalOrderMessageFilter(message, BrokerNeighbours.SONL);
                        }
                    }
                    
                    brokerTreeInterface[BrokerNeighbours.SONL].totalOrderFlood(topic, newMessage);
                    sentSonL = true;
                }

            }

            if (String.CompareOrdinal(routing, RoutingPolicyType.FILTER) == 0)
            {
                if (!sentSonR)
                {
                    //Console.WriteLine( "nao enviou para o R");
                    
                    int temp;
                    lock (seqNub)
                    {
                        if (seqNbToDecrement.TryGetValue(BrokerNeighbours.SONR, out temp))
                        {
                            seqNbToDecrement[BrokerNeighbours.SONR] += 1;
                        }
                        else
                        {
                            seqNbToDecrement.TryAdd(BrokerNeighbours.SONR, 1);
                        }
                    }
                    
                }

                if (!sentSonL)
                {
                    //Console.WriteLine("nao enviou para o L");
                    
                    int temp;
                    lock (seqNub)
                    {
                        if (seqNbToDecrement.TryGetValue(BrokerNeighbours.SONL, out temp))
                        {
                            seqNbToDecrement[BrokerNeighbours.SONL] += 1;
                        }
                        else
                        {
                            seqNbToDecrement.TryAdd(BrokerNeighbours.SONL, 1);
                        }
                    }
                    
                }
            }

            /*
            
                TODO : AQUI FALTA FAZER A ENTREGA ORDEIRA DAS MENSAGENS AOS SUBS
            
            */
            List<int> delivered = new List<int>();

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
                            delivered.Add(subReqID.SubID);
                            
                            newMessage = totalOrderMessageFlooding(message, subReqID.SubID, true);
                            
                            
                            subReqID.SubDelegate(this, new MessageArgs(topic, newMessage));
                        }
                    }
                }
                
            }
            int tempN;
            lock (subscribers)
            {
                foreach (KeyValuePair<int, SubscriberInterface> pair in subscribers)
                {
                    if (!delivered.Contains(pair.Key))
                    {
                        if (notSentMessages.TryGetValue(pair.Key, out tempN))
                        {
                            notSentMessages[pair.Key] += 1;
                        }
                        else
                        {
                            notSentMessages.TryAdd(pair.Key, 1);
                        }
                    }
                }
            }

            delivered.Clear();
            string action = "BroEvent - " + myName + " Flooded message on topic with TotalOrder " + topic;
            informPuppetMaster(action);
            //Console.WriteLine(action);

        }

        //used for the PuppetMaster to request a broker to flood a message
        [MethodImpl(MethodImplOptions.Synchronized)]
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
                int msg = 0;

                parseMessage(ref pubName, ref msg, message);


                // Tuple<int, int> msgMngmt = new Tuple<int, int>(0, 0);

                //Console.WriteLine("PRE-CheckIfIsNext - {0}", msg.Item1);
                if (checkIfIsNext(msg, pubName, topic))
                {
                    do
                    {
                        flood(source, topic, message);
                        if (getFromQueue(pubName, ref message, ref msg))
                        {
                            //Console.WriteLine("GETFROMQUEUE - {0}", msg.Item1);
                            break;
                        }
                        // Thread
                    } while (true);
                }
                else
                {
                    // Just add to queue
                    //Console.WriteLine("ADICIONAR À QUEUE - {0}", msg.Item1);
                    addToQueue(pubName, msg, message);
                }
            }
            else if (String.CompareOrdinal(OrderingType.TOTAL, ordering) == 0)
            {

                if (String.CompareOrdinal(source, ProcessType.PUBLISHER) == 0)
                {
                    lock (brokerTreeInterface)
                    {
                        if (brokerTreeInterface.ContainsKey(BrokerNeighbours.PARENT)) // nao e' root
                        {
                            rootBroker.totalOrderFlood(topic, message);
                        }
                        else // e' root
                        {
                            totalOrderFlood(topic, message);
                        }
                    }
                }
                else
                {
                    /*
                        NADA ??
                    */
                }

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

            TopicsTable testTable = new TopicsTable();

                if (filteringTable.TryGetValue(relation, out testTable))
                {

                    if (filteringTable[relation].containsTopic(topic))
                    {
                        filteringTable[relation].addSubNumber(topic);
                    }
                    else
                    {
                        filteringTable[relation].AddTopic(topic);
                        filterSubscriptionFlood(topic, ip, port);

                    }
                }
                else
                {
                    testTable = new TopicsTable();
                    testTable.AddTopic(topic);
                    bool test = filteringTable.TryAdd(relation, testTable);
                    filterSubscriptionFlood(topic, ip, port);

                }
            }

        // Unsubscription

        public void filterUnsubscription(string topic, string ip, int port)
        {
            var t = new Thread(() => RealfilterUnsubscription(topic, ip, port));
            t.Start();
        }

        private void RealfilterUnsubscription(string topic, string ip, int port)
        {
            
            string relation = sourceType(ip, port);
            TopicsTable testTable = new TopicsTable();
            
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


        public void status()
        {
            if (this.amIFrozen()) {
                List<string> args = new List<string>();
                myFrozenOrders.Add(new Tuple<string, List<string>>(BrokerOrders.STATUS, args));
            } else {
            var t = new Thread(() => Realstatus());
            t.Start();
        }
        }

        public void Realstatus()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(".---------------- Status ----------------.");
            Console.WriteLine("|");
            if (subscribers.Count == 0)
            {
                Console.WriteLine("| ..No Subscribers..");
            }
            else
            {
                Console.WriteLine("| ..Subscribers at..");
                lock (subscribers)
                {
                    foreach (KeyValuePair<int, SubscriberInterface> pair in subscribers)
                    {
                        Console.WriteLine("|     - " + pair.Key);
                    }
                }
            }
            Console.WriteLine("| ");
            Console.WriteLine(".----------------------------------------.");
            Console.WriteLine("| ");
            if (publishers.Count == 0)
            {
                Console.WriteLine("| ..No Publishers..");
            }
            else
            {
                Console.WriteLine("| ..Publishers");
                foreach (KeyValuePair<int, PublisherInterface> pair in publishers)
                {
                    Console.WriteLine("|     - " + pair.Key);
                }
            }
            
            Console.WriteLine("| ");
            Console.WriteLine(".----------------------------------------.");
            Console.WriteLine("");
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

            subscribers.TryAdd(port, subscriber);
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
            publishers.TryAdd(port, publisher);
        }

        public void addRootBroker(int port, string ip)
        {
            var t = new Thread(() => RealaddRootBroker(port, ip));
            t.Start();
        }

        public void RealaddRootBroker(int port, string ip)
        {

            if (myPort != port)
            {
                BrokerInterface rootB = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://" + ip + ":" + port + "/broker");
                this.rootBroker = rootB;
                Console.WriteLine("Root Broker adicionado " + port);
            }
        }

        public void addBroker(int port, string ip, string relation)
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
                //lock (brokerTreeInterface)
                {
                    switch (relation)
                    {
                        case BrokerNeighbours.SONL:
                            brokerTreeInterface.TryAdd(BrokerNeighbours.SONL, broker);
                            Tuple<string, int> ipAndPortL = new Tuple<string, int>(ip, port);
                            brokerTreeIpAndPort.Add(BrokerNeighbours.SONL, ipAndPortL);
                            break;
                        case BrokerNeighbours.SONR:
                            brokerTreeInterface.TryAdd(BrokerNeighbours.SONR, broker);
                            Tuple<string, int> ipAndPortR = new Tuple<string, int>(ip, port);
                            brokerTreeIpAndPort.Add(BrokerNeighbours.SONR, ipAndPortR);
                            break;
                        case BrokerNeighbours.PARENT:
                            brokerTreeInterface.TryAdd(BrokerNeighbours.PARENT, broker);
                            Tuple<string, int> ipAndPortP = new Tuple<string, int>(ip, port);
                            brokerTreeIpAndPort.Add(BrokerNeighbours.PARENT, ipAndPortP);
                            break;
                    }
                }
            }
        }




        public void registerLocalPuppetMaster(int port)
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
            if (string.Compare(logging, LoggingLevelType.FULL) == 0)
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

        public void giveInfo(string ip, int port, string name)
        {
            var t = new Thread(() => RealgiveInfo(ip, port, name));
            t.Start();
        }

        public void RealgiveInfo(string ip, int port, string name)
        {
            this.myPort = port;
            this.myIp = ip;
            this.myName = name;
        }

        private bool amIFrozen()
        {
            return this.freezeFlag;
        }

        public void setFreezeState(bool isFrozen)
        {
            this.freezeFlag = isFrozen;

            if (!this.freezeFlag)
            {
                this.executeAllFrozenCommands();
            }
        }

        private void executeAllFrozenCommands()
        {
            List<string> args = null;
            foreach (Tuple<string, List<string>> order in myFrozenOrders)
            {
                args = order.Item2;
                switch (order.Item1)
                {
                    case BrokerOrders.FLOOD:
                        this.receiveOrderToFlood(args[0], args[1], args[2], int.Parse(args[3]));
                        break;
                    case BrokerOrders.FILTERING:
                        //this.receiveOrderToFilter(args[0], args[1], args[2], int.Parse(args[3]));
                        break;
                    case BrokerOrders.STATUS:
                        this.status();
                        break;
                }
            }
            this.myFrozenOrders.Clear();
        }
    }
}
