using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using Toolbox.NETMF;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT.Net.NetworkInformation;

using Netronix.UM005;//Class RFID

namespace Télépeage
{
    public class Network
    {
        Gadgeteer.Modules.GHIElectronics.EthernetJ11D m_ethernetJ11D;

        /*Adresse du script avec les champs.
        Donne quelques chose comme vous pouvez testé dans votre navigateur : dtecht.000webhostapp.com/scripts/dataManager.php?&querytype=insert&badge_id=1900BB9B58&pass=cHJvamV0dGVsZXBlYWdl*/
        private string scriptUrl = "http://dtecht.000webhostapp.com/scripts/dataManager.php?";
        private string pass = Tools.Base64Encode("projettelepeage");//Mot de passe projettelepeage codé en base64 évite qu'on le mette en clair

        public bool isClientValid = false;//Si le client est valid donc badge non expiré

        /*Constructeur d'initialisation du module ethernet
        *@param Le module Ethernet J11D
        * */
        public Network(Gadgeteer.Modules.GHIElectronics.EthernetJ11D ethernetJ11D)
        {
            m_ethernetJ11D = ethernetJ11D;

            Debug.Print("EthernetJ11D Initialization.");
            //ethernetJ11D.UseThisNetworkInterface(); Dosen't work with J11D
            m_ethernetJ11D.NetworkInterface.Open();
            m_ethernetJ11D.NetworkInterface.EnableDhcp();
            m_ethernetJ11D.NetworkInterface.EnableDynamicDns();
            Thread.Sleep(1000);//pause de 1s
            ListNetworkInterfaces();//Debug
            //insertClientData(); Call the upload script test
        }


        /*Fonction qui liste tout les paramètre du module Ethernet (adresse ip, massque sous réseau..etc) a titre informatifs
        *@param Le module Ethernet J11D
        * */
        private void ListNetworkInterfaces()
        {
            var settings = m_ethernetJ11D.NetworkSettings;

            Debug.Print("------------------------------------------------");
            //Debug.Print("MAC: " + settings.PhysicalAddress);
            Debug.Print("IP Address:   " + settings.IPAddress);
            Debug.Print("DHCP Enabled: " + settings.IsDhcpEnabled);
            //Debug.Print("DNS :  " + settings.DnsAddresses);
            Debug.Print("Subnet Mask:  " + settings.SubnetMask);
            Debug.Print("Gateway:      " + settings.GatewayAddress);
            Debug.Print("Network Connection :  " + m_ethernetJ11D.IsNetworkConnected);
            Debug.Print("Cable Connected :  " + m_ethernetJ11D.NetworkInterface.CableConnected);
            Debug.Print("Network Available :  " + m_ethernetJ11D.NetworkInterface.NetworkAvailable);
            Debug.Print("------------------------------------------------");
        }


        /**Fonction principale
        * */
        public bool isBadgeValid(string badge_id)
        {
            if (m_ethernetJ11D.IsNetworkConnected)
            {
                insertClientData(badge_id);//On insert deja la date et l'heure de passage.
                isClientValid = false;
                Thread.Sleep(500);
                compareClientData(badge_id);//On compare la date d'expiration

                if (isClientValid == true)
                    return true;
                else
                    return false;
            }
            else
                Debug.Print("Vérifier que vous etes connecté a un réseau");

            return false;//Default usecase on ne passe pas
        }

        
        /*Fonction qui va call notre script php pour insert la date et l'heure de pasage dans la bdd
        * */
        private void insertClientData(string badge_id)
        {
            scriptUrl = "http://dtecht.000webhostapp.com/scripts/dataManager.php?";

            if (badge_id != null && badge_id.Length != 0)
            {
                scriptUrl += "querytype=insert";
                scriptUrl += "&badge_id=" + badge_id;

                scriptUrl += "&pass=" + pass;//Champ mot de passe
                Debug.Print("URL:"+scriptUrl);
                string result = makeRequest(scriptUrl);
                if (Utils.Contains(result, "<i class=\"fa fa-database fa-3x\" aria-hidden=\"true\"></i><i class=\"fa fa-check fa-2x\" aria-hidden=\"true\"></i>"))
                {
                    Debug.Print("La date et l'heure de passage a bien été ajouté a la base de donnée");
                }
            }
            else
                Debug.Print("Insert query can't be called when badge_id is null or empty");
        }

        /*Fonction qui va appelé notre scripts php pour savoir si l'utilisateur est autorisé a passer, il compare la date dexpiration a la date d'ajd
         ***/
        private void compareClientData(string badge_id)
        {
            scriptUrl = "http://dtecht.000webhostapp.com/scripts/dataManager.php?";
            if (badge_id != null && badge_id.Length != 0)
            {
                scriptUrl += "querytype=compare";
                scriptUrl += "&badge_id=" + badge_id;

                scriptUrl += "&pass=" + pass;//Champ mot de passe 

                string result = makeRequest(scriptUrl);
                if (Utils.Contains(result, "Votre badge est toujours valide."))
                {
                    isClientValid = true;
                }
                else if (Utils.Contains(result, "Votre badge est expire veuillez renouveler votre abonnement."))
                {
                    isClientValid = false;
                    //Debug.Print("--Votre badge est expire veuillez renouveler votre abonnement.");
                }
                //Si c'est le dernier jour de validité
                else if (Utils.Contains(result, "Dernier jour de validité avant que votre badge expire."))
                {
                    isClientValid = true;
                    //Debug.Print("--Dernier jour de validité avant que votre badge expire.");
                }
            }
            else
                Debug.Print("Comapre query can't be called when badge_id is null");
        }

        //Fonction qui va envoyé une requete a une adresse précise et va renvoyé une chaine de caractère qui est la reponse (la page du site)
        private static string makeRequest(string url)
        {
            string content = null;
            var request = WebRequest.Create(url) as HttpWebRequest;
            if (request != null)
                using (var response = request.GetResponse() as HttpWebResponse)
                using (var responseStream = response.GetResponseStream())
                {
                    var buffer = new byte[responseStream.Length];
                    responseStream.Read(buffer, 0, int.Parse(responseStream.Length.ToString()));
                    content = new String(Encoding.UTF8.GetChars(buffer));
                }
            request.Dispose();
            request.KeepAlive = false;
            Debug.Print(content);
            return content;
        }
    }

    //Classe de fonctions utiles 
    public static class Utils
    {
        //Fonction qui verifie si une methode commence par une chaine donnée en parametre
        public static bool StartsWith(this string s, string value)
        {
            return s.IndexOf(value) == 0;
        }

        //Fonction qui verifie si une chaine de caractere contient une chaine donnée en parametre
        public static bool Contains(this string s, string value)
        {
            return s.IndexOf(value) >= 0;
        }
    }

}
