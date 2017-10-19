using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using GHI.Processor;
using GHI.Networking;
using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

using Netronix.UM005;

//Coded with 💗 by Jordan Nijean visit jnijean.herokuapp.com

namespace Télépeage
{
    public partial class Program
    {
        Display display;
        Network network;
        LED led;
        Servo servo;
        RFIDUM005 rfid;

        // This method is run when the mainboard is powered up or reset.
        void ProgramStarted()
        {
            Debug.Print("Program Started");

            //Initialisation des composants
            display = new Display(displayTE35);
            network = new Network(ethernetJ11D);
            led = new LED(multicolorLED);
            servo = new Servo();
            rfid = new RFIDUM005(rs232);

            GT.Timer whileEmulatorTimer = new GT.Timer(700);//Timer qui va etre appelé toute les 10ms nous permet de simuler une boucle while
            whileEmulatorTimer.Tick += new GT.Timer.TickEventHandler(telepeageLogic_Tick);//On ajoute ce timer a la fonction evenemnt
            
            whileEmulatorTimer.Start();
        }

        void telepeageLogic_Tick(GT.Timer timer)
        {
            /*Logique du télépeage*/
            //display.infoMsg("Bienvenue !");
            if (rfid.isCardDetected)
            {
                //Comparer la validité du badge rfid avec la base de donnée
                 if(network.isBadgeValid(rfid.badgeID))
                 {
                   led.G_TrafficLight(multicolorLED);//Passe le feu au vert
                   servo.openFence();//Ouvre la barrière
                   display.infoMsg("Merci de votre paiment et bon voyage !");
                   Thread.Sleep(5000);//Attend 5s
                   led.Y_TrafficLight(multicolorLED);
                   Thread.Sleep(2000);//Attend 2s
                 }
                 else
                 {
                   display.infoMsg("Un probleme est survenu, veuillez attendre l'assistance.");
                 }
                 rfid.isCardDetected = false;
            }
            else
            {
                led.R_TrafficLight(multicolorLED);//Passe le feu au rouge
                servo.closeFence();//Ferme la barriere
            }
        }
    }
}
