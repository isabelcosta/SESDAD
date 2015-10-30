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
    class PuppetMaster
    {
        [STAThread]
        static void Main(string[] args)
        {
            string pubName = "Publisher";
            int pubPort = 8086;

            string broName = "Broker";
            int broPort = 8088;

            string subName = "Subscriber";
            int subPort = 8087;

            string message = "Existem 7 notas musicais";
            string topic = "Musica";

            string messageTwo = "As ovelhas têm lã";
            string topicTwo = "Ovelhas";


            Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort + "/" + pubName);
            PublisherInterface publisher =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + pubPort + "/" + pubName);


            Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort + "/" + subName);
            SubscriberInterface subscriber =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + subPort + "/" + subName);


            Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort + "/" + broName);
            BrokerInterface broker =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + broPort + "/" + broName);


            // 1. establecer as ligacoes entre os varios elementos do no'
            publisher.registerLocalBroker(broName, broPort);
            subscriber.registerLocalBroker(broName, broPort);
            broker.addPublisher(pubName, pubPort);
            broker.addSubscriber(subName, subPort);

            // 2. publisher comeca a publicar

            publisher.recieveOrderToPublish(topic, message);

            Thread.Sleep(2000); //espera 2 segundos

            publisher.recieveOrderToPublish(topic, message);


            // 3. Subscriber subscreve ao topico para comecar a receber mensagens
            subscriber.recieveOrderToSubscribe(topic, subName, subPort);

            Thread.Sleep(2000); //espera 2 segundos
            publisher.recieveOrderToPublish(topic, message);
            
            Thread.Sleep(2000); //espera 2 segundos
            publisher.recieveOrderToPublish(topicTwo, messageTwo);

            Thread.Sleep(2000);
            subscriber.recieveOrderToSubscribe(topicTwo, subName, subPort);

            Thread.Sleep(2000); //espera 2 segundos
            publisher.recieveOrderToPublish(topicTwo, messageTwo);

            Thread.Sleep(4000);
            subscriber.printRecievedMessages();


            Console.WriteLine("Press <enter> to exit..");
            Console.ReadLine();

        }
    }
}
