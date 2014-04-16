using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CrawlerTest
{
    public partial class Form1 : Form
    {
        SaveData saveData = new SaveData();
        public Form1()
        {
            InitializeComponent();

            //Console.WriteLine(GetDetailURL(GetSource("http://yanchanghui.taiwandao.tw/List_year_2013_month_9_city___p_1.html")));
            //Console.WriteLine(GetPage(2013, 9));
            //Console.WriteLine(GetSource("http://yanchanghui.taiwandao.tw/Detail_856.html"));
            Crawler();
            //GetInfo("http://yanchanghui.taiwandao.tw/Detail_856.html");
        }

        public void Crawler()
        {
            for (int i = 9; i <= 12; i++) //2013-09~12
            {
                string url = String.Format("http://yanchanghui.taiwandao.tw/List_year_2013_month_{0}_city__.html", i);
                for (int j = 1; j <= GetPageNum(2013, i); j++) //第一頁開始                
                    foreach (string info in GetDetailURL(GetSource(url)))
                    {
                        //Console.WriteLine(info.Split(',')[0] + "," + GetInfo(info.Split(',')[1]));
                        saveData.Save("export.txt", info.Split(',')[0] + "," + GetInfo(info.Split(',')[1]));
                    }

            }
        }

        public string GetInfo(string url) //詳細頁的相關資訊
        {
            string input = GetSource(url);

            string pattern1 = String.Format("<div[^>]*?class=([\"'])[^>]*{0}[^>]*\\1[^>]*>(.*?)</ul>", "zh-detail-content"); ;
            Regex regex = new Regex(pattern1, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

            string result = "";
            if (regex.Match(input).Success)
            {
                //時間
                string time = regex.Match(input).Value.Split(new string[] { "</em>" }, StringSplitOptions.RemoveEmptyEntries)[1]
                    .Split(new string[] { "</li>" }, StringSplitOptions.RemoveEmptyEntries)[0];

                time = Regex.Replace(time.Replace(" / ", "/"), "（.*?）|\\(.*?\\)", " ").Replace("│", " ").Trim();
                time = Regex.Replace(time, "(?<y>\\d{4}).*?(?<m>\\d{1,2}).*?(?<d>\\d{1,2}).|(?<m>\\d{1,2}).*?(?<d>\\d{1,2}).*?(?<y>\\d{4}).", "${y}-${m}-${d} ");

                string date = time.Split(' ')[0]; //日期

                time = time.Replace(date, "").Trim();
                if (time == "") time = "N";
                
                string endTime = ""; //結束時間
                if (time.Split('~').Length > 1)
                    endTime = time.Split('~')[1];
                else
                    endTime = "N";

                time = time.Split('~')[0]; //開始時間
                date = Regex.Replace(date, "(?<y>\\d{4})-(?<m>\\d{1})-(?<d>\\d{1})", "${y}-0${m}-0${d}"); //yyyy-MM-dd

                string city = regex.Match(input).Value.Split(new string[] { "</em>" }, StringSplitOptions.RemoveEmptyEntries)[2]
                    .Split(new string[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[1]
                    .Split(new string[] { "</a>" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                string place = regex.Match(input).Value.Split(new string[] { "</em>" }, StringSplitOptions.RemoveEmptyEntries)[3]
                    .Split(new string[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[1]
                    .Split(new string[] { "</a>" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                string price = regex.Match(input).Value.Split(new string[] { "</em>" }, StringSplitOptions.RemoveEmptyEntries)[4]
                    .Split(new string[] { "</li>" }, StringSplitOptions.RemoveEmptyEntries)[0].Split('元')[0];

                price = price.Replace("│", "/").Replace(" / ", "/").Replace(",", "").Replace("、", "/").Trim();

                result = date + "," + time + "," + endTime + "," + city + "," + place + "," + price;
            }
            return result;
        }

        public int GetPageNum(int year, int month) //取得頁數
        {
            string url = String.Format("http://yanchanghui.taiwandao.tw/List_year_{0}_month_{1}_city__.html", year, month);

            string input = GetSource(url);
            int maxPage = 1;
            foreach (Match match in Regex.Matches(input, "<option.*?_city___p_(?<page>\\d{1})", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase))
            {
                if (int.Parse(match.Groups["page"].Value) > maxPage)
                    maxPage = int.Parse(match.Groups["page"].Value);
            }
            return maxPage;
        }

        public List<string> GetDetailURL(string input) //取得詳細頁的網址
        {
            List<string> result = new List<string>();
            foreach (Match match in Regex.Matches(input, "<em><a href=['\"](?<url>.Detail_(?<id>\\d{3}).+?)[\"'].*?[>]"))
            {
                result.Add(match.Groups["id"].Value + "," + "http://yanchanghui.taiwandao.tw/" + match.Groups["url"].Value);
            }
            return result;
        }

        public static string GetSource(string url) // 從網路上取得原始碼
        {
            WebClient client = new WebClient();

            //以防萬一 模擬自己為瀏覽器
            client.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.56 Safari/536.5");
            client.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.Headers.Add("Accept-Encoding: identity");
            client.Headers.Add("Accept-Language: zh-TW,en;q=0.8");
            client.Headers.Add("Accept-Charset: utf-8;q=0.7,*;q=0.3");
            client.Headers.Add("ContentType", "application/x-www-form-urlencoded");
            client.Encoding = Encoding.UTF8;

            //Console.WriteLine(ToTraditional(client.DownloadString(url)));
            return ToTraditional(client.DownloadString(url));

        }

        #region 簡繁轉換
        private const int LocaleSystemDefault = 0x0800;
        private const int LcmapSimplifiedChinese = 0x02000000;
        private const int LcmapTraditionalChinese = 0x04000000;

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int LCMapString(int locale, int dwMapFlags, string lpSrcStr, int cchSrc,
                                              [Out] string lpDestStr, int cchDest);

        public static string ToSimplified(string argSource)
        {
            var t = new String(' ', argSource.Length);
            LCMapString(LocaleSystemDefault, LcmapSimplifiedChinese, argSource, argSource.Length, t, argSource.Length);
            return t;
        }

        public static string ToTraditional(string argSource)
        {
            var t = new String(' ', argSource.Length);
            LCMapString(LocaleSystemDefault, LcmapTraditionalChinese, argSource, argSource.Length, t, argSource.Length);
            return t;
        }
        #endregion

    }
}
