﻿using System;
using System.Collections.Generic;


namespace SESDADInterfaces
{

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

    public interface PuppetInterface
    {
        void informAction(string action);
            
    }
    public interface PublisherInterface
    {
        void recieveOrderToPublish(string topic, int numeberOfEvents, int interval_x_ms);

        // network config
        void registerLocalBroker(int Brokerport);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string name, int port);

        void status();
    }
    public interface SubscriberInterface
    {
        void recieveOrderToSubscribe(string topic, int subPort);

        void recieveOrderToUnSubscribe(string topic, int subPort);

        // network config
        void registerLocalBroker(int Brokerport);

        void printRecievedMessages();

        void Callback(object sender, MessageArgs m);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        void giveInfo(int port);

        void status();
    }
    public interface BrokerInterface
    {
        void recieveOrderToFlood(string topic, string message, object source);

        void subscribeRequest(string topic, int port);

        void unSubscribeRequest(string topic, int port);

        // network config
        void addSubscriber(int port);

        // network config
        void addPublisher(int port);

        // network config
        void addBroker(int port, string ip, string relation);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        void status();
    }
}
