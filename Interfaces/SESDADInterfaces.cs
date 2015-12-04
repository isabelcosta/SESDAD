using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SESDADInterfaces
{
    //**********************************************************************
    //                              CONSTANTS
    //**********************************************************************

    public class RoutingPolicyType
    {
        public const string FILTER = "filter";
        public const string FLOODING = "flooding";
    }

    public class OrderingType
    {
        public const string NO = "NO";
        public const string TOTAL = "TOTAL";
        public const string FIFO = "FIFO";
    }

    public class LoggingLevelType
    {
        public const string FULL = "full";
        public const string LIGHT = "light";
    }

    public class ProcessType
    {
        public const string BROKER = "broker";
        public const string PUBLISHER = "publisher";
        public const string SUBSCRIBER = "subscriber";
    }

    public class BrokerNeighbours
    {
        public const string SONL = "sonL";
        public const string SONR = "sonR";
        public const string PARENT = "parent";
    }

    public class seqNumber
    {
        private ConcurrentDictionary<string, int> seqN = new ConcurrentDictionary<string, int>();

        public seqNumber()
        {
            seqN.TryAdd("order", 1);
        }

        public void increaseSeqN()
        {
            seqN["order"] += 1;
        }

        public int getSeqN()
        {
            return seqN["order"];
        }
    }

    public interface PuppetInterface
    {
        void receiveOrderToCrash(string processName);
        void receiveOrderToFreeze(string processName);
        void receiveOrderToUnfreeze(string processName);
        void receiveOrderToPublish(string processName, string topic, int numberOfEvents, int interval_x_ms);
        void receiveOrderToSubscribe(string processName, string topic);
        void receiveOrderToUnsubscribe(string processName, string topic);
        void receiveOrderToShowStatus();
        void informAction(string action);
        void slaveIsReady();
        int getNumberOfSlaves();
        void slavesAreUp();
        bool areAllSlavesUp();
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
        void receiveOrderToPublish(string topic, int numeberOfEvents, int interval_x_ms);

        // network config
        void registerLocalBroker(int Brokerport);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string name, int port);

        void status();

        void setFreezeState(bool isFrozen);
    }

    public interface SubscriberInterface
    {
        void receiveOrderToSubscribe(string topic);

        void receiveOrderToUnSubscribe(string topic);

        // network config
        void registerLocalBroker(int Brokerport);

        void printReceivedMessages();

        void Callback(object sender, MessageArgs m);

        // network config
        void registerLocalPuppetMaster(string name, int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string name, int port);

        void status();

        void setFreezeState(bool isFrozen);
    }

    public interface BrokerInterface
    {
        void receiveOrderToFlood(string topic, string message, string ip, int port);

        void totalOrderFlood(string topic, string message);

        void subscribeRequest(string topic, int port);

        void unSubscribeRequest(string topic, int port);

        void filterSubscription(string topic, string ip, int port);

        void filterUnsubscription(string topic, string ip, int port);

        // network config
        void addSubscriber(int port);

        // network config
        void addPublisher(int port);

        // network config
        void addBroker(int port, string ip, string relation);

        void addRootBroker(int port, string ip);

        // network config
        void registerLocalPuppetMaster(int port);

        // network config
        void policies(string routing, string ordering, string logging);

        // network config
        void giveInfo(string ip, int port, string name);

        void status();

        void setFreezeState(bool isFrozen);
    }

    //**************************************************************************
    //From http://stackoverflow.com/questions/71257/suspend-process-in-c-sharp
    //**************************************************************************

    public static class ProcessExtension
    {
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        public static void Suspend(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                SuspendThread(pOpenThread);
            }
        }
        public static void Resume(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                ResumeThread(pOpenThread);
            }
        }
    }
}
