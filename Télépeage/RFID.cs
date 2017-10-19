using System;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Collections;
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

namespace Impinj.Speedway
{ 
    public class RFID
    {
            private Socket m_socket = null;
            private ushort m_llrp_id_version_type; //version du protocole sur 2 bits, les 6 suivants sont pour le type de message
            private uint m_llrp_id_length; // longueur de la trame
            private uint m_llrp_id_id; // Un identifiant qu'on récupère dans les réponses
            private ushort m_llrp_tlv_type; //les 6 bits de poids faible identifient le type de TLV
            private ushort m_llrp_tlv_length; //	Taille du TLV en octets
            private ushort m_llrp_reponse_statusCode; //code du status, 0 Success
            private ushort m_llrp_reponse_error; // description d'erreur
            private byte[] m_llrp_tlv_restore = new byte[1];
            private byte[] m_llrp_tlv_keepalive = new byte[1];
            private uint m_llrp_tlv_timeinterval;
            private uint m_llrp_rospec_id;
            private uint m_llrp_answer_length; //taille de la trame en réponse attendue
            private bool m_connect;


            public RFID(Socket s)
            {
                m_llrp_id_id = 0;
                m_socket = s;
            }

            public RFID(string adresseIP)
            {
                m_connect = true;
                try
                {
                    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    m_socket.Connect(new IPEndPoint(IPAddress.Parse(adresseIP), 5084));
                }
                catch (SocketException e)
                {
                    Debug.Print("Source : " + e.ErrorCode);
                    Debug.Print("Message : " + e.Message);
                    m_connect = false;
                }
            }

            // Declare a Name property of type string:
            public bool isConnected
            {
                get
                {
                    return m_connect;
                }
            }

