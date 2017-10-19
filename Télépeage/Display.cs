 using System;
using System.Threading;
using System.Collections;
using System.IO;
using System.Globalization;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Input;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;



namespace Télépeage
{
    static partial class Constants
    {
        public const string VERSION = "v1.3";
        public const int displayWidth = 320;
        public const int displayHeight = 240;
    }

    public class Display
    {
        /*UI(TEXT,FONT..) VARIABLES
         * 
         * */
        private Gadgeteer.Modules.GHIElectronics.DisplayTE35 m_displayTE35;

        private Window window;
        private Canvas canvas;

        private Font baseFont = Resources.GetFont(Resources.FontResources.NinaB);

        private Image lyceeDiderot;

        private Text teamsC;//Text Mohamed Chelloul
        private Text teamsB;//Text Maxence Barby 
        private Text teamsN;//Text Jordan Nijean
        private Text url;//Url Diderot

        private Text msg;

        private string titleBarText = "Telepeage Diderot";

        public bool isUIDraw = false;

        /*Constructeur de configuration de l'ecran Police d'écriture, Canvas, Textes, Images, Evenements
         * Fait office d'ecran d'introduction
         *@param L'afficheur LCD TE35
        * */
        public Display(Gadgeteer.Modules.GHIElectronics.DisplayTE35 displayTE35)
        {
            Debug.Print("DisplayTE35 Initialization.");
            m_displayTE35 = displayTE35 ;

            m_displayTE35.SimpleGraphics.BackgroundColor = GT.Color.White;

            window = m_displayTE35.WPFWindow;
            canvas = new Canvas();
            window.Child = canvas;

            //Initialise le logo Diderot puis affiche le logo Lycée Diderot
            lyceeDiderot = new Image(new Bitmap(Resources.GetBytes(Resources.BinaryResources.logoDiderotX), Bitmap.BitmapImageType.Bmp));
            AddChild(lyceeDiderot, 0, 0);

            //Affiche léquipe au centre de l'écran
            teamsC = new Text(baseFont, "Mohamed CHELLOUL");
            teamsC.ForeColor = GT.Color.Black;
            AddChild(teamsC, Constants.displayWidth / 2 - 45, Constants.displayHeight / 2);

            teamsN = new Text(baseFont, "Jordan Nijean");
            teamsN.ForeColor = GT.Color.Black;
            AddChild(teamsN, Constants.displayWidth / 2 - 30, Constants.displayHeight / 2 + 30);

            teamsB = new Text(baseFont, "Maxence Barby");
            teamsB.ForeColor = GT.Color.Black;
            AddChild(teamsB, Constants.displayWidth / 2 - 35, Constants.displayHeight / 2 + 60);

            //Affiche l'url du lycée en bas a droite de l'ecran
            url = new Text(baseFont, "http://www.diderot.org/");
            url.ForeColor = GT.Color.Red;
            AddChild(url, Constants.displayWidth / 2 - 70, Constants.displayHeight - 15);

            window.TouchDown += new Microsoft.SPOT.Input.TouchEventHandler(window_TouchDown);//On ajoute l'évenement pour qu'il soit pris en compte
        }

        /*Fonctionse declanche lors de l'apuie sur lecran tactile
         *@param 
         * */
        void window_TouchDown(object sender, Microsoft.SPOT.Input.TouchEventArgs e)
        {
            setupUI();//On dessine notre interface
        }

        /*Fonction qui affiche l'interface que le client va voir
         * Efface les precedent elements et affiche l'interface
         *@param 
         * */
        private void setupUI()
        {
            window.TouchDown -= new Microsoft.SPOT.Input.TouchEventHandler(window_TouchDown);//On suprimme levenement on en a plus besoin
            //Supprime tout le texte, les images
            lyceeDiderot.Visibility = Visibility.Hidden;
            teamsC.Visibility = Visibility.Hidden;
            teamsB.Visibility = Visibility.Hidden;
            teamsN.Visibility = Visibility.Hidden;
            url.Visibility = Visibility.Hidden;

            //On affiche la bar d'en haut
            AddTitleBar(titleBarText, baseFont,GT.Color.White, GT.Color.Magenta, GT.Color.Cyan);

            msg = new Text(baseFont, "");
            msg.ForeColor = GT.Color.Black;
            AddChild(msg, Constants.displayWidth / 2 - 50, Constants.displayHeight / 2);

            //On affiche la bar d'en bas
            Text txtMessage = new Text(baseFont, Constants.VERSION);
            txtMessage.ForeColor = GT.Color.White;
            AddStatusBar(txtMessage, GT.Color.Cyan, GT.Color.Magenta);
        }

