using System;
using System.Collections.Generic;


namespace SESDADInterfaces
{
    //*********************************************************************
    //                              CONSTANTS
    //*********************************************************************

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
        public const string SONL = "SonL";
        public const string SONR = "SonR";
        public const string PARENT = "Parent";
    }

    public interface PuppetInterface
    {
        void receiveOrderToCrash(string processName);
        void receiveOrderToFreeze(string processName);
        void receiveOrderToUnfreeze(string processName);
        void receiveOrderToPublish(string processName); //mais cenas
        void receiveOrderToSubscribe(string processName); //mais cenas
        void receiveOrderToUnsubscribe(string processName); //mais cenas
        void receiveOrderToShowStatus(string processName);
        //void receiveOrderToStartProcess(string processName, string processType, string args);
        void sendLogsToMaster(string logInfo);
        void informMyMaster(string logInfo);
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
        void recieveOrderToPublish(string topic, string message);

        void registerLocalBroker(string BrokerName, int Brokerport);
    }
    public interface SubscriberInterface
    {
        void recieveOrderToSubscribe(string topic, string subName, int subPort);



        void registerLocalBroker(string BrokerName, int Brokerport);

        void printRecievedMessages();
        void Callback(object sender, MessageArgs m);

    }
    public interface BrokerInterface
    {
        void recieveOrderToFlood(string topic, string message);

        void subscribeRequest(string topic, string subscriberName, int port);

        void addSubscriber(string name, int port);

        void addPublisher(string name, int port);

        void addBroker(string name, int port);
    }
}