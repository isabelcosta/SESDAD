using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualBasic.MyServices;

using SESDADInterfaces;
using System.Diagnostics;

namespace SESDAD
{
    class PuppetMaster
    {
        [STAThread]
        static void Main(string[] args)
        {
            string ip = "localhost";

            string pubName1 = "Publisher-1";
            int pubPort1 = 8086;

            string broName1 = "Broker-1";
            int broPort1 = 8087;

            string subName1 = "Subscriber-1";
            int subPort1 = 8088;


            string pubName2 = "Publisher-2";
            int pubPort2 = 8089;

            string broName2 = "Broker-2";
            int broPort2 = 8090;

            string subName2 = "Subscriber-2";
            int subPort2 = 8091;

            int frequence = 2000;
            int numbOfMsgs1 = 1;
            int numbOfMsgs3 = 3;
            int numbOfMsgs5 = 5;

            /*
                Messages
            */

            //Basics
            string messageBasics = "Music has rhythm and sound.";
            string topicBasics = @"\Music\Basics\";

            //Scales
            string messageBasicsScales = "There are twelve notes in the chromatic scale built on C.";
            string topicBasicsScales = @"\Music\Basics\Scales\";

            //Chords
            string messageBasicsChords = "A major chord has a Root, a Major Third and a Perfect Fifht.";
            string topicBasicsChords = @"\Music\Basics\Chords\";
            
            /*
                Simple Test - Network Topology

                 
                       B1(P1,S1) 
                      /                  
                B2(P2,S2)
            */


            /*
            
                1st Node S1, P1, B1

            */
            Process pubProcess1 = new Process();
            pubProcess1.StartInfo.FileName = @"G:\vicente\tecnico\4ano\DAD\proj\testeInicial\Subscriber\Publisher\bin\Debug\Publisher.exe";
            pubProcess1.StartInfo.Arguments = pubPort1.ToString();
            pubProcess1.Start();

/*
            Process subProcess1 = new Process();
            subProcess1.StartInfo.FileName = @"G:\vicente\tecnico\4ano\DAD\proj\testeInicial\Subscriber\Subscriber\bin\Debug\Subscriber.exe";
            subProcess1.StartInfo.Arguments = subPort1.ToString();
            subProcess1.Start();
*/
            Process broProcess1 = new Process();
            broProcess1.StartInfo.FileName = @"G:\vicente\tecnico\4ano\DAD\proj\testeInicial\Subscriber\Broker\bin\Debug\Broker.exe";
            broProcess1.StartInfo.Arguments = broPort1.ToString();
            broProcess1.Start();


            /*
            
                2nd Node S2, P2, B2

            */
            Process pubProcess2 = new Process();
            pubProcess2.StartInfo.FileName = @"G:\vicente\tecnico\4ano\DAD\proj\testeInicial\Subscriber\Publisher\bin\Debug\Publisher.exe";
            pubProcess2.StartInfo.Arguments = pubPort2.ToString();
            pubProcess2.Start();

/*
            Process subProcess2 = new Process();
            subProcess2.StartInfo.FileName = @"G:\vicente\tecnico\4ano\DAD\proj\testeInicial\Subscriber\Subscriber\bin\Debug\Subscriber.exe";
            subProcess2.StartInfo.Arguments = subPort2.ToString();
            subProcess2.Start();
*/
            Process broProcess2 = new Process();
            broProcess2.StartInfo.FileName = @"G:\vicente\tecnico\4ano\DAD\proj\testeInicial\Subscriber\Broker\bin\Debug\Broker.exe";
            broProcess2.StartInfo.Arguments = broPort2.ToString();
            broProcess2.Start();



            /*
                get remote Objects
            */

            /*
                1st Node
            */
            Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort1 + "/pub");
            PublisherInterface publisher1 =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + pubPort1 + "/pub");

/*
            Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort1 + "/sub");
            SubscriberInterface subscriber1 =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + subPort1 + "/sub");
*/

            Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort1 + "/broker");2
            BrokerInterface broker1 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + broPort1 + "/broker");

            /*
                2nd Node
            */
            Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort2 + "/pub");
            PublisherInterface publisher2 =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + pubPort2 + "/pub");

