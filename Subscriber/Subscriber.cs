﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;

using SESDADInterfaces;
using System.Runtime.Serialization.Formatters;

namespace SESDAD
{
    class Subscriber
    {
        [STAThread]
        static void Main(string[] args)
        {
            int subscriberPort = Int32.Parse(args[0]);

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = subscriberPort;
            TcpChannel channel = new TcpChannel(props, null, provider);

            //  TcpChannel channel = new TcpChannel(8087);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(SubscriberServices), "sub",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Subscriber...");
            System.Console.ReadLine();
        }
    }

    [Serializable]
    class SubscriberServices : MarshalByRefObject, SubscriberInterface
    {
        BrokerInterface localBroker;
        PuppetInterface localPuppetMaster;

        /* Policies*/
        string routing;
        string ordering;
        string logging;

        int myPort;
        string myName;

        bool freezeFlag = false;
        private List<Tuple<string, List<string>>> myFrozenOrders = new List<Tuple<string, List<string>>>();

        List<string> subscriptions = new List<string>();
        List<Tuple<string, string>> messages = new List<Tuple<string, string>>();
        ConcurrentDictionary<string, int> messagesReceived = new ConcurrentDictionary<string, int>();
        /*
        Thread Methods
            */


        public void receiveOrderToSubscribe(string topic)
        {
            if (this.amIFrozen())
            {
                List<string> args = new List<string>();
                args.Add(topic);
                myFrozenOrders.Add(new Tuple<string, List<string>>(SubscriberOrders.SUBSCRIBE, args));
            } else {
                var t = new Thread(() => RealreceiveOrderToSubscribe(topic));
                t.Start();
            }
        }

        //invocado pelo PuppetMaster para subscrever a um topico e informar o local broker
        public void RealreceiveOrderToSubscribe(string topic)
        {
            if (topic == null || topic.Equals(""))
                throw new Exception("topic is empty");

            //adicionar as subscricoes a lista
            subscriptions.Add(topic);

            //informar o local broker que subscreveu
            localBroker.subscribeRequest(topic, myPort);
            //string action = "Subscribed to " + topic;

            //informPuppetMaster(action);
            //Console.WriteLine(action);
        }

        public void receiveOrderToUnSubscribe(string topic)
        {
            if (this.amIFrozen())
            {
                List<string> args = new List<string>();
                args.Add(topic);
                myFrozenOrders.Add(new Tuple<string, List<string>>(SubscriberOrders.UNSUBSCRIBE, args));
            } else {
                var t = new Thread(() => RealreceiveOrderToUnSubscribe(topic));
                t.Start();
            }
        }

        public void RealreceiveOrderToUnSubscribe(string topic)
        {
            //adicionar as subscricoes a lista
            subscriptions.Remove(topic);

            //informar o local broker que subscreveu
            localBroker.unSubscribeRequest(topic, myPort);

            //string action = "Unsubscribed to " + topic;
            //informPuppetMaster(action);
            //Console.WriteLine(action);

        }

        public void Callback(object sender, MessageArgs m)
        {
            var t = new Thread(() => RealCallback(sender, m));
            t.Start();
        }

        public void RealCallback(object sender, MessageArgs m)
        {
            string action = "SubEvent - " + this.myName + " received " + m.Topic + " : " + m.Body;
            informPuppetMaster(action);
            if (messagesReceived.ContainsKey(m.Topic))
            {
                messagesReceived[m.Topic]++;
            }
            else
            {
                messagesReceived.TryAdd(m.Topic, 1);
            }
            //Console.WriteLine(action);

            //messages.Add(new Tuple<string, string>(m.Topic, m.Body));
        }

        public void registerLocalBroker(int brokerPort)
        {
            var t = new Thread(() => RealregisterLocalBroker(brokerPort));
            t.Start();
        }
        //  invocado pelo PuppetMaster para registar o broker local 
        public void RealregisterLocalBroker(int brokerPort)
        {
            Console.WriteLine("Broker local registado no Subscriber: " + "tcp://localhost:" + brokerPort + "/broker");
            localBroker =
                (BrokerInterface)Activator.GetObject(
                       typeof(BrokerInterface), "tcp://localhost:" + brokerPort + "/broker");

        }

        public void printReceivedMessages()
        {
            foreach (Tuple<string, string> msg in messages)
            {
                Console.WriteLine("--");
                Console.WriteLine("Topic: {0}", msg.Item1);
                Console.WriteLine("Message: {0}", msg.Item2);
            }
            Console.WriteLine("--");
        }

        public void status()
        {
            if (this.amIFrozen())
            {
                List<string> args = new List<string>();
                myFrozenOrders.Add(new Tuple<string, List<string>>(SubscriberOrders.STATUS, args));
            } else {
                var t = new Thread(() => Realstatus());
                t.Start();
            }
        }
        public void Realstatus()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(".---------------- Status ----------------.");
            Console.WriteLine("| ");
            Console.WriteLine("| ..Topics Subscribed..");

            foreach (KeyValuePair<string, int> pair in messagesReceived)
            {
                Console.WriteLine("|    - " + pair.Key + " -> " + pair.Value + " messages received");
            }

            Console.WriteLine("|");
            Console.WriteLine(".----------------------------------------.");
            Console.WriteLine("");

        }

        public void registerLocalPuppetMaster(string name, int port)
        {
            var t = new Thread(() => RealregisterLocalPuppetMaster(name, port));
            t.Start();
        }

        public void RealregisterLocalPuppetMaster(string name, int port)
        {
            Console.WriteLine("PuppetMasterLocal adicionado " + port);
            this.myName = name;
            PuppetInterface puppetMaster = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), "tcp://localhost:" + port + "/puppet");
            localPuppetMaster = puppetMaster;
        }

        private void informPuppetMaster(string action)
        {
            //if (string.Compare(logging, LoggingLevelType.FULL) == 0)
            //{
            localPuppetMaster.informAction(action);
            //}
        }
        public void policies(string routing, string ordering, string logging)
        {
            var t = new Thread(() => Realpolicies(routing, ordering, logging));
            t.Start();
        }


        public void Realpolicies(string routing, string ordering, string logging)
        {
            this.routing = routing;
            this.ordering = ordering;
            this.logging = logging;
        }

        public void giveInfo(string name, int port)
        {
            var t = new Thread(() => RealgiveInfo(name, port));
            t.Start();
        }

        public void RealgiveInfo(string name, int port)
        {
            this.myPort = port;
            this.myName = name;
        }

        private bool amIFrozen() {
            return this.freezeFlag;
        }

        public void setFreezeState(bool isFrozen)
        {
            this.freezeFlag = isFrozen;

            if (!this.freezeFlag)
            {
                this.executeAllFrozenCommands();
            }
        }

        private void executeAllFrozenCommands()
        {
            List<string> args = null;
            foreach (Tuple<string, List<string>> order in myFrozenOrders)
            {
                args = order.Item2;
                switch (order.Item1)
                {
                    case SubscriberOrders.SUBSCRIBE:
                        this.receiveOrderToSubscribe(args[0]);
                        break;
                    case SubscriberOrders.UNSUBSCRIBE:
                        this.receiveOrderToUnSubscribe(args[0]);
                        break;
                    case SubscriberOrders.STATUS:
                        this.status();
                        break;
                }
            }
            this.myFrozenOrders.Clear();
        }
    }
}
