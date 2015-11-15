using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;

using SESDADInterfaces;
using System.Diagnostics;

namespace SESDAD
{
    class PuppetMaster
    {
        [STAThread]
        static void Main(string[] args)
        {
            string nameBro0 = "broker0";
            int portBro0 = 3330;

            Process procBro0 = new Process();
            procBro0.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Broker\bin\Debug\Broker.exe";
            procBro0.StartInfo.Arguments = portBro0.ToString();
            procBro0.Start();

            Console.WriteLine("Broker " + nameBro0);
            BrokerInterface broker0 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + portBro0 + "/broker");

            //---------------------------------------------------------------------------------------------------------------------------------------------

            string nameBro1 = "broker1";
            int portBro1 = 3331;

            Process procBro1 = new Process();
            procBro1.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Broker\bin\Debug\Broker.exe";
            procBro1.StartInfo.Arguments = portBro1.ToString();
            procBro1.Start();

            Console.WriteLine("Broker " + nameBro1);
            BrokerInterface broker1 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + portBro1 + "/broker");

            //---------------------------------------------------------------------------------------------------------------------------------------------

            string nameBro2 = "broker2";
            int portBro2 = 3332;

            Process procBro2 = new Process();
            procBro2.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Broker\bin\Debug\Broker.exe";
            procBro2.StartInfo.Arguments = portBro2.ToString();
            procBro2.Start();