            private byte[] ConstruirellrpID(UInt16 type, UInt32 taille)
            {
                m_llrp_id_version_type = 1024; //version vaut 4
                m_llrp_id_version_type += type;
                m_llrp_id_id++;
                switch (type)
                {
                    case 3: m_llrp_id_length = 20; break; //type 3 (Set Reader Config)
                    case 20: m_llrp_id_length = 257; break; // type 20 (Add ROSpec)
                    case 21: //type 21 (Delete ROSpec)
                    case 24: m_llrp_id_length = 14; m_llrp_answer_length = 18; break; //type 24 (enable ROSpec)
                    case 1023: m_llrp_id_length = taille; break;
                }

                byte[] data = new byte[10];
                byte[] temp;

                temp = BitConverter.GetBytes(m_llrp_id_version_type);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 0, 2);
                temp = BitConverter.GetBytes(m_llrp_id_length);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 2, 4);
                temp = BitConverter.GetBytes(m_llrp_id_id);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 6, 4);

                return data;
            }

            private bool GererReponses(UInt16 type)
            {
                bool retour = false;
                byte[] temp;
                byte[] reponse = new byte[10];
                ushort version;
                byte[] reponseBadge = new byte[100];
            
            


                do
                {

                    int nbOctets = m_socket.Receive(reponse, 10, SocketFlags.None);
                    temp = new byte[2];
                    System.Buffer.BlockCopy(reponse, 0, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    version = BitConverter.ToUInt16(temp, 0);
                    version -= 1024;
                                
                } while (version != type);
                temp = new byte[4];
                System.Buffer.BlockCopy(reponse, 6, temp, 0, 4);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                uint id = BitConverter.ToUInt32(temp, 0);
                if (id == m_llrp_id_id)
                {
                    retour = true;
                }
                return retour;
            }

            private byte[] ConstruirellrpTLV(ushort type, ushort taille)
            {
                m_llrp_tlv_type = type; //les 6 bits de poids faible identifient le type de TLV
                switch (type)
                {
                    case 177: m_llrp_tlv_length = 247; break; // RO spec
                    case 178: m_llrp_tlv_length = 18; break; // RO Bound spec
                    case 179: m_llrp_tlv_length = 5; break; // RO spec Start Trigger
                    case 182: // RO spec Stop Trigger
                    case 184: m_llrp_tlv_length = 9; break; // AI spec Stop
                    case 183: m_llrp_tlv_length = 206; break; // AI spec
                    case 186: m_llrp_tlv_length = 187; break; // Inventory Parameter Spec ID
                    case 222: m_llrp_tlv_length = 90; break; // Antenna Configuration
                    case 224: m_llrp_tlv_length = 10; break; // RF Transmitter
                    case 237: m_llrp_tlv_length = 13; break; // RO Report Spec
                    case 238: m_llrp_tlv_length = 6; break; // Tag Report Content Selector
                    case 330: m_llrp_tlv_length = 74; break; // C1G2 Inventory Command
                    case 335: m_llrp_tlv_length = 8; break; // C1G2 RF Control
                    case 336: m_llrp_tlv_length = 11; break; // C1G2 Singulation Control
                    case 1023: m_llrp_tlv_length = taille; break; // Custom Parameter
                }

                byte[] data = new byte[4];
                byte[] temp;
                temp = BitConverter.GetBytes(m_llrp_tlv_type);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 0, 2);
                temp = BitConverter.GetBytes(m_llrp_tlv_length);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 2, 2);
                return data;
            }

            private byte[] ConfigurerAntenne(ushort id)
            {
                byte[] data = new byte[90];
                byte[] temp;
                ushort valeur16b;
                uint valeur32b;
                int index = 0;
                //TLV Antenna Configuration
                temp = ConstruirellrpTLV(222, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index = temp.Length;
                temp = BitConverter.GetBytes(id);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;

                //TLV RF Transmitter
                temp = ConstruirellrpTLV(224, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0;//Hop table ID: 0
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 1;//Channel index: 1
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 81;//Transmit power value: 81
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;

                //TLV Parameter: C1G2 Inventory Command
                temp = ConstruirellrpTLV(330, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 0; //0... .... = Tag inventory state aware: No
                index++;
                //TLV Parameter: C1G2 RF Control
                temp = ConstruirellrpTLV(335, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 1000; //Mode index: 1000
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0; //Tari: 0
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: C1G2 Singulation Control
                temp = ConstruirellrpTLV(336, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 128; //10.. .... = Session: 2
                index++;
                valeur16b = 32; //Tag population: 32
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 0; //Tag tranzit time: 0
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: Custom parameter (Impinj - Inventory search mode)
                temp = ConstruirellrpTLV(1023, 14);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 25882; //Vendor ID: Impinj (25882)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 23; //Impinj parameter subtype: Inventory search mode (23)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 2; //Inventory search mode: Dual target (2)
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: Custom parameter (Impinj - Fixed frequency list)
                temp = ConstruirellrpTLV(1023, 18);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 25882; //Vendor ID: Impinj (25882)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 26; //Impinj parameter subtype: Fixed frequency list (26)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 1; //Fixed frequency mode: Auto select (1)
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0; //Reserved for future use: 0000
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0; //Number of channels: 0
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: Custom parameter (Impinj - Low duty cycle)
                temp = ConstruirellrpTLV(1023, 18);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 25882; //Vendor ID: Impinj (25882)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 28; //Impinj parameter subtype: Low duty cycle (28)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0; //Low duty cycle mode: Disabled (0)
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0; //Empty field timeout: 0
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 0; //Field ping interval: 0
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;

                return data;
            }

            public bool SetReaderConfig()
            {
                bool retour = false;
                ushort valeur16b;
                byte[] reponse = new byte[18];

                m_llrp_tlv_type = 220;
                m_llrp_tlv_length = 9;

                m_llrp_tlv_restore[0] = 0;

                m_llrp_tlv_keepalive[0] = 1;
                m_llrp_tlv_timeinterval = 5000;

                byte[] temp;
                temp = ConstruirellrpID(3, 0);

                byte[] data = new byte[m_llrp_id_length];

                System.Buffer.BlockCopy(temp, 0, data, 0, 10);
                System.Buffer.BlockCopy(m_llrp_tlv_restore, 0, data, 10, 1);
                temp = BitConverter.GetBytes(m_llrp_tlv_type);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 11, 2);
                temp = BitConverter.GetBytes(m_llrp_tlv_length);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 13, 2);
                System.Buffer.BlockCopy(m_llrp_tlv_keepalive, 0, data, 15, 1);
                temp = BitConverter.GetBytes(m_llrp_tlv_timeinterval);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 16, 4);

                m_socket.Send(data);

                if (GererReponses(13))
                {
                    int nbOctets = m_socket.Receive(reponse, 8, SocketFlags.None);
                    temp = new byte[2];
                    System.Buffer.BlockCopy(reponse, 4, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    valeur16b = BitConverter.ToUInt16(temp, 0);
                    if (valeur16b == 0) retour = true;
                }
                return retour;
            }

            /*public bool SetReaderConfigResponse(uint id)
            {
                m_llrp_id_version_type = 1024; //version vaut 4
                m_llrp_id_version_type += 13; //type 3 (Set Reader Config)
                m_llrp_id_length = 18;
                m_llrp_id_id = id;

                m_llrp_tlv_type = 287;
                m_llrp_tlv_length = 8;

                m_llrp_reponse_statusCode = 0;
                m_llrp_reponse_error = 0;

                byte[] data = new byte[m_llrp_id_length];
                byte[] temp;

                temp = BitConverter.GetBytes(m_llrp_id_version_type);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 0, 2);
                temp = BitConverter.GetBytes(m_llrp_id_length);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 2, 4);
                temp = BitConverter.GetBytes(m_llrp_id_id);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 6, 4);
                temp = BitConverter.GetBytes(m_llrp_tlv_type);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 10, 2);
                temp = BitConverter.GetBytes(m_llrp_tlv_length);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 12, 2);
                temp = BitConverter.GetBytes(m_llrp_reponse_statusCode);
                System.Buffer.BlockCopy(temp, 0, data, 14, 2);
                temp = BitConverter.GetBytes(m_llrp_reponse_error);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 16, 2);

                m_socket.Send(data);

                return true;
            }*/

            public bool SupprimerROSpec()
            {
                bool retour = false;
                byte[] temp;
                temp = ConstruirellrpID(21, 0);

                byte[] data = new byte[m_llrp_id_length];
                byte[] reponse = new byte[10];


                System.Buffer.BlockCopy(temp, 0, data, 0, 10);
                m_llrp_rospec_id = 0; //tous
                temp = BitConverter.GetBytes(m_llrp_rospec_id);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 10, 4);

                m_socket.Send(data);

                if (GererReponses(31))
                {
                    int nbOctets = m_socket.Receive(reponse, 8, SocketFlags.None);

                    temp = new byte[2];
                    ushort tlv;
                    System.Buffer.BlockCopy(reponse, 0, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    tlv = BitConverter.ToUInt16(temp, 0);
                    if (tlv == 287)
                    {
                        System.Buffer.BlockCopy(reponse, 2, temp, 0, 2);
                        if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                        tlv = BitConverter.ToUInt16(temp, 0);
                        if (tlv == 8)
                        {
                            System.Buffer.BlockCopy(reponse, 4, temp, 0, 2);
                            if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                            tlv = BitConverter.ToUInt16(temp, 0);

                            if (tlv == 0) retour = true;
                        }
                    }

                }


                return retour;
            }

            public bool ActiverImpinjExtensions()
            {
                bool retour = false;
                byte[] temp;
                byte[] reponse = new byte[23];
                int index = 0;
                uint valeur32b;
                ushort valeur16b;
                temp = ConstruirellrpID(1023, 19);

                byte[] data = new byte[m_llrp_id_length];

                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 25882; //Vendor ID: Impinj (25882)
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 21; //Subtype: Enable extensions (21)
                index++;
                valeur32b = 0; //Reserved for future use: 00000000
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;

                m_socket.Send(data);

                if (GererReponses(1023))
                {
                    int nbOctets = m_socket.Receive(reponse, 13, SocketFlags.None);
                    temp = new byte[2];

                    System.Buffer.BlockCopy(reponse, 9, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    valeur16b = BitConverter.ToUInt16(temp, 0);
                    if (valeur16b == 0) retour = true;
                }
                return retour;
            }

            public bool AjouterROSpec()
            {
                bool retour = false;
                byte[] temp;
                temp = ConstruirellrpID(20, 0);
                byte[] data = new byte[m_llrp_id_length];
                int index = 0;
                ushort valeur16b;
                uint valeur32b;
                byte[] reponse = new byte[18];


                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: RO Spec
                temp = ConstruirellrpTLV(177, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur32b = 1234; //ROSpec ID: 1234
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 0; //Priority: 0
                index++;
                data[index] = 0; //Current state: 0
                index++;
                //TLV Parameter: RO Bound Spec
                temp = ConstruirellrpTLV(178, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: RO Spec Start Trigger
                temp = ConstruirellrpTLV(179, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 1; //ROSpec start trigger type: 1
                index++;
                //TLV Parameter: RO Spec Stop Trigger
                temp = ConstruirellrpTLV(182, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 0; //ROSpec stop trigger type: 0
                index++;
                valeur32b = 0; //Duration trigger value: 0
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: AI Spec
                temp = ConstruirellrpTLV(183, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 2; //Antenna count: 2
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 1; //Antenna ID: 1
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 2; //Antenna ID: 2
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: AI Spec Stop
                temp = ConstruirellrpTLV(184, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 0; //AISpec stop trigger type: 0
                index++;
                valeur32b = 0; //Duration trigger value: 0
                temp = BitConverter.GetBytes(valeur32b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: Inventory Parameter Spec ID
                temp = ConstruirellrpTLV(186, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 1234; //Inventory parameter spec id: 1234
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 1; //Protocol ID: EPCGlobal Class 1 Gen 2 (1)
                index++;
                temp = ConfigurerAntenne(1);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                temp = ConfigurerAntenne(2);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: RO Report Spec
                temp = ConstruirellrpTLV(237, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                data[index] = 2; //RO report trigger: 2
                index++;
                valeur16b = 1; //N: 1
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                //TLV Parameter: Tag Report Content Selector
                temp = ConstruirellrpTLV(238, 0);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;
                valeur16b = 7744; //tags
                temp = BitConverter.GetBytes(valeur16b);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, index, temp.Length);
                index += temp.Length;

                m_socket.Send(data);

                if (GererReponses(30))
                {
                    int nbOctets = m_socket.Receive(reponse, 8, SocketFlags.None);
                    temp = new byte[2];

                    System.Buffer.BlockCopy(reponse, 4, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    valeur16b = BitConverter.ToUInt16(temp, 0);
                    if (valeur16b == 0) retour = true;
                }
                return retour;
            }

            public bool ActiverRospec()
            {
                bool retour = false;
                byte[] temp;
                ushort valeur16b;
                byte[] reponse = new byte[39];
           
            

                temp = ConstruirellrpID(24, 0);
                byte[] data = new byte[m_llrp_id_length];

                System.Buffer.BlockCopy(temp, 0, data, 0, 10);
                m_llrp_rospec_id = 1234;
                temp = BitConverter.GetBytes(m_llrp_rospec_id);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                System.Buffer.BlockCopy(temp, 0, data, 10, 4);
                m_socket.Send(data);
                           
                if (GererReponses(34))
                {
                    int nbOctets = m_socket.Receive(reponse, 8, SocketFlags.None);
                    temp = new byte[2];

                    System.Buffer.BlockCopy(reponse, 4, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    valeur16b = BitConverter.ToUInt16(temp, 0);
                    if (valeur16b == 0) retour = true;

                }

                return retour;
            }

            public string RecupererTagRFID()
            {
                byte[] reponse = new byte[10];
                byte[] temp;
                byte[] tag = new byte[2];
                ushort valeur16b;
                uint valeur32b;
                string retour = "";

                int nbOctets = m_socket.Receive(reponse);
                temp = new byte[2];
                System.Buffer.BlockCopy(reponse, 0, temp, 0, 2);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                valeur16b = BitConverter.ToUInt16(temp, 0);
                valeur16b -= 1024;
                if (valeur16b == 61)
                {
                    temp = new byte[4];
                    System.Buffer.BlockCopy(reponse, 2, temp, 0, 4);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    valeur32b = BitConverter.ToUInt16(temp, 0);
                    reponse = new byte[valeur32b - 10];
                    nbOctets = m_socket.Receive(reponse);
                    temp = new byte[2];
                    System.Buffer.BlockCopy(reponse, 8, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    valeur16b = BitConverter.ToUInt16(temp, 0);
                    Debug.Print("Taille en bits du tag {0}", valeur16b);                
                    tag = new byte[valeur16b / 8];
                    System.Buffer.BlockCopy(reponse, 10, tag, 0, valeur16b / 8);
                    //Debug.Print("tag: ", tag);

                    retour = BitConverter.ToString(tag);
                
                }
                else if (valeur16b == 62)
                {
                    retour = "Keep Alive, pas de tag RFID";
                }

                return retour;
            }
            public void getReaderConfigReponse()
            {

                 //get reader config//
                     byte[] temp;
                     byte[] data2 = new byte[17];
                     ushort type2 = 1024 + 2;
                     uint longueur2 = 17;
                     uint ID2 = 303;
                     ushort Antenna = 0;
                     ushort gpioPort = 9;
                     byte[] badge = new byte[24];
                     byte[] answer = new byte[100];
                                  
                     temp = BitConverter.GetBytes(type2);

                     if (BitConverter.IsLittleEndian)
                     {
                         Array.Reverse(temp);
                     }
                     //placer dans le tableau d'octet
                     Buffer.BlockCopy(temp, 0, data2, 0, 2);

                     temp = BitConverter.GetBytes(longueur2);
                     if (BitConverter.IsLittleEndian)
                     {
                         Array.Reverse(temp);
                     }
                     //placer dans le tableau d'octet
                     Buffer.BlockCopy(temp, 0, data2, 2, 4);

                     temp = BitConverter.GetBytes(ID2);
                     if (BitConverter.IsLittleEndian)
                     {
                         //inverse pois fort pois faible
                         Array.Reverse(temp);
                     }
                     //placer dans le tableau d'octet
                     Buffer.BlockCopy(temp, 0, data2, 6, 4);


                     temp = BitConverter.GetBytes(Antenna);
                     Buffer.BlockCopy(temp, 0, data2, 10, 2);

                     temp = BitConverter.GetBytes(gpioPort);
                     //placer dans le tableau d'octet
                     Buffer.BlockCopy(temp, 0, data2, 12, 2);

                     m_socket.Send(data2);

                     /*reception du message de retour
                    m_socket.ReceiveFrom(answer, ref EndPoint m_socket);
                     for (int i = 0; i < 17; i++)
                     {
                         answerString += (char)answer[i];
                         Console.Write(answer[i] + " ");
                     }*/

            }

            public bool getReaderNotificationEvent()
            {
                bool retour = false;
                byte[] reponse = new byte[32];
                byte[] temp;
                ushort version;

                int nbOctets = m_socket.Receive(reponse);

                temp = new byte[2];
                System.Buffer.BlockCopy(reponse, 0, temp, 0, 2);
                if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                version = BitConverter.ToUInt16(temp, 0);
                version -= 1024;
                if (version == 63)
                {
                    System.Buffer.BlockCopy(reponse, 26, temp, 0, 2);
                    if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                    version = BitConverter.ToUInt16(temp, 0);
                    if (version == 256)
                    {
                        System.Buffer.BlockCopy(reponse, 30, temp, 0, 2);
                        if (BitConverter.IsLittleEndian) Array.Reverse(temp);
                        version = BitConverter.ToUInt16(temp, 0);
                        if (version == 0) retour = true;
                    }
                }

                return retour;
            }
    }
}