/*
            Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort2 + "/sub");
            SubscriberInterface subscriber2 =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + subPort2 + "/sub");

*/
            
            Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort2 + "/broker");
            BrokerInterface broker2 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + broPort2 + "/broker");


            /* 
            Network configuration
            */



            publisher1.registerLocalBroker(broPort1);
            publisher1.giveInfo(pubName1, pubPort1);
            //subscriber1.registerLocalBroker(broName1, broPort1);
            broker1.addPublisher(pubPort1);
            broker1.addSubscriber(subPort1);
            broker1.addBroker(broPort2, ip, "sonL");


            
            publisher2.registerLocalBroker(broPort2);
            publisher2.giveInfo(pubName2, pubPort1);

            //subscriber2.registerLocalBroker(broName2, broPort2);
            broker2.addPublisher(pubPort2);
            broker2.addSubscriber(subPort2);
            
            broker2.addBroker(broPort1, ip, "parent");

            // Publisher -> Basics/chords
            publisher1.recieveOrderToPublish(topicBasicsChords, numbOfMsgs5, frequence);
            publisher2.recieveOrderToPublish(topicBasicsScales, numbOfMsgs5, frequence-1000);


            Thread.Sleep(2000); //2 seconds wait

            /*

            // Subscriber -> Basics/chords
            subscriber1.recieveOrderToSubscribe(topicBasicsChords + "*", subName1, subPort1);
            Thread.Sleep(2000);

            //Publisher -> Basics/chords
            publisher1.recieveOrderToPublish(topicBasicsChords, messageBasicsChords, numbOfMsgs1, frequence);
            Thread.Sleep(2000);

            // Publisher -> Basics
            publisher1.recieveOrderToPublish(topicBasics, messageBasics, numbOfMsgs1, frequence);
            Thread.Sleep(2000);

            //Subscriber - unsub-> Basic/chords
            subscriber1.recieveOrderToUnSubscribe(topicBasicsChords + "*", subPort1);
            Thread.Sleep(2000);

            //Subscriber - sub Basics
            subscriber1.recieveOrderToSubscribe(topicBasics + "*", subName1, subPort1);
            Thread.Sleep(2000);

            // Publisher -> Basics/chords
            publisher1.recieveOrderToPublish(topicBasics, messageBasics, numbOfMsgs1, frequence);
            Thread.Sleep(2000);


            // Subscriber 2 -> topic Basics
            subscriber2.recieveOrderToSubscribe(topicBasics + "*", subName2, subPort2);
            Thread.Sleep(2000);


            // Publisher -> topic Basics\Scales
            publisher1.recieveOrderToPublish(topicBasicsScales, messageBasicsScales, numbOfMsgs1, frequence);
            Thread.Sleep(2000);
            */


            /*
            publisher2.recieveOrderToPublish(topicBasicsScales, messageBasicsScales, numbOfMsgs1, frequence);
            
            // Publisher
            publisher1.recieveOrderToPublish(topicTwo, messageTwo, numbOfMsgs1, frequence);
            Thread.Sleep(2000);

            publisher1.recieveOrderToPublish(topic, message, numbOfMsgs1, frequence);
            Thread.Sleep(2000);
            subscriber1.printRecievedMessages();
            */
           
           /*

            publisher1.recieveOrderToPublish(topic, message);
            Thread.Sleep(2000);

            subscriber1.recieveOrderToSubscribe(topic, subName1, subPort1);
            Thread.Sleep(2000);

            publisher1.recieveOrderToPublish(topic, message);
            Thread.Sleep(2000);

            subscriber2.recieveOrderToSubscribe(topic, subName2, subPort2);
            Thread.Sleep(2000);
            
            publisher1.recieveOrderToPublish(topic, message);
            Thread.Sleep(2000);
            */
           
           /*
            publisher1.recieveOrderToPublish(topic, message);
            Thread.Sleep(2000);

            subscriber1.recieveOrderToSubscribe(topic, subName1, subPort1);
            Thread.Sleep(2000);

            publisher1.recieveOrderToPublish(topic, message);
            Thread.Sleep(2000);
            */






            Console.WriteLine("Press <enter> to exit..");
            Console.ReadLine();

        }
    }

}
