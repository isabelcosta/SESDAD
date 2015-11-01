using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using SESDADInterfaces;

namespace SESDAD
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*
            string message = "Existem 7 notas musicais";
            string topic = "Musica";

            string messageTwo = "As ovelhas têm lã";
            string topicTwo = "Ovelhas";

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

            */

            //Initialize PuppetMaster GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PuppetMasterForm());
        }
    }
}