        //Dessine notre interface de type windows
        //Bar du haut
        private Border AddTitleBar(string title, Font font, GT.Color foreColor, GT.Color backgroundColor)
        {

            return AddTitleBar(title, font, foreColor, backgroundColor, backgroundColor);
        }

        private Border AddTitleBar(string title, Font font, GT.Color foreColor, GT.Color startColor, GT.Color endColor)
        {
            Brush backgroundBrush = null;
            if (startColor == endColor)
                backgroundBrush = new SolidColorBrush(startColor);
            else
                backgroundBrush = new LinearGradientBrush(startColor, endColor);

            return AddTitleBar(title, font, foreColor, backgroundBrush);
        }

        private Border AddTitleBar(string title, Font font, GT.Color foreColor, Brush backgroundBrush)
        {
            Border titleBar = new Border();
            titleBar.Width = 320;
            titleBar.Height = 27;
            titleBar.Background = backgroundBrush;

            Text text = new Text(font, title);
            text.Width = 320;
            text.ForeColor = foreColor;
            text.SetMargin(5);
            titleBar.Child = text;

            AddChild(titleBar, 0, 0);

            return titleBar;
        }

        //Bar du bas
        private Border AddStatusBar(UIElement element, GT.Color backgroundColor)
        {
            return AddStatusBar(element, backgroundColor, backgroundColor);
        }

        private Border AddStatusBar(UIElement element, GT.Color startColor, GT.Color endColor)
        {
            Brush backgroundBrush = null;
            if (startColor == endColor)
                backgroundBrush = new SolidColorBrush(startColor);
            else
                backgroundBrush = new LinearGradientBrush(startColor, endColor);

            return AddStatusBar(element, backgroundBrush);
        }

        private Border AddStatusBar(UIElement element, Brush backgroundBrush)
        {
            Border statusBar = new Border();
            statusBar.Width = 320;
            statusBar.Height = 27;
            statusBar.Background = backgroundBrush;

            int left, top, right, bottom;
            element.GetMargin(out left, out top, out right, out bottom);
            left = System.Math.Max(left, 5);
            top = System.Math.Max(top, 5);
            bottom = System.Math.Max(bottom, 5);
            element.SetMargin(left, top, right, bottom);
            statusBar.Child = element;

            AddChild(statusBar, 0, 215);

            return statusBar;
        }

        //Fonction qui ajoute des elements au tableau
        private void AddChild(UIElement element, int left, int top)
        {
            canvas.Children.Add(element);
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }

        //Fonction qui affiche un message a l'écran personnalisé en cours d'execution
        public void infoMsg(string text)
        {
            /*
            msg.TextContent = text;
            msg.Invalidate();*/
            m_displayTE35.SimpleGraphics.DisplayRectangle(GT.Color.White, 2, GT.Color.White, 40, 50, 300, 50);
            m_displayTE35.SimpleGraphics.DisplayTextInRectangle(
            text,
            40, 50, 250, 200, GT.Color.Green, Resources.GetFont(Resources.FontResources.NinaB),
            GTM.Module.DisplayModule.SimpleGraphicsInterface.TextAlign.Center,
            GTM.Module.DisplayModule.SimpleGraphicsInterface.WordWrap.None,
            GTM.Module.DisplayModule.SimpleGraphicsInterface.Trimming.CharacterEllipsis,
            GTM.Module.DisplayModule.SimpleGraphicsInterface.ScaleText.ScaleToFit);
        }
    }
}