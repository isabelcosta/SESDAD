using System;
using System.Collections.Generic;


namespace SESDADInterfaces
{
    public interface PuppetMasterInterface
    {
        // penso que nao seja necessario, pois ninguem fala com o PuppetMaster
    }
    public interface PublisherInterface
    {
        void recieveOrderToPublish(string topic, string message);

        void registerLocalBroker(string BrokerName, int Brokerport);
    }
    public interface SubscriberInterface
    {
        void recieveOrderToSubscribe(string topic, string subName, int subPort);

        void recieveMessage(string topic, string message);

        void registerLocalBroker(string BrokerName, int Brokerport);

        void printRecievedMessages();
       
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
