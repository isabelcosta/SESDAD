using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Collections.Generic;
using SESDADInterfaces;
using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters;

namespace SESDAD
{

    class Publisher
    {
        [STAThread]

        static void Main(string[] args)
        {

            int publisherPort = Int32.Parse(args[0]);


            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = publisherPort;
            TcpChannel channel = new TcpChannel(props, null, provider);


            //TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PublisherServices), "pub",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Publisher...");
            System.Console.ReadLine();
        }
    }

    class seqNumber
    {
        private int seqN;
        public seqNumber()
        {
            seqN = 1;
        }
        
        public int SeqN
        {
            get { return seqN; }
            set { seqN = value; }
        }
    }

    [Serializable]
    class PublisherServices: MarshalByRefObject, PublisherInterface
    {


        BrokerInterface localBroker;
        PuppetInterface localPuppetMaster;

        /* Policies*/
        string routing;
        string ordering;
        string logging;

        /*Publisher Info*/
        string myName;
        int myPort;

        bool freezeFlag = false;
        private List<Tuple<string, List<string>>> myFrozenOrders = new List<Tuple<string, List<string>>>();

        private seqNumber seqNb = new seqNumber();
        
        
        
        ConcurrentDictionary<string, int> topicsPublishing = new ConcurrentDictionary<string, int>();
        
        /*
        message
            */


        public void registerLocalPuppetMaster(string name, int port)
        {
            var t = new Thread(() => RealregisterLocalPuppetMaster(name, port));
            t.Start();
        }

        public void RealregisterLocalPuppetMaster(string name, int port)
        {
            Console.WriteLine("PuppetMasterLocal adicionado " + port);
            PuppetInterface puppetMaster = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), "tcp://localhost:" + port + "/puppet");
            localPuppetMaster = puppetMaster;
        }

        public void receiveOrderToPublish(string topic, int numberOfEvents, int interval_x_ms)
        {
            topicsPublishing.TryAdd(topic, 0);
            if (this.amIFrozen()) {
                List<string> args = new List<string>();
                args.Add(topic);
                args.Add(numberOfEvents.ToString());
                args.Add(interval_x_ms.ToString());
                myFrozenOrders.Add(new Tuple<string, List<string>>(PublisherOrders.PUBLISH, args));
            } else {
                var t = new Thread(() => RealreceiveOrderToPublish(topic, numberOfEvents, interval_x_ms));
                t.Start();
            }
        }

        public void RealreceiveOrderToPublish(string topic, int numberOfEvents, int interval_x_ms)
        {
            // Formato da mensagem : PubName SeqNumber/Total

            string content;

            //Fifo Testing
            // content = myName + " " + 2 + "/" + numberOfEvents;
            // localBroker.receiveOrderToFlood(topic, content, myName, myPort);
            // content = myName + " " + 3 + "/" + numberOfEvents;
            // localBroker.receiveOrderToFlood(topic, content, myName, myPort);
            // content = myName + " " + 7 + "/" + numberOfEvents;
            // localBroker.receiveOrderToFlood(topic, content, myName, myPort);
            // content = myName + " " + 8 + "/" + numberOfEvents;
            // localBroker.receiveOrderToFlood(topic, content, myName, myPort);
            // Thread.Sleep(5000);
            
            

            for (int i = 1; i <= numberOfEvents; i++)
            {
                //check
                lock (seqNb)
                {
                    content = myName + " " + seqNb.SeqN + "/" + numberOfEvents;
                    seqNb.SeqN += 1;

                    int num;
                    if (topicsPublishing.TryGetValue(topic, out num))
                    {
                        topicsPublishing[topic] += 1;
                    }
                }
                                                
                                            // Exe: Publisher1 1/10
                                            // localBroker fica a null de vez em quando
                localBroker.receiveOrderToFlood(topic, content, myName, myPort);

                string action = "PubEvent - " + myName + " publishes " + topic + " : " + content; //TODO: as mensagens vao como PubEvent certo?
                informPuppetMaster(action);
                
                //Console.WriteLine(action);
                Thread.Sleep(interval_x_ms);
            }
            int temp;
            topicsPublishing.TryRemove(topic, out temp);

        }


        public void registerLocalBroker(int BrokerPort)
        {
            var t = new Thread(() => RealregisterLocalBroker(BrokerPort));
            t.Start();
        }

        public void RealregisterLocalBroker(int BrokerPort)
        {
            Console.WriteLine("Broker local registado no Publisher: " + "tcp://localhost:" + BrokerPort + "/broker");
            localBroker =
               (BrokerInterface)Activator.GetObject(
                      typeof(BrokerInterface), "tcp://localhost:" + BrokerPort + "/broker");
        }

        public void status()
        {
            if (this.amIFrozen()) {
                //Console.WriteLine("---------------- ya tou FROZEN e agora?");
                List <string> args = new List<string>();
                myFrozenOrders.Add(new Tuple<string, List<string>>(PublisherOrders.STATUS, args));
            } else {
                //Console.WriteLine("---------------- ya agora tou LIVRE e agora?");
                var t = new Thread(() => Realstatus());
                t.Start();
            }
        }

        public void Realstatus()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(".---------------- Status ----------------.");
            Console.WriteLine("|");

            if (topicsPublishing.Count == 0)
            {
                Console.WriteLine("| " + myName + " has not published any topics..");
            }
            else
            {
                Console.WriteLine("| ..Published..");
                foreach (KeyValuePair<string, int> pair in topicsPublishing)
                {
                    Console.WriteLine("|   -" + pair.Key + " -> " + pair.Value + " messages sent");
                }
                
            }
            Console.WriteLine("| ");
            Console.WriteLine(".----------------------------------------.");
            Console.WriteLine("");
        }

        private void informPuppetMaster(string action)
        {
            var t = new Thread(() => RealinformPuppetMaster(action));
            t.Start();
        }
        private void RealinformPuppetMaster(string action)
        {
            localPuppetMaster.informAction(action);
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

        private bool amIFrozen()
        {
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
                    case PublisherOrders.PUBLISH:
                        this.receiveOrderToPublish(args[0], int.Parse(args[1]), int.Parse(args[2]));
                        break;
                    case PublisherOrders.STATUS:
                        this.status();
                        break;
                }
            }
            this.myFrozenOrders.Clear();
        }
    }


}
