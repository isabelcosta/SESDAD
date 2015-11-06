using System;
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
        private int slaves = 0;

        private String puppetURL = null; //tcp://localhost:30000/puppet
        private int puppetID = 0; //indexes the URLs in configPuppet

        //for both PuppetMaster and PuppetSlaves
        private IDictionary<string, Process> LocalProcesses = new Dictionary<string, Process>(); //Array with processes <processName, process>
        private IDictionary<string, Tuple<int, PublisherInterface>> myPubs = new Dictionary<string, Tuple<int, PublisherInterface>>(); //<processName, PublisherInterface>
        private IDictionary<string, Tuple<int, SubscriberInterface>> mySubs = new Dictionary<string, Tuple<int, SubscriberInterface>>(); //<processName, SubscriberInterface>
        private BrokerInterface myBroker = null;
        private int myBrokerPort = 0;
        private IDictionary<string, string> myBrokerinfo = new Dictionary<string, string>();

        //Only PuppetMaster's use, to access the Puppet Master Slaves
        private IDictionary<string, int> slavesProcesses  = new Dictionary<string, int>(); //Hashtable please <processName, slaveURL>
        private IDictionary<int, PuppetInterface> slavesRemoteObjects = new Dictionary<int, PuppetInterface>(); //Hashtable please <processName, slaveURL>

        //Only to slave
        private PuppetInterface puppetMasterRemote = null;


        /// <summary>
        /// Puppet Master Constructor
        /// </summary>
        /// <param name="args"></param>
        public PuppetMasterForm(String[] args)
        {
            try {
                this.puppetID = int.Parse(args[0]);
            } catch {
                MessageBox.Show("Puppet Master doesn't understand his ID... \nI'll exit for you. Try again!");
                Environment.Exit(0);
            }
            
            //Builds the Puppet Master GUI
            InitializeComponent();
            this.tb_Command.ReadOnly = !isMaster();
            this.tb_Script.ReadOnly = !isMaster();
            this.Text = "Puppet " + (isMaster() ? "Master" : "Slave") + " on site" + puppetID.ToString();

            if (this.isMaster())
            {
                if (args.Length > 1)
                {
                    try
                    {
                        this.slaves = int.Parse(args[1]);
                    }
                    catch
                    {
                        //Check if the system should run with a single Puppet Master for testing purposes
                        if (String.Compare("-singlepuppet", args[1].ToLower()) == 0)
                        {
                            singlePuppetMode = true;
                        }
                        else
                        {
                            MessageBox.Show("Wrong Arguments... Try Again...");
                            Environment.Exit(0);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Puppet Master has to know how much slaves he has... Try again!");
                    Environment.Exit(0);
                }
            }
            //MessageBox.Show("This is " + (singlePuppetMode ? "single" : "multiple") + " PuppetMaster mode");
            
            String configPuppetPath = Environment.CurrentDirectory + @"\..\..\..\configPuppet.txt";
            
            String[] allPuppetURL = System.IO.File.ReadAllLines(configPuppetPath);
            this.puppetURL = allPuppetURL[puppetID];

            int puppetPort = 30000 + puppetID;
            
            //Single Mode: PuppetMaster reads and processes all processes
            //Multiple Mode: Each Puppet Master/Slave processes its processes 
            this.readConfigFile();
            
            PuppetServices.form = this;

            if (!isMaster())
            {
                //slave will save puppetMasterRemoteObject for remote communications
                puppetMasterRemote =
                        (PuppetInterface)Activator.GetObject(
                            typeof(PuppetInterface),
                            allPuppetURL[0]
                        );
                puppetMasterRemote.slaveIsReady(); //avisa o Master que ja iniciou os seus processos

                PuppetInterface me = (PuppetInterface)Activator.GetObject(
                            typeof(PuppetInterface),
                            allPuppetURL[puppetID]
                        );

                while (!me.areAllSlavesUp())
                {
                    Thread.Sleep(2000);
                }
            }

            //Being the puppetMaster, he stores the puppetSlaves remote objects, when they are all up
            if (this.isMaster()) {
                PuppetInterface me =
                        (PuppetInterface)Activator.GetObject(
                            typeof(PuppetInterface),
                            allPuppetURL[0]
                        );

                while (me.getNumberOfSlaves() != slaves) {
                    Thread.Sleep(2000);
                }

                for (int puppetIndex = 1; puppetIndex <= this.slaves; puppetIndex++) {

                    PuppetInterface slave =
                        (PuppetInterface)Activator.GetObject(
                            typeof(PuppetInterface),
                            allPuppetURL[puppetIndex]
                        );
                    slavesRemoteObjects.Add(puppetIndex, slave);
                    slave.slavesAreUp();
                }
                
            }


            //configuring network
            this.addNeighboursToMyBroker(); //TODO: acho q pode ficar fora dos dois if's

            PublisherInterface pubI;
            SubscriberInterface subI;
            int pubPort, subPort;

            foreach (KeyValuePair<string, Tuple<int, PublisherInterface>> entry in myPubs)
            {
                pubI = entry.Value.Item2;
                pubPort = entry.Value.Item1;
                pubI.registerLocalBroker(myBrokerPort);
                pubI.registerLocalPuppetMaster(entry.Key, puppetPort);
                pubI.policies(this.routingPolicy, this.ordering, this.loggingLevel);
                myBroker.addPublisher(pubPort);

            }
            foreach (KeyValuePair<string, Tuple<int, SubscriberInterface>> entry in mySubs)
            {
                subI = entry.Value.Item2;
                subPort = entry.Value.Item1;
                subI.registerLocalBroker(myBrokerPort);
                subI.registerLocalPuppetMaster(entry.Key, puppetPort);
                subI.policies(this.routingPolicy, this.ordering, this.loggingLevel);
                myBroker.addSubscriber(subPort);
            }
            myBroker.registerLocalPuppetMaster(puppetPort);
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
                        int timeToSleep = 0;
                        try {
                            timeToSleep = Int32.Parse(parsedLine[1]);
                            Thread.Sleep(timeToSleep);
                        }
                        catch (FormatException) { break; }
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

        private void addNeighboursToMyBroker()
        {
            String configPuppetPath = Environment.CurrentDirectory + @"\..\..\..\configPuppet.txt";
            
            String[] lines = System.IO.File.ReadAllLines(configPuppetPath);

            String[] blankSpace = { " " };

            foreach (string line in lines)
            {
                String[] parsed = line.Split(blankSpace, StringSplitOptions.None);

                if (String.Compare(parsed[0], "Process") == 0)
                {
                    string brokerPort = portAndIpFromURL(parsed[7])[0];
                    string brokerIp = portAndIpFromURL(parsed[7])[1];

                    if (String.Compare(parsed[5], myBrokerinfo[BrokerNeighbours.PARENT]) == 0) //se o broker for pai do meu broker
                    {
                        myBroker.addBroker(int.Parse(brokerPort), brokerIp, BrokerNeighbours.PARENT);
                    }
                    else if (String.Compare(parsed[5], myBrokerinfo[BrokerNeighbours.SONR]) == 0) //se o broker for filho direito do meu broker
                    {
                        myBroker.addBroker(int.Parse(brokerPort), brokerIp, BrokerNeighbours.SONR);
                    }
                    else if (String.Compare(parsed[5], myBrokerinfo[BrokerNeighbours.SONL]) == 0) //se o broker for filho esquerdo do meu broker
                    {
                        myBroker.addBroker(int.Parse(brokerPort), brokerIp, BrokerNeighbours.SONL);
                    }
                }
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
                    string processPort = portAndIpFromURL(URL)[0];
                    string processIp = portAndIpFromURL(URL)[1];

                    if (this.isMaster()) {
                        string slaveID = parsed[5].Replace("site", ""); //removes the "site" from string
                        //MessageBox.Show(slaveID);
                        slavesProcesses.Add(parsed[1], int.Parse(slaveID)); //processName -> slaveID
                    }

                    if (String.Compare(parsed[3], ProcessType.BROKER) == 0) {

                        if (String.Compare("site" + this.puppetID, parsed[5]) == 0) //se for o meu broker
                        {
                            process = startProcess(parsed[1], ProcessType.BROKER, processPort);
                            if (process == null) {
                                addMessageToLog("Couldn't start " + parsed[1]);
                            }
                            addMessageToLog("Broker " + parsed[1] + " at " + URL);

                            BrokerInterface bro = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), URL);
                            myBroker = bro;
                            myBrokerPort = int.Parse(processPort);
                        }

                    } else if (String.Compare(parsed[3], ProcessType.PUBLISHER) == 0) {
                        if (String.Compare("site" + this.puppetID, parsed[5]) != 0)
                        { //se não pertence ao meu site nao me interessa
                            break;
                        }

                        process = startProcess(parsed[1], ProcessType.PUBLISHER, processPort);
                        if (process == null) {
                            addMessageToLog("Couldn't start " + parsed[1]);
                        }
                        addMessageToLog("Publisher " + parsed[1] + " at " + URL);
                        PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), URL);
                        myPubs.Add(parsed[1], new Tuple<int, PublisherInterface>(int.Parse(processPort), publisher));

                    } else if (String.Compare(parsed[3], ProcessType.SUBSCRIBER) == 0) {
                        if (String.Compare("site" + this.puppetID, parsed[5]) != 0)
                        { //se não pertence ao meu site nao me interessa
                            break;
                        }

                        process = startProcess(parsed[1], ProcessType.SUBSCRIBER, processPort);
                        if (process == null) {
                            addMessageToLog("Couldn't start " + parsed[1]);
                        }
                        addMessageToLog("Subscriber " + parsed[1] + " at " + URL);
                        SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), URL);
                        mySubs.Add(parsed[1], new Tuple<int, SubscriberInterface>(int.Parse(processPort), subscriber));

                    }
                    break;

                case "RoutingPolicy": // flooding|filter
                    this.routingPolicy = parsed[1]; break;

                case "Ordering": // NO|FIFO|TOTAL
                    this.ordering = parsed[1]; break;

                case "LoggingLevel":// full|light
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
            //MessageBox.Show(puppetID.ToString());
            if (isMaster())
            {
                if (LocalProcesses.ContainsKey(processName))
                {
                    LocalProcesses[processName].Kill();
                }
                else {
                    slavesRemoteObjects[slavesProcesses[processName]].receiveOrderToCrash(processName);
                }
            }
            else {
                if (LocalProcesses.ContainsKey(processName))
                {
                    LocalProcesses[processName].Kill();
                }
                else { return; } // TALVEZ DESNECESSARIO
            }  
        }

        public void status()
        {

        }

        public void freeze(string processName)
        {
            //send a sleep thread request to process or tells responsible puppetmaster slave to do it
            //suspend(process)
            if (isMaster())
            {
                slavesRemoteObjects[slavesProcesses[processName]].receiveOrderToFreeze(processName);
            }
            else
            {
                if (LocalProcesses.ContainsKey(processName))
                {
                    //LocalProcesses[processName].Suspend();
                }
                else { return; } // TALVEZ DESNECESSARIO
            }
        }

        public void unfreeze(string processName)
        {
            //resume(process)
            if (isMaster())
            {
                slavesRemoteObjects[slavesProcesses[processName]].receiveOrderToUnfreeze(processName);
            }
            else
            {
                if (LocalProcesses.ContainsKey(processName))
                {
                    //LocalProcesses[processName].ResumeThread();
                }
                else { return; } // TALVEZ DESNECESSARIO
            }
        }

        public void addMessageToLog(string message) {
            
            tb_Log.AppendText(message);
            tb_Log.AppendText(Environment.NewLine);
        }

        private String[] portAndIpFromURL(string url) { //melhorar codigo - returns port and processType
            String[] spliter = { ":" };

            String[] portAndProcess = url.Split(spliter, StringSplitOptions.None);
            String[] spliter2 = { @"/" };

            String port = portAndProcess[2].Split(spliter2, StringSplitOptions.None)[0];
            String ip = portAndProcess[1];

            String[] portAndIpFromURL = {port, ip};
            return portAndIpFromURL;
        }

        private bool isMaster() {
            return puppetID == 0;
        }
    }

    //************************************************************************************
    //************************          Puppet Delegates       ***************************
    //************************************************************************************
    delegate void DelCrashProcess(string processName);
    delegate void DelFreezeProcess(string processName);
    delegate void DelUnfreezeProcess(string processName);
    delegate Process DelStartProcess(string processName, string processType, string args);

    class PuppetServices : MarshalByRefObject, PuppetInterface
    {
        public static PuppetMasterForm form;

        //for PuppetMaster
        int numberOfSlaves = 0;

        //for PuppetSlave
        bool allSlavesAreUp = false;

        public PuppetServices()
        {

        }

        public void receiveOrderToCrash(string processName)
        {
            // thread-safe access to form
            form.Invoke(new DelCrashProcess(form.crash), processName);
        }

        public void receiveOrderToFreeze(string processName) {
            // thread-safe access to form
            form.Invoke(new DelFreezeProcess(form.freeze), processName);
        }

        public void receiveOrderToUnfreeze(string processName) {
            // thread-safe access to form
            form.Invoke(new DelUnfreezeProcess(form.unfreeze), processName);
        }

        public void registerSuperPuppetMaster()
        {
            String configPuppetPath = Environment.CurrentDirectory + @"\configPuppet.txt";

            String[] puppetsLocation = System.IO.File.ReadAllLines(configPuppetPath);

            //Console.WriteLine("Broker local registado no Publisher: " + "tcp://localhost:" + BrokerPort + "/" + BrokerName);
            PuppetInterface superPuppetMaster =
               (PuppetInterface)Activator.GetObject(
                      typeof(PuppetInterface), puppetsLocation[0]);
        }
        
        
        public void receiveOrderToPublish(string processName) { } //mais cenas
        public void receiveOrderToSubscribe(string processName) { } //mais cenas
        public void receiveOrderToUnsubscribe(string processName) { } //mais cenas
        public void receiveOrderToShowStatus(string processName) { }
        public void sendLogsToMaster(string logInfo) { }
        public void informAction(string action) { }

        public void slaveIsReady()
        {
            numberOfSlaves++;
        }

        public void slavesAreUp()
        {
            allSlavesAreUp = true;
        }

        public int getNumberOfSlaves()
        {
            return numberOfSlaves;
        }

        public bool areAllSlavesUp()
        {
            return allSlavesAreUp;
        }
    }

}