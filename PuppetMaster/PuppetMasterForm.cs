﻿using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using SESDADInterfaces;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace SESDAD
{

    public partial class PuppetMasterForm : Form
    {

        //*********************************************************************
        //                              ATTRIBUTES
        //*********************************************************************
        private String routingPolicy = RoutingPolicyType.FLOODING;
        private String ordering = OrderingType.FIFO;
        private String loggingLevel = LoggingLevelType.LIGHT;

        private bool singlePuppetMode = false;

        private String puppetURL = null; //tcp://localhost:30000/puppet
        private int puppetID = 0; //indexes the URLs in configPuppet

        private IDictionary<string, Process> LocalProcesses = new Dictionary<string, Process>(); //Array with processes <processName, process>
        private IDictionary<string, string> myBrokerinfo = new Dictionary<string, string>();

        //Only PuppetMaster's use, to access the Puppet Master Slaves
        private IDictionary<string, int> slavesProcesses  = new Dictionary<string, int>(); //Hashtable please <processName, slaveURL>
        private IDictionary<int, string> slavesRemoteObjects = new Dictionary<int, string>(); //Hashtable please <processName, slaveURL>




        /// <summary>
        /// Puppet Master Constructor
        /// </summary>
        /// <param name="args"></param>
        public PuppetMasterForm(String[] args)
        {
            //Builds the Puppet Master GUI
            InitializeComponent();
            
            //Check if the system should run with a single Puppet Master for testing purposes
            try { 
                if (String.Compare("-singlepuppet", args[1].ToLower()) == 0) {
                    singlePuppetMode = true; 
                }
            } catch (Exception) { }
            //MessageBox.Show("This is " + (singlePuppetMode ? "single" : "multiple") + " PuppetMaster mode");
            
            String configPuppetPath = Environment.CurrentDirectory + @"\..\..\..\configPuppet.txt";
            
            this.puppetID = Int32.Parse(args[0]);
            //MessageBox.Show(configPuppetPath);
            this.puppetURL = System.IO.File.ReadAllLines(configPuppetPath)[puppetID];

            int puppetPort = 30000 + puppetID;
            
            //Single Mode: PuppetMaster reads and processes all processes
            //Multiple Mode: Each Puppet Master/Slave processes its processes 
            readConfigFile();
            
            //regista o servico de puppet
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = puppetPort;
            TcpChannel channel = new TcpChannel(props, null, provider);
            ChannelServices.RegisterChannel(channel, false);

            /*
            PuppetServices servicos = new PuppetServices();
            RemotingServices.Marshal(servicos, "Puppet - " + puppetID,
                typeof(PuppetServices));*/
        }

        //************************************************************************************
        //***********************          Handle buttons        *****************************
        //************************************************************************************

        //Button Run Single Command method - runs a single command and cleans the text box
        private void bt_Command_Click(object sender, EventArgs e)
        {
            String singleCommand = tb_Command.Text;
            
            if (singleCommand == "") {
                    return;
            }

            //erases command after clicking the button
            tb_Command.Clear();

            String[] blankSpace = { " " };
            String[] parsedLine = singleCommand.Split(blankSpace, StringSplitOptions.None);

            processCommand(parsedLine);
        }

        //Button Run Script method - run every line of script and cleans the text box
        private void bt_Script_Click(object sender, EventArgs e)
        {
            String[] scriptLines = tb_Script.Lines;

            try {
                if (scriptLines[0] == "")
                {
                    return;
                }
            } catch (IndexOutOfRangeException) {
                return;
            }
            
            //erases command after clicking the button
            tb_Script.Clear();

            String[] blankSpace = { " " };
            String[] parsedLine = null;
            
            foreach (String line in scriptLines) {
                parsedLine = line.Split(blankSpace, StringSplitOptions.None);

                switch (parsedLine[0]) {
                    case "Wait":
                        int msToSleep = 0;
                        try {
                            msToSleep = Int32.Parse(parsedLine[1]);
                        }
                        catch (FormatException) { break; }
                        //Thread.Sleep(msToSleep);
                        break;

                    default:
                        processCommand(parsedLine);
                        break;
                }
            }
        }

        private void processCommand(String[] parsedLine)
        {
            switch (parsedLine[0])
            {
                case "Subscriber":
                    if (parsedLine[2] == "Subscribe")
                    {
                        //MessageBox.Show(parsedLine[1] + " sub " + parsedLine[3]);
                        //subscribe(parsed[1], parsed[3]);//1 - process; 3 - topic
                    }
                    else if (parsedLine[2] == "Unsubscribe")
                    {
                        //MessageBox.Show(parsed[1] + " unsub " + parsed[3]);
                        //unsubscribe(parsed[1], parsed[3]);
                    }
                    break;
                case "Publisher":
                    //publish(parsed[1], parsed[3], parsed[5], parsed[7]); //1 - process; 3 - nEvents; 5 - topic; 7 - interval;
                    break;
                case "Status":
                    status();
                    //send to all nodes status request
                    break;
                case "Crash":
                    crash(parsedLine[1]);
                    addMessageToLog("Crash " + parsedLine[1]);
                    break;
                case "Freeze":
                    freeze(parsedLine[1]);
                    addMessageToLog("Freeze " + parsedLine[1]);
                    break;
                case "Unfreeze":
                    unfreeze(parsedLine[1]);
                    addMessageToLog("Unfreeze " + parsedLine[1]);
                    break;
                default: break;
            }
        }

        //************************************************************************************
        //***************          Initial configuration functions        ********************
        //************************************************************************************

        //Reads and process the configFile.txt
        private void readConfigFile() {

            String configFilePath = Environment.CurrentDirectory + @"\..\..\..\configFile.txt";
            
            String[] lines = System.IO.File.ReadAllLines(configFilePath);
            
            foreach (string line in lines)
            {
                processConfigFileLines(line);
            }
        }

        private void processConfigFileLines(String line) {
            String[] blankSpace = { " " };
            String[] parsed = line.Split(blankSpace, StringSplitOptions.None);

            switch (parsed[0])
            {
                case "Site": //Site sitename Parent sitename|none
                    if (String.Compare("site" + this.puppetID, parsed[1]) == 0)
                    { //se eu for um filho logo tenho o pai do meu broker
                        myBrokerinfo[BrokerNeighbours.PARENT] = parsed[3];
                    }
           
                    else if (String.Compare("site" + this.puppetID, parsed[3]) == 0) { //primeiro defino o filho SonL depois o SonR
                        if (myBrokerinfo.ContainsKey(BrokerNeighbours.SONL)) {
                            myBrokerinfo.Add(BrokerNeighbours.SONR, parsed[1]);
                        }
                        else {
                            myBrokerinfo.Add(BrokerNeighbours.SONL, parsed[1]);
                        }
                    }
                    break;

                case "Process": //Process processname Is publisher|subscriber|broker On sitename URL process-url REFATORIZAR REFATORIZAR REFATORIZAR
                    
                    string URL = parsed[7];
                    Process process = null;

                    if (String.Compare("site" + this.puppetID, parsed[5]) != 0) { //se não pertence ao meu site nao me interessa
                        break;
                    }

                    if (String.Compare(parsed[3], ProcessType.BROKER) == 0) {
                        /*BrokerInterface broker =
                            (BrokerInterface)Activator.GetObject(
                                typeof(BrokerInterface), ProcessURL);*/

                        process = startProcess(parsed[1], ProcessType.BROKER, processPortFromURL(URL) + " " + myBrokerinfo[BrokerNeighbours.PARENT] + " " + myBrokerinfo[BrokerNeighbours.SONR] + " " + myBrokerinfo[BrokerNeighbours.SONL]); //parente SonR SonL
                        if (process == null) {
                            addMessageToLog("Couldn't start " + parsed[1]);
                        }
                        addMessageToLog("Broker " + parsed[1] + " at " + URL);

                    } else if (String.Compare(parsed[3], ProcessType.PUBLISHER) == 0) {
                        /*PublisherInterface publisher =
                            (PublisherInterface)Activator.GetObject(
                                typeof(PublisherInterface), ProcessURL);*/
                        
                        process = startProcess(parsed[1], ProcessType.PUBLISHER, processPortFromURL(URL));
                        if (process == null) {
                            addMessageToLog("Couldn't start " + parsed[1]);
                        }
                        addMessageToLog("Publisher " + parsed[1] + " at " + URL);

                    } else if (String.Compare(parsed[3], ProcessType.SUBSCRIBER) == 0) {
                        /*SubscriberInterface subscriber =
                            (SubscriberInterface)Activator.GetObject(
                                typeof(SubscriberInterface), ProcessURL);*/
                        
                        process = startProcess(parsed[1], ProcessType.SUBSCRIBER, processPortFromURL(URL));
                        if (process == null) {
                            addMessageToLog("Couldn't start " + parsed[1]);
                        }
                        addMessageToLog("Subscriber " + parsed[1] + " at " + URL);
                    }
                    break;

                case "RoutingPolicy": //RoutingPolicy flooding|filter
                    this.routingPolicy = parsed[1]; break;

                case "Ordering": //Ordering NO|FIFO|TOTAL
                    this.ordering = parsed[1]; break;

                case "LoggingLevel"://LoggingLevel full|light
                    this.loggingLevel = parsed[1]; break;

                default:
                    break;
            }
        }

        /********************************************/
        //          START A SINGLE PROCESS
        /*******************************************/
        public Process startProcess(string processName, string processType, string args) {

            string processTypeDirectory = processType.First().ToString().ToUpper() + processType.Substring(1);
            string processPath = Environment.CurrentDirectory.Replace("PuppetMaster", processTypeDirectory); //Broker | Subscriber | Publisher
            
            processPath += @"\" + processType + ".exe";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = processPath;
            startInfo.Arguments = args;
            Process process = null;

            try
            {
                process = Process.Start(startInfo);
                LocalProcesses.Add(processName, process); //have to give bro, pub and sub arguments
            }
            catch (Exception e)
            {
                MessageBox.Show(startInfo.FileName);
                MessageBox.Show(e.Message);
            }

            return process;
        }

        public void crash(String processName) {

            if (LocalProcesses.ContainsKey(processName))
            {
                LocalProcesses[processName].Kill();
            }
            else {
                return;
            }
        }

        public void status()
        {

        }

        public void freeze(string processName)
        {
            //send a sleep thread request to process or tells responsible puppetmaster slave to do it
            //suspend(process)
        }

        public void unfreeze(string processName)
        {
            //resume(process)
        }

        public void addMessageToLog(string message) {
            
            tb_Log.AppendText(message);
            tb_Log.AppendText(Environment.NewLine);
        }

        private String processPortFromURL(string url) { //melhorar codigo - returns port and processType
            String[] spliter = { ":" };

            String[] portAndProcess = url.Split(spliter, StringSplitOptions.None);
            String[] spliter2 = { @"/" };

            String port = portAndProcess[2].Split(spliter2, StringSplitOptions.None)[0];

            return port;
        }


    }

    //************************************************************************************
    //************************          Puppet Delegates       ***************************
    //************************************************************************************
    delegate void DelCrashProcess(string processName);
    delegate Process DelStartProcess(string processName, string processType, string args);

    class PuppetServices : MarshalByRefObject, PuppetInterface
    {

        public static PuppetMasterForm form;

        public PuppetServices()
        {

        }

        public void recieveOrderToCrash(string processName)
        {
            // thread-safe access to form
            form.Invoke(new DelCrashProcess(form.crash), processName);
        }

        //public void receiveOrderToStartProcess(string processName, string processType, string args) {
        //    form.Invoke(new DelStartProcess(form.startProcess), processName, processType, args);
        //}

        public void registerSuperPuppetMaster()
        {
            String configPuppetPath = Environment.CurrentDirectory + @"\configPuppet.txt";

            String[] puppetsLocation = System.IO.File.ReadAllLines(configPuppetPath);

            //Console.WriteLine("Broker local registado no Publisher: " + "tcp://localhost:" + BrokerPort + "/" + BrokerName);
            PuppetInterface superPuppetMaster =
               (PuppetInterface)Activator.GetObject(
                      typeof(PuppetInterface), puppetsLocation[0]);
        }
        
        public void receiveOrderToFreeze(string processName) {

        }

        public void receiveOrderToUnfreeze(string processName) {

        }

        public void receiveOrderToCrash(string processName) { }
        public void receiveOrderToPublish(string processName) { } //mais cenas
        public void receiveOrderToSubscribe(string processName) { } //mais cenas
        public void receiveOrderToUnsubscribe(string processName) { } //mais cenas
        public void receiveOrderToShowStatus(string processName) { }
        public void sendLogsToMaster(string logInfo) { }
        public void informMyMaster(string logInfo) { }
    }

}