            Console.WriteLine("Broker " + nameBro2);
            BrokerInterface broker2 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + portBro2 + "/broker");



            //---------------------------------------------------------------------------------------------------------------------------------------------

            string nameBro3 = "broker3";
            int portBro3 = 3333;

            Process procBro3 = new Process();
            procBro3.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Broker\bin\Debug\Broker.exe";
            procBro3.StartInfo.Arguments = portBro3.ToString();
            procBro3.Start();

            Console.WriteLine("Broker " + nameBro3);
            BrokerInterface broker3 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + portBro3 + "/broker");



            //---------------------------------------------------------------------------------------------------------------------------------------------

            string nameBro4 = "broker4";
            int portBro4 = 3334;

            Process procBro4 = new Process();
            procBro4.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Broker\bin\Debug\Broker.exe";
            procBro4.StartInfo.Arguments = portBro4.ToString();
            procBro4.Start();


            Console.WriteLine("Broker " + nameBro4);
            BrokerInterface broker4 =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + portBro4 + "/broker");
            //---------------------------------------------------------------------------------------------------------------------------------------------


            string namePub00 = "publisher00";
            int portPub00 = 2220;


            Process procPub00 = new Process();
            procPub00.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Publisher\bin\Debug\Publisher.exe";
            procPub00.StartInfo.Arguments = portPub00.ToString();
            procPub00.Start();


            Console.WriteLine("Publisher " + namePub00);
            PublisherInterface publisher00 =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + portPub00 + "/pub");


            //---------------------------------------------------------------------------------------------------------------------------------------------

            string namePub01 = "publisher01";
            int portPub01 = 2221;


            Process procPub01 = new Process();
            procPub01.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Publisher\bin\Debug\Publisher.exe";
            procPub01.StartInfo.Arguments = portPub01.ToString();
            procPub01.Start();


            Console.WriteLine("Publisher " + namePub01);
            PublisherInterface publisher01 =
               (PublisherInterface)Activator.GetObject(
                      typeof(PublisherInterface), "tcp://localhost:" + portPub01 + "/pub");


            //---------------------------------------------------------------------------------------------------------------------------------------------


            string nameSub1 = "subscriber1";
            int portSub1 = 1111;


            Process procSub1 = new Process();
            procSub1.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Subscriber\bin\Debug\Subscriber.exe";
            procSub1.StartInfo.Arguments = portSub1.ToString();
            procSub1.Start();


            Console.WriteLine("Subscriber " + nameSub1);
            SubscriberInterface subscriber1 =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + portSub1 + "/sub");

            //---------------------------------------------------------------------------------------------------------------------------------------------



            string nameSub2 = "subscriber2";
            int portSub2 = 1112;

            Process procSub2 = new Process();
            procSub2.StartInfo.FileName = @"C:\Users\vicente\Documents\GitHubVisualStudio\SESDAD\Subscriber\bin\Debug\Subscriber.exe";
            procSub2.StartInfo.Arguments = portSub2.ToString();
            procSub2.Start();


            Console.WriteLine("Subsriber " + nameSub1);
            SubscriberInterface subscriber2 =
               (SubscriberInterface)Activator.GetObject(
                      typeof(SubscriberInterface), "tcp://localhost:" + portSub2 + "/sub");




            broker0.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            broker0.addPublisher(2220);
            broker0.addBroker(3331, "localhost", "sonL");
            broker0.addBroker(3332, "localhost", "sonR");
            broker0.giveInfo(nameBro0, portBro0);

            broker1.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            broker1.addPublisher(2221);
            broker1.addBroker(3330, "localhost", "parent");
            broker1.giveInfo(nameBro1, portBro1);


            broker2.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            broker2.addBroker(3330, "localhost", "parent");
            broker2.addBroker(3333, "localhost", "sonL");
            broker2.addBroker(3334, "localhost", "sonR");
            broker2.giveInfo(nameBro2, portBro2);



            broker3.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            broker3.addBroker(3332, "localhost", "parent");
            broker3.addSubscriber(1111);
            broker3.giveInfo(nameBro3, portBro3);


            broker4.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            broker4.addBroker(3332, "localhost", "parent");
            broker4.addSubscriber(1112);
            broker4.giveInfo(nameBro4, portBro4);


            publisher00.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            publisher00.registerLocalBroker(3330);
            publisher00.giveInfo(namePub00, portPub00);


            
            publisher01.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            publisher01.registerLocalBroker(3331);
            publisher01.giveInfo(namePub01, portPub01);

            subscriber1.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            subscriber1.registerLocalBroker(3333);
            subscriber1.giveInfo(nameSub1, portSub1);



            subscriber2.policies(RoutingPolicyType.FLOODING, OrderingType.NO, LoggingLevelType.FULL);
            subscriber2.registerLocalBroker(3334);
            subscriber2.giveInfo(nameSub2, portSub2);

            /*Comandos*/


            subscriber1.receiveOrderToSubscribe(@"/p00-0");
            subscriber1.receiveOrderToSubscribe(@"/p00-1");
            subscriber1.receiveOrderToSubscribe(@"/p01-1");
            subscriber2.receiveOrderToSubscribe(@"/p00-1");
            subscriber2.receiveOrderToSubscribe(@"/p01-0");
            subscriber2.receiveOrderToSubscribe(@"/p01-1");

            publisher00.receiveOrderToPublish(@"/p00-0", 120, 500);
            publisher00.receiveOrderToPublish(@"/p00-1", 120, 500);
            publisher01.receiveOrderToPublish(@"/p01-0", 120, 500);
            publisher01.receiveOrderToPublish(@"/p01-1", 120, 500);

            Thread.Sleep(30000);

            subscriber1.status();
            subscriber2.status();

            publisher00.status();
            publisher01.status();

            broker0.status();
            broker1.status();
            broker2.status();
            broker3.status();
            broker4.status();

            subscriber1.receiveOrderToUnSubscribe(@"p00-0");
            subscriber2.receiveOrderToUnSubscribe(@"p01-0");

            Thread.Sleep(3000);

            subscriber1.status();
            subscriber2.status();

            publisher00.status();
            publisher01.status();

            broker0.status();
            broker1.status();
            broker2.status();
            broker3.status();
            broker4.status();


            /*

        broker0 is broker On site0 URL tcp://localhost:3330/broker
Process publisher00 is publisher On site0 URL tcp://localhost:2220/pub
Process broker1 is broker On site1 URL tcp://localhost:3331/broker
Process publisher01 is publisher On site1 URL tcp://localhost:2221/pub
Process broker2 is broker On site2 URL tcp://localhost:3332/broker
Process broker3 is broker On site3 URL tcp://localhost:3333/broker
Process subscriber1 is subscriber On site3 URL tcp://localhost:1111/sub
Process broker4 is broker On site4 URL tcp://localhost:3334/broker
Process subscriber2
        */


            /*
                Simple Test - Network Topology

                 
                       B1(P1,S1) 
                      /                  
                B2(P2,S2)
            */


            /*
            
                1st Node S1, P1, B1

            //*/
            //Process pubProcess1 = new Process();
            //pubProcess1.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Publisher\bin\Debug\Publisher.exe";
            //pubProcess1.StartInfo.Arguments = pubPort1.ToString();
            //pubProcess1.Start();


            //Process subProcess1 = new Process();
            //subProcess1.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Subscriber\bin\Debug\Subscriber.exe";
            //subProcess1.StartInfo.Arguments = subPort1.ToString();
            //subProcess1.Start();

            //Process broProcess1 = new Process();
            //broProcess1.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Broker\bin\Debug\Broker.exe";
            //broProcess1.StartInfo.Arguments = broPort1.ToString();
            //broProcess1.Start();


            ///*

            //    2nd Node S2, P2, B2

            //*/
            //Process pubProcess2 = new Process();
            //pubProcess2.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Publisher\bin\Debug\Publisher.exe";
            //pubProcess2.StartInfo.Arguments = pubPort2.ToString();
            //pubProcess2.Start();


            //Process subProcess2 = new Process();
            //subProcess2.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Subscriber\bin\Debug\Subscriber.exe";
            //subProcess2.StartInfo.Arguments = subPort2.ToString();
            //subProcess2.Start();

            //Process broProcess2 = new Process();
            //broProcess2.StartInfo.FileName = @"C:\Users\Francisco Caixeiro\Desktop\SESDAD\Broker\bin\Debug\Broker.exe";
            //broProcess2.StartInfo.Arguments = broPort2.ToString();
            //broProcess2.Start();



            /*
                get remote Objects
            */

            ///*
            //    1st Node
            //*/
            //Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort1 + "/pub");
            //PublisherInterface publisher1 =
            //   (PublisherInterface)Activator.GetObject(
            //          typeof(PublisherInterface), "tcp://localhost:" + pubPort1 + "/pub");


            //Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort1 + "/sub");
            //SubscriberInterface subscriber1 =
            //   (SubscriberInterface)Activator.GetObject(
            //          typeof(SubscriberInterface), "tcp://localhost:" + subPort1 + "/sub");


            //Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort1 + "/broker");
            //BrokerInterface broker1 =
            //   (BrokerInterface)Activator.GetObject(
            //          typeof(BrokerInterface), "tcp://localhost:" + broPort1 + "/broker");


            ///*
            //    2nd Node
            //*/
            //Console.WriteLine("Publisher acessivel em: " + "tcp://localhost:" + pubPort2 + "/pub");
            //PublisherInterface publisher2 =
            //   (PublisherInterface)Activator.GetObject(
            //          typeof(PublisherInterface), "tcp://localhost:" + pubPort2 + "/pub");


            //Console.WriteLine("Subscriber acessivel em: " + "tcp://localhost:" + subPort2 + "/sub");
            //SubscriberInterface subscriber2 =
            //   (SubscriberInterface)Activator.GetObject(
            //          typeof(SubscriberInterface), "tcp://localhost:" + subPort2 + "/sub");



            //Console.WriteLine("Broker acessivel em: " + "tcp://localhost:" + broPort2 + "/broker");
            //BrokerInterface broker2 =
            //   (BrokerInterface)Activator.GetObject(
            //          typeof(BrokerInterface), "tcp://localhost:" + broPort2 + "/broker");


            // Network configuration
            //publisher1.registerLocalBroker(broPort1);
            //publisher1.giveInfo(pubName1, pubPort1);
            //subscriber1.registerLocalBroker(broPort1);
            //subscriber1.giveInfo(subName1, subPort1);
            //broker1.addPublisher(pubPort1);
            //broker1.addSubscriber(subPort1);
            //broker1.addBroker(broPort2, "localhost", "sonL");
            //broker1.policies(RoutingPolicyType.FILTER, OrderingType.FIFO, LoggingLevelType.LIGHT);
            //subscriber1.policies(RoutingPolicyType.FILTER, OrderingType.FIFO, LoggingLevelType.LIGHT);
            //publisher1.policies(RoutingPolicyType.FILTER, OrderingType.FIFO, LoggingLevelType.LIGHT);



            //publisher2.registerLocalBroker(broPort2);
            //publisher2.giveInfo(pubName2, pubPort2);
            //subscriber2.registerLocalBroker(broPort2);
            //subscriber2.giveInfo(subName2, subPort2);
            //broker2.addPublisher(pubPort2);
            //broker2.addSubscriber(subPort2);
            //broker2.addBroker(broPort1, "localhost", "parent");
            //broker2.policies(RoutingPolicyType.FILTER, OrderingType.FIFO, LoggingLevelType.LIGHT);
            //subscriber2.policies(RoutingPolicyType.FILTER, OrderingType.FIFO, LoggingLevelType.LIGHT);
            //publisher2.policies(RoutingPolicyType.FILTER, OrderingType.FIFO, LoggingLevelType.LIGHT);

            //// Publisher -> Basics/chords


            //publisher1.receiveOrderToPublish(topicBasicsChords, numbOfMsgs5, frequence);
            //publisher2.receiveOrderToPublish(topicBasicsScales, numbOfMsgs5, frequence - 1000);

            //subscriber1.receiveOrderToSubscribe(topicBasicsChords + "*");
            //Thread.Sleep(2000);




            Console.WriteLine("Press <enter> to exit..");
            Console.ReadLine();

        }
    }

}