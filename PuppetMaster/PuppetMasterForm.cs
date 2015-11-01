using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SESDADInterfaces;

namespace SESDAD
{
    public class RoutingPolicyType {
        public const string FILTER = "filter";
        public const string FLOODING = "flooding";
    }

    public class OrderingType {
        public const string NO = "NO";
        public const string TOTAL = "TOTAL";
        public const string FIFO = "FIFO";
    }

    public class LoggingLevelType {
        public const string FULL = "FULL";
        public const string LIGHT = "LIGHT";
    }

    public class ProcessType
    {
        public const string BROKER = "broker";
        public const string PUBLISHER = "publisher";
        public const string SUBSCRIBER = "subscriber";
    }

    public partial class PuppetMasterForm : Form
    {

        //***************** Attributes **********************
        String RoutingPolicy = RoutingPolicyType.FLOODING;
        String Ordering = null;
        String LoggingLevel = null;

 
        //Array with processes

        public PuppetMasterForm()
        {
            InitializeComponent();
            //readConfigFile();
        }

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
                        //parsedLine[1].Sleep(2000); //process to wait == sleep
                        //MessageBox.Show("Waiting");
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
                case "Status": status(); break;
                case "Crash": crash(parsedLine[1]); break;
                case "Freeze": freeze(parsedLine[1]); break;
                case "Unfreeze": unfreeze(parsedLine[1]); break;
                default: break;
            }
        }

        //Reads and process the configFile.txt
        private void readConfigFile() {
            //change to file destination
            String[] lines = System.IO.File.ReadAllLines(@"C:\Users\Isabel\Source\Repos\SESDAD\PuppetMaster\configFile.txt");
            
            foreach (String line in lines)
            {
                processConfigFileLines(line);
            }
            //MessageBox.Show(RoutingPolicy + " " + Ordering + " " + LoggingLevel);
        }

        private void processConfigFileLines(String line) {
            String[] blankSpace = { " " };
            String[] parsed = line.Split(blankSpace, StringSplitOptions.None);

            switch (parsed[0])
            {
                case "Site":
                    break;
                case "Process":

                    string[] portAndProcess = processURL(parsed[7]);
                    string ProcessURL = "tcp://localhost:" + portAndProcess[0] + "/" + portAndProcess[1];
                    addMessageToLog("Publisher acessivel em: " + ProcessURL);

                    if (String.Compare(parsed[3], ProcessType.BROKER) == 0) {
                        BrokerInterface broker =
                            (BrokerInterface)Activator.GetObject(
                                typeof(BrokerInterface), ProcessURL);
                    } else if (String.Compare(parsed[3], ProcessType.PUBLISHER) == 0) {

                        PublisherInterface publisher =
                            (PublisherInterface)Activator.GetObject(
                                typeof(PublisherInterface), ProcessURL);

                    } else if (String.Compare(parsed[3], ProcessType.SUBSCRIBER) == 0) {
                        SubscriberInterface subscriber =
                            (SubscriberInterface)Activator.GetObject(
                                typeof(SubscriberInterface), ProcessURL);
                    }
                    break;
                case "RoutingPolicy":
                    this.RoutingPolicy = parsed[1]; break;
                case "Ordering":
                    this.Ordering = parsed[1]; break;
                case "LoggingLevel":
                    this.LoggingLevel = parsed[1]; break;
                default:
                    break;
            }
        }

        private void crash(String processName) {
            /*
            Process[] processes = null;
			processes = Process.GetProcessesByName(processName);

			foreach (Process process in processes)
			{
				process.Kill();
			}

            //System.Diagnostics.Process.GetProcessesByName("csrss")[0].Kill();
            */
        }

        private void status()
        {

        }

        private void freeze(string processName)
        {

        }

        private void unfreeze(string processName)
        {

        }

        private void addMessageToLog(string message) {
            
            tb_Log.AppendText(message);
            tb_Log.AppendText(Environment.NewLine);
        }

        private String[] processURL(string url) {
            String[] spliter = { ":" };

            String[] portAndProcess = url.Split(spliter, StringSplitOptions.None);
            String [] spliter2 = { @"/" };

            return portAndProcess[2].Split(spliter2, StringSplitOptions.None);
        }
    }

}