﻿using System;
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
            pubProcess1.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Publisher\bin\Debug\Publisher.exe";
            pubProcess1.StartInfo.Arguments = pubName1 + " " + pubPort1;
            pubProcess1.Start();


            Process subProcess1 = new Process();
            subProcess1.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Subscriber\bin\Debug\Subscriber.exe";
            subProcess1.StartInfo.Arguments = subName1 + " " + subPort1;
            subProcess1.Start();

            Process broProcess1 = new Process();
            broProcess1.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Broker\bin\Debug\Broker.exe";
            broProcess1.StartInfo.Arguments = broName1 + " " + broPort1;
            broProcess1.Start();


            /*
            
                2nd Node S2, P2, B2

            */
            Process pubProcess2 = new Process();
            pubProcess2.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Publisher\bin\Debug\Publisher.exe";
            pubProcess2.StartInfo.Arguments = pubName2 + " " + pubPort2;
            pubProcess2.Start();


            Process subProcess2 = new Process();
            subProcess2.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Subscriber\bin\Debug\Subscriber.exe";
            subProcess2.StartInfo.Arguments = subName2 + " " + subPort2;
            subProcess2.Start();

            Process broProcess2 = new Process();
            broProcess2.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Broker\bin\Debug\Broker.exe";
            broProcess2.StartInfo.Arguments = broName2 + " " + broPort2;
            broProcess2.Start();



            /*
                get remote Objects
            */

            /*
                1st Node
            */
            Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort1 + "/" + pubName1);
            PublisherInterface publisher1 =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + pubPort1 + "/" + pubName1);


            Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort1 + "/" + subName1);
            SubscriberInterface subscriber1 =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + subPort1 + "/" + subName1);


            Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort1 + "/" + broName1);
            BrokerInterface broker1 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + broPort1 + "/" + broName1);

            /*
                2nd Node
            */
            Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort2 + "/" + pubName2);
            PublisherInterface publisher2 =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + pubPort2 + "/" + pubName2);


            Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort2 + "/" + subName2);
            SubscriberInterface subscriber2 =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + subPort2 + "/" + subName2);



            Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort2 + "/" + broName2);
            BrokerInterface broker2 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + broPort2 + "/" + broName2);


            // Network configuration
            publisher1.registerLocalBroker(broName1, broPort1);
            publisher1.giveName(pubName1);
            subscriber1.registerLocalBroker(broName1, broPort1);
            broker1.addPublisher(pubName1, pubPort1);
            broker1.addSubscriber(subName1, subPort1);
            broker1.addBroker(broName2, broPort2, "sonL");



            publisher2.registerLocalBroker(broName2, broPort2);
            publisher1.giveName(pubName2);

            subscriber2.registerLocalBroker(broName2, broPort2);
            broker2.addPublisher(pubName2, pubPort2);
            broker2.addSubscriber(subName2, subPort2);

            broker2.addBroker(broName1, broPort1, "parent");

            // Publisher -> Basics/chords

            publisher1.recieveOrderToPublish(topicBasicsChords, numbOfMsgs5, frequence);
            publisher2.recieveOrderToPublish(topicBasicsScales, numbOfMsgs5, frequence - 1000);
            subscriber1.recieveOrderToSubscribe(topicBasicsChords + "*", subName1, subPort1);
            Thread.Sleep(2000);



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
