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

    public interface PuppetInterface
    {
        void informAction(string action);
            
    }
    public interface PublisherInterface
    {
        void recieveOrderToPublish(string topic, string message, int numeberOfEvents, int interval_x_ms);

        void registerLocalBroker(string BrokerName, int Brokerport);

        void addPupperMaster(string name, int port);

        void policies(string routing, string ordering, string logging);

        void status();
    }
    public interface SubscriberInterface
    {
        void recieveOrderToSubscribe(string topic, string subName, int subPort);

        void recieveOrderToUnSubscribe(string topic, int subPort);

        void registerLocalBroker(string BrokerName, int Brokerport);

        void printRecievedMessages();

        void Callback(object sender, MessageArgs m);

        void addPupperMaster(string name, int port);

        void policies(string routing, string ordering, string logging);

        void status();
    }
    public interface BrokerInterface
    {
        void recieveOrderToFlood(string topic, string message, object source);

        void subscribeRequest(string topic, string subscriberName, int port);

        void unSubscribeRequest(string topic, int port);

        void addSubscriber(string name, int port);

        void addPublisher(string name, int port);

        void addBroker(string name, int port, string relation);

        void addPupperMaster(string name, int port);

        void policies(string routing, string ordering, string logging);

        void status();
    }
}
