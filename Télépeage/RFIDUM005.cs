using System;
using System.Text;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace Netronix.UM005
{
    class RFIDUM005
    {
        Gadgeteer.Modules.GHIElectronics.RS232 m_rs232;//Module RS232 VAR

        byte[] readData;//Donnée lu
        public bool isCardDetected = false;
        public string badgeID;//ID de la carte scanné

        public RFIDUM005(Gadgeteer.Modules.GHIElectronics.RS232 rs232)
        {

            m_rs232 = rs232;
            Debug.Print("RS232 Initialization.");
            //On configure le port r232
            m_rs232.Configure(9600, GT.SocketInterfaces.SerialParity.None, GT.SocketInterfaces.SerialStopBits.One, 8, GT.SocketInterfaces.HardwareFlowControl.NotRequired);
            m_rs232.Port.Open();//On ouvre le port rs232

            if (isCardDetected == false)//Si la carte est pas detecté
                m_rs232.Port.DataReceived += Port_DataReceived;//Evenement emit lors de la reception des donnees, on attend un flux de données
            else
                Debug.Print("isCardDetected must be false wait before trying again");
        }

        //fonction executé une fois qu'on a capturer un flux de donnée
        void Port_DataReceived(GT.SocketInterfaces.Serial sender)
        {
            //Debug.Print("RS232 COM OPEN : " + m_rs232.Port.IsOpen.ToString());
            Debug.Print("\nLes donnes sont reçus !");
            Debug.Print("Byte to Read : " + sender.BytesToRead);
            isCardDetected = true;//La carte a été detecté
            readData = new byte[13];//Un badge comporte 11 caracteres on initialise un tableau de 13 byte pour etre large
            sender.Read(readData, 0, readData.Length);//On lit les donnée et les ajoute a notre tableau
            getBadgeID(readData);//On appel notre fonction
            readData = null;
            m_rs232.Port.DiscardInBuffer();
        }

        //Fonction qui va recuperer l'ID de la carte en scrutant le tableau
        void getBadgeID(byte[] data)
        {
            badgeID = null;
            
            Debug.Print("Output :");
            for (int i=3; i < 8 ; i++)
            {
                badgeID += data[i].ToString("X2");
                //Debug.Print(data[i].ToString("X2"));
            }
            Debug.Print("ID du badge :" + badgeID);
        }
    }
}
