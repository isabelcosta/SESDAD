using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SESDADInterfaces
{
    //**********************************************************************
    //                              CONSTANTS
    //**********************************************************************

    public class RoutingPolicyType
    {
        public const string FILTER = "filter";
        public const string FLOODING = "flooding";
    }

    public class OrderingType
    {
        public const string NO = "NO";
        public const string TOTAL = "TOTAL";
        public const string FIFO = "FIFO";
    }

    public class LoggingLevelType
    {
        public const string FULL = "full";
        public const string LIGHT = "light";
    }

    public class ProcessType
    {
        public const string BROKER = "broker";
        public const string PUBLISHER = "publisher";
        public const string SUBSCRIBER = "subscriber";
    }

    public class BrokerNeighbours
    {
        public const string SONL = "sonL";
        public const string SONR = "sonR";
        public const string PARENT = "parent";
    }
    public class BrokerOrders
    {
        public const string FLOOD = "flooding";
        public const string FILTERING = "filtering";
        public const string STATUS = "status";
    }

    public class SubscriberOrders
    {
        public const string SUBSCRIBE = "subscribe";
        public const string UNSUBSCRIBE = "unsubscribe";
        public const string STATUS = "status";
    }

    public class PublisherOrders
    {
        public const string PUBLISH = "publish";
        public const string STATUS = "status";
    }

    public class seqNumber
    {
        private ConcurrentDictionary<string, int> seqN = new ConcurrentDictionary<string, int>();

        public seqNumber()
        {
            seqN.TryAdd("order", 1);
        }

        public void increaseSeqN()
        {
            seqN["order"] += 1;
        }

        public int getSeqN()
        {
            return seqN["order"];
        }
    }

    public interface PuppetInterface
    {
        void receiveOrderToCrash(string processName);
        void receiveOrderToFreeze(string processName);
        void receiveOrderToUnfreeze(string processName);
        void receiveOrderToPublish(string processName, string topic, int numberOfEvents, int interval_x_ms);
        void receiveOrderToSubscribe(string processName, string topic);
        void receiveOrderToUnsubscribe(string processName, string topic);
        void receiveOrderToShowStatus();
        void informAction(string action);
        void slaveIsReady();
        int getNumberOfSlaves();
        void slavesAreUp();
        bool areAllSlavesUp();
    }
    [Serializable]
    public class MessageArgs : EventArgs
    {
        private string body;
        private string topic;

        public MessageArgs(string topic, string body)
        {
            this.body = body;
            this.topic = topic;
        }

        public string Body { get { return body; } }
        public string Topic { get { return topic; } }
    }
    
    public interface PublisherInterface
    {
        void receiveOrderToPublish(string topic, int numeberOfEvents, int interval_x_ms);

        // network config
        void registerLocalBroker(int Brokerport);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string name, int port);

        void status();

        void setFreezeState(bool isFrozen);
    }

    public interface SubscriberInterface
    {
        void receiveOrderToSubscribe(string topic);

        void receiveOrderToUnSubscribe(string topic);

        // network config
        void registerLocalBroker(int Brokerport);

        void printReceivedMessages();

        void Callback(object sender, MessageArgs m);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string name, int port);

        void status();

        void setFreezeState(bool isFrozen);
    }

    public interface BrokerInterface
    {
        void receiveOrderToFlood(string topic, string message, string ip, int port);

        void totalOrderFlood(string topic, string message);

        void subscribeRequest(string topic, int port);

        void unSubscribeRequest(string topic, int port);

        void filterSubscription(string topic, string ip, int port);

        void filterUnsubscription(string topic, string ip, int port);

        // network config
        void addSubscriber(int port);

        // network config
        void addPublisher(int port);

        // network config
        void addBroker(int port, string ip, string relation);

        void addRootBroker(int port, string ip);

        // network config
        void registerLocalPuppetMaster(int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string ip, int port, string name);

        void status();

        void setFreezeState(bool isFrozen);
    }
    
}
