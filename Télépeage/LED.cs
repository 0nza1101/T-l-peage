using System;
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

namespace Télépeage
{
    class LED
    {
        private Gadgeteer.Modules.GHIElectronics.MulticolorLED m_multicolorLED;

        /*Constructeur d'initialisation du module LED et effectue quelque test
         *@param Le module LED
         * */
        public LED(Gadgeteer.Modules.GHIElectronics.MulticolorLED multicolorLED)
        {
            m_multicolorLED = multicolorLED;

            Debug.Print("MulticolorLED Initialization.");

            /*TimeSpan fadeTime = new TimeSpan(0, 0, 5);
            m_multicolorLED.FadeOnce(GT.Color.Blue, fadeTime, GT.Color.Magenta);
            Thread.Sleep(10000);//10s wait 10s
            m_multicolorLED.TurnOff();*/
        }

        /*Allume la LED de couleur ROUGE
        *@param Le module LED
        * */
        public void R_TrafficLight(Gadgeteer.Modules.GHIElectronics.MulticolorLED multicolorLED)
        {
            //m_multicolorLED.TurnOff();
            m_multicolorLED.TurnRed();
        }

        /*Allume la LED de couleur JAUNE
        *@param Le module LED
        * */
        public void Y_TrafficLight(Gadgeteer.Modules.GHIElectronics.MulticolorLED multicolorLED)
        {
            //m_multicolorLED.TurnOff();
            m_multicolorLED.TurnColor(GT.Color.Yellow);
        }

        /*Allume la LED de couleur VERTE
        *@param Le module LED
        * */
        public void G_TrafficLight(Gadgeteer.Modules.GHIElectronics.MulticolorLED multicolorLED)
        {
            //m_multicolorLED.TurnOff();
            m_multicolorLED.TurnGreen();
        }
    }
}
