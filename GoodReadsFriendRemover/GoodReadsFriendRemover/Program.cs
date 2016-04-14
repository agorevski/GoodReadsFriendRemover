using Fiddler;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace GoodReadsFriendRemover
{
    class Program
    {
        /// <summary>
        /// The post data from Fiddler
        /// Example: "_method=post&authenticity_token=aaabbbcccdddeeefffggg%3D%3d";
        /// </summary>
        static string POST_DATA = string.Empty;

        /// <summary>
        /// Your cookie header to attach to the requests
        //Example:  "csid=123; locale=en; csm-sid=456; __utmt=1; [...] _session_id2=aaabbbccc";
        /// </summary>
        static string COOKIE_HEADER = string.Empty;

        static Dictionary<int, string> UserNameDictionary = new Dictionary<int, string>();
        const string GET_FRIEND_URI = "https://www.goodreads.com/friend?page={0}";
        const string DESTROY_FRIEND_URI = "https://www.goodreads.com/friend/destroy/{0}?return_url=%2Ffriend";

        static void Main(string[] args)
        {
            StartListeningWithFiddler();

            while (string.IsNullOrEmpty(POST_DATA) && string.IsNullOrEmpty(COOKIE_HEADER))
            { }

            Thread.Sleep(2000);

            Console.WriteLine("Querying for all of your friends...");

            using (WebClient getFriendWC = new WebClient())
            {
                getFriendWC.Headers.Add(HttpRequestHeader.Cookie, COOKIE_HEADER);

                HtmlNodeCollection nodes = null;
                var page = 1;
                do
                {
                    var response = getFriendWC.DownloadString(string.Format(GET_FRIEND_URI, page));
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response);
                    nodes = doc.DocumentNode.SelectNodes("//div[@class='elementList']/a[@title]");

                    if (null != nodes)
                    {
                        foreach (var node in nodes)
                        {
                            var name = node.GetAttributeValue("title", "");
                            var href = node.GetAttributeValue("href", "");
                            var strId = href.Replace("https://www.goodreads.com/user/show/", "");
                            var id = int.Parse(strId);
                            if (!UserNameDictionary.ContainsKey(id))
                            {
                                UserNameDictionary.Add(id, name);
                            }
                        }
                    }
                    page++;
                } while (null != nodes && nodes.Count > 0);
            }

            Console.WriteLine("{0} Users Found!", UserNameDictionary.Count);
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.Cookie, COOKIE_HEADER);
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                foreach (var kvp in UserNameDictionary)
                {
                    var id = kvp.Key;
                    var name = kvp.Value;
                    Console.WriteLine("Would you like to remove {0}? [Y/N]", name);

                    if (GetKeyPressYesNo())
                    {
                        Console.WriteLine("Are you sure? [Y/N]");
                        if (GetKeyPressYesNo())
                        {
                            try
                            {
                                wc.UploadString(string.Format(DESTROY_FRIEND_URI, id), POST_DATA);
                                Console.WriteLine("{0} has been removed successfully.", name);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failure occurred! {0} || {1}", e.Message, e.StackTrace);
                            }
                        }
                    }
                }

                Console.WriteLine("Cleanup complete!  Hit enter to exit");
                Console.ReadLine();
            }
        }

        private static bool GetKeyPressYesNo()
        {
            var keyPressed = '~';
            do
            {
                keyPressed = Console.ReadKey().KeyChar;
            }
            while (!keyPressed.Equals('y') &&
                   !keyPressed.Equals('n') &&
                   !keyPressed.Equals('Y') &&
                   !keyPressed.Equals('N'));

            Console.WriteLine(Environment.NewLine);
            if (keyPressed == 'y' || keyPressed == 'Y')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void WriteCommandResponse(string s)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(s);
            Console.ForegroundColor = oldColor;
        }

        public static void DetachFiddler()
        {
            Console.WriteLine("Shutting down FiddlerCore...");
            FiddlerApplication.Shutdown();
            Thread.Sleep(500);
        }
        private static string Ellipsize(string s, int iLen)
        {
            if (s.Length <= iLen) return s;
            return s.Substring(0, iLen - 3) + "...";
        }

        static void StartListeningWithFiddler()
        {
            var oAllSessions = new List<Session>();
            #region AttachEventListeners

            FiddlerApplication.BeforeRequest += delegate (Fiddler.Session oS)
            {
                if (oS.uriContains("www.goodreads.com/friend/destroy") && oS.RequestMethod == "POST")
                {
                    string post_data = System.Text.Encoding.UTF8.GetString(oS.RequestBody);
                    var cookie_header = oS.RequestHeaders.AllValues("Cookie");
                    POST_DATA = post_data;
                    COOKIE_HEADER = cookie_header;
                    Console.WriteLine("Cookie and Post information found! Gracefully terminating FiddlerCore and proceeding..");
                    DetachFiddler();
                }
            };

            // Tell the system console to handle CTRL+C by calling our method that
            // gracefully shuts down the FiddlerCore.
            // Note, this doesn't handle the case where the user closes the window with the close button.
            // See http://geekswithblogs.net/mrnat/archive/2004/09/23/11594.aspx for info on that...
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            #endregion AttachEventListeners

            Console.WriteLine(String.Format("Starting {0}...", FiddlerApplication.GetVersionString()));

            Console.WriteLine("Steps:" + Environment.NewLine +
                "1. Navigate to http://www.goodreads.com" + Environment.NewLine +
                "2. Log in (if you haven't already)" + Environment.NewLine +
                "3. Go to your friends page, and delete someone (this will not take effect)" + Environment.NewLine);
                
            // For the purposes of this demo, we'll forbid connections to HTTPS 
            // sites that use invalid certificates
            CONFIG.IgnoreServerCertErrors = false;

            // Because we've chosen to decrypt HTTPS traffic, makecert.exe must
            // be present in the Application folder.
            FiddlerApplication.Startup(8877, true, true);
            Console.WriteLine("Hit CTRL+C to end session.");
        }

        /// <summary>
        /// When the user hits CTRL+C, this event fires.  We use this to shut down and unregister our FiddlerCore.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DetachFiddler();
        }
    }
}