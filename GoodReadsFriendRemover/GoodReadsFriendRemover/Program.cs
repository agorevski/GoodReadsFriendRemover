using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;

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
    }
}