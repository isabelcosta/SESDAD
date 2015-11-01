using System;
using System.Collections.Generic;


namespace SESDADInterfaces
{
    public interface PuppetMasterInterface
    {
        // penso que nao seja necessario, pois ninguem fala com o PuppetMaster
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