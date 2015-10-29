using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;

using SESDADInterfaces;

namespace SESDAD
{
    
    class Broker
    {
        [STAThread]
        static void Main(string[] args)
        {
            string brokerName = "Broker";

            TcpChannel channel = new TcpChannel(8088);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublishserServices), brokerName,
                WellKnownObjectMode.Singleton);

            

            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }
    }


    class PublishserServices : MarshalByRefObject, BrokerInterface
    {
        List<SubscriberInterface> subscribers = new List<SubscriberInterface>();
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        List<BrokerInterface> brokers = new List<BrokerInterface>();
        Dictionary<SubscriberInterface, List<string>> subscribersTopics = new Dictionary<SubscriberInterface, List<string>>();


        //used for the PuppetMaster to request a broker to flood a message
        public void recieveOrderToFlood(string topic, string message)
        {

            foreach (BrokerInterface broker in brokers)
            {
                broker.recieveOrderToFlood(topic, message);
            }

            if (subscribers.Count == 1)
            {
                subscribers[0].recieveMessage(topic, message);
                Console.WriteLine("sent message");
            }
            else
            {
                Console.WriteLine("No subs");
            }
            /*
            // percorre a lista de subscribers que o broker conhece, e verifica quais os brokers que estao subscritos ao topico
            foreach(SubscriberInterface sub in subscribers)
            {
                
                if (subscribersTopics.ContainsKey(sub)) //entra se o subscriber ja' existir na lista
                {
                    //verifica se subscriber esta subscrito ao topico
                   if(subscribersTopics[sub].Contains(topic))
                    {
                        //envia a mensagem ao subscriber
                        Console.WriteLine("Sent message to subscriber..");
                        sub.recieveMessage(topic, message);
                    }
                }
                else
                {
                    Console.WriteLine("Not subscribed..");
                }
            }
            */
                Console.WriteLine("Flooded: " + message);

        }

        public void subscribeRequest(string topic, string subscriberName, int port)
        {


            SubscriberInterface subscriber =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:"+ port + "/" + subscriberName);

            Console.WriteLine("Added subscriber at : " + "tcp://localhost:" + port + "/" + subscriberName);
            subscribers.Add(subscriber);
            /*
            
                    FALTA IMPLEMENTAR COM CALLBACK
            
            */
            if (subscribersTopics.ContainsKey(subscriber)) //entra se o subscriber ja' existir na lista
            {
                subscribersTopics[subscriber].Add(topic); // adiciona 'a lista do subscriber, o novo topico subscrito (com um Del facilmente se faz unsubscribe)
                Console.WriteLine("Added Topic to the subscriber list");
            }else
            {
                List <string> topicsList = new List<string>();
                topicsList.Add(topic);

                subscribersTopics.Add(subscriber, topicsList);

                Console.WriteLine("Added subscriber to the dictionary: " + topicsList[0]);
            }

        }

        // PuppetMaster envia ordem para o broker para adicionar um subscriber que esta conectado
        public void addSubscriber(string name, int port)
        {
            Console.WriteLine("Subscriber adicionado " + name + " " + port);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:" + port + "/" + name);
            //subscriber.registerLocalBroker(name, port);
            //subscribers.Add(subscriber);
        }

        public void addPublisher(string name, int port)
        {
            Console.WriteLine("Publisher adicionado " + name + " " + port );
            PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:" + port + "/" + name);
            //publisher.registerLocalBroker(name, port);
            publishers.Add(publisher);
        }

        public void addBroker(string name, int port)
        {
            Console.WriteLine("Broker adicionado " + name + " " + port);
            //LOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOP
            /*
            BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://localhost:" + port + "/" + name);

            
            //        PERIGO DE LOOP 
            

            broker.addBroker(name, port);


            brokers.Add(broker);
            
            */
        }
    }
}
