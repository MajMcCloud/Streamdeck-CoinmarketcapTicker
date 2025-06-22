using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CommandLine;
using Newtonsoft.Json.Linq;
using System.Globalization;
using CoinmarketcapTicker.lib;
using HtmlAgilityPack;
using System.Reflection.Emit;
using streamdeck_client_csharp;
using IronSoftware.Drawing;
using StreamDeckBase;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;

using SKFont = SixLabors.Fonts.Font;
using SKColor = SixLabors.ImageSharp.Color;
using SKImage = SixLabors.ImageSharp.Image;
using SKRectangle = SixLabors.ImageSharp.Rectangle;
using SKPoint = SixLabors.ImageSharp.Point;
using SixLabors.Fonts;



namespace CoinmarketcapTicker
{
    class Program
    {

        public static TileManager Manager { get; set; }


        public static StreamDeckConnection connection
        {
            get
            {
                return Manager.connection;
            }
        }

        public static Dictionary<string, JObject> settings
        {
            get
            {
                return Manager.TileSettings;
            }
        }

        public static List<CurrencyEntry> CurrencyCache { get; set; } = new List<CurrencyEntry>();

        public static List<SixLabors.Fonts.Font> Fonts { get; set; } = new List<SixLabors.Fonts.Font>();

        public static System.Timers.Timer timer { get; set; }

        public static System.Timers.Timer tmInternet { get; set; }

        public static bool Loading { get; set; } = false;


        public static CultureInfo Culture = CultureInfo.CurrentUICulture ?? new CultureInfo("en-gb");

        // StreamDeck launches the plugin with these details
        // -port [number] -pluginUUID [GUID] -registerEvent [string?] -info [json]
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Threading.Thread.Sleep(1000);
            //}

            // The command line args parser expects all args to use `--`, so, let's append
            for (int count = 0; count < args.Length; count++)
            {
                if (args[count].StartsWith("-") && !args[count].StartsWith("--"))
                {
                    args[count] = $"-{args[count]}";
                }
            }

            //AnyBitmap bmp = Properties.Resources.loading;


            //String base64 = bmp.ToString();


            var set = cache.load();
            if (set != null)
                CurrencyCache = set.Cache;

            Fonts = CollectFonts();

            timer = new System.Timers.Timer();

            timer.Elapsed += Timer_Elapsed;

            Parser parser = new Parser((with) =>
            {
                with.EnableDashDash = true;
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.IgnoreUnknownArguments = true;
                with.HelpWriter = Console.Error;
            });

            //System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            ParserResult<Options> options = parser.ParseArguments<Options>(args);
            options.WithParsed<Options>(o => RunPlugin(o));

        }

        private static async Task<bool> CheckEvents(bool UpdateImage = true)
        {

            if (DateTime.Today.Month == 12 && (DateTime.Today.Day >= 24 && DateTime.Today.Day <= 26))
            {
                if (!UpdateImage)
                    return true;

                await SetImageForAll(StaticImages.Christmas);

                new Thread(() =>
                {
                    Thread.Sleep(5000);

                    Timer_Elapsed(null, null);

                }).Start();

                return true;
            }

            if (DateTime.Today.Month == 12 && DateTime.Today.Day == 31)
            {
                if (!UpdateImage)
                    return true;

                await SetImageForAll(StaticImages.Sylvester);

                new Thread(() =>
                {
                    Thread.Sleep(5000);

                    Timer_Elapsed(null, null);

                }).Start();

                return true;
            }

            if (DateTime.Today.Month == 1 && DateTime.Today.Day == 1)
            {
                if (!UpdateImage)
                    return true;

                await SetImageForAll(StaticImages.NewYears);

                new Thread(() =>
                {
                    Thread.Sleep(5000);

                    Timer_Elapsed(null, null);

                }).Start();

                return true;
            }



            return false;
        }

        public static async Task SetImageForAll(AnyBitmap image)
        {
            foreach (var c in settings.Reverse())
            {
                if (settings[c.Key] == null)
                {
                    continue;
                }

                await connection.SetImageAsync(image, c.Key, SDKTarget.HardwareAndSoftware, null);

            }
        }


        public static void CheckInternet()
        {
            int i = 0;

            tmInternet = new System.Timers.Timer(10000);
            tmInternet.Elapsed += (s, en) =>
            {
                var p = new Ping();
                try
                {
                    var res = p.Send("1.1.1.1", 1000);
                    if (res.Status != IPStatus.Success)
                    {
                        i++;
                        return;
                    }

                    tmInternet.Stop();

                    if (i > 0)
                    {
                        Timer_Elapsed(null, null);
                    }

                }
                catch
                {
                    return;
                }
            };

            tmInternet.Start();
        }

        //private static void NetworkChange_NetworkAvailabilityChanged(object sender, System.Net.NetworkInformation.NetworkAvailabilityEventArgs e)
        //{
        //    if (!e.IsAvailable)
        //        return;

        //    if (Loading)
        //        return;

        //    Timer_Elapsed(null, null);
        //}

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Loading = true;

            foreach (var c in settings.Reverse())
            {
                if (settings[c.Key] == null)
                {
                    continue;
                }

                ThreadPool.QueueUserWorkItem(async a =>
                {
                    await RefreshTile(c.Key, settings[c.Key], true);
                });
            }

            Loading = false;
        }

        private static async Task RefreshTile(String context, JObject settings, bool force = false)
        {
            if (settings["currencyName"] == null || settings["currencyName"].ToString() == "")
            {
                return;
            }

            Thread t = new Thread(new ThreadStart(() =>
            {
                try
                {
                    LoadCurrency(settings["currencyName"].ToString(), context, force).Wait();
                }
                catch (Exception ex)
                {

                }

            }));

            t.Start();

        }


        static void RunPlugin(Options options)
        {

            Manager = new TileManager(options);

            Manager.Start();

            Manager.PageOpened += async (sender, args) =>
            {

                if (await CheckEvents(true))
                {
                    return;
                }

                if (settings == null)
                {
                    return;
                }

                //Timer_Elapsed(null, null);
                var cfg = settings.FirstOrDefault().Value ?? new JObject();

                if (!timer.Enabled && cfg["refreshInterval"] != null)
                {
                    timer.Interval = (double)cfg["refreshInterval"] * 1000 * 60;
                    timer.Start();
                }
            };


            connection.OnConnected += (sender, args) =>
            {
                tmInternet?.Start();
            };

            connection.OnWillAppear += async (sender, args) =>
            {
                var cfg = new JObject();

                //    //System.Diagnostics.Debug.WriteLine("Appear " + args.Event.Action);

                settings[args.Event.Context] = args.Event.Payload.Settings;
                if (settings[args.Event.Context] == null)
                {
                    settings[args.Event.Context] = new JObject();

                }

                cfg = settings[args.Event.Context];



                if (await CheckEvents(false))
                {
                    return;
                }

                if (!timer.Enabled && cfg["refreshInterval"] != null)
                {
                    timer.Interval = (double)cfg["refreshInterval"] * 1000 * 60;
                    timer.Start();
                }

                await RefreshTile(args.Event.Context, cfg);

            };

            connection.OnDidReceiveSettings += async (sender, args) =>
            {
                var cfg = new JObject();

                //System.Diagnostics.Debug.WriteLine("Settings " + args.Event.Action);


                settings[args.Event.Context] = args.Event.Payload.Settings;
                if (settings[args.Event.Context] == null)
                {
                    settings[args.Event.Context] = new JObject();
                }

                cfg = settings[args.Event.Context];


                timer.Stop();

                timer.Interval = (double)cfg["refreshInterval"] * 1000 * 60;

                timer.Start();

                //ThreadPool.QueueUserWorkItem<object>(delegate
                //{
                //    RefreshTile(args.Event.Context, cfg).Wait();
                //}, null, true);

                await RefreshTile(args.Event.Context, cfg, true);
            };

            connection.OnWillDisappear += async (sender, args) =>
            {

                lock (settings)
                {
                    if (settings.ContainsKey(args.Event.Context))
                    {
                        settings.Remove(args.Event.Context);
                    }
                }


                timer.Stop();

            };

            connection.OnKeyUp += async (sender, args) =>
            {
                var cfg = settings[args.Event.Context];
                if (cfg["currencyName"] == null || cfg["currencyName"].ToString() == "")
                {
                    return;
                }

                try
                {
                    switch (cfg["event"].ToString())
                    {
                        case "copy":

                            //currencyName

                            break;

                        case "openwebsite":

                            var entry = CurrencyCache.FirstOrDefault(a => a.CurrencyTitle == cfg["currencyName"].ToString());

                            if (entry == null)
                            {
                                return;
                            }


                            String url = $"https://coinmarketcap.com/currencies/{entry.CurrencyTitle}/";

                            Tools.OpenUrl(url);

                            break;

                        case "refresh":

                            await RefreshTile(args.Event.Context, settings[args.Event.Context], true);

                            break;

                    }
                }
                catch
                {

                }



            };


            Manager.WaitForStop();

            //Remove duplicates and other wrong items
            Cleanup();

            var cache = new cache();
            cache.Cache = CurrencyCache;
            cache.save();

            // Current Directory is the base Stream Deck Install path.
            // For example: C:\Program Files\Elgato\StreamDeck\
            //Image image = Image.FromFile(@"Images\TyDidIt40x40.png");

            //System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
        }


        private static void Cleanup()
        {

            var vc = (from v in CurrencyCache
                      group v by v.CurrencyTitle into vg
                      select vg).ToList();


            var cleaned = vc.Select(a => a.LastOrDefault()).ToList();

            CurrencyCache = cleaned;
        }


        public static async Task LoadCurrency(String currencyName, String Context, bool force = false)
        {
            var entry = CurrencyCache.FirstOrDefault(a => a.CurrencyTitle == currencyName);

            try
            {

                if (entry == null)
                {
                    connection.SetImageAsync(StaticImages.Loading, Context, SDKTarget.HardwareAndSoftware, null).Wait();

                    entry = new CurrencyEntry();
                    entry.CurrencyTitle = currencyName;
                    CurrencyCache.Add(entry);
                }
                else if (entry != null && !force && DateTime.Now.Subtract(entry.LastUpdate).TotalMinutes < (timer.Interval / 60 / 1000))
                {
                    await Render(Context);

                    return;
                }

                if (force | DateTime.Now.Subtract(entry.LastUpdate).TotalMinutes > 120)
                {
                    connection.SetImageAsync(StaticImages.Loading, Context, SDKTarget.HardwareAndSoftware, null).Wait();
                }

                if (settings[Context]["startPrice"] != null)
                {
                    decimal sp = 0;
                    if (decimal.TryParse(settings[Context]["startPrice"]?.ToString(), NumberStyles.Currency, new CultureInfo("en-gb"), out sp))
                    {
                        entry.StartPrice = sp;
                    }
                    else
                    {
                        entry.StartPrice = 0;
                    }
                }

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

                WebClientEx wc = new WebClientEx();

                wc.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-gb");
                wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
                wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                wc.Headers.Add("sec-ch-ua", "\"Not/A)Brand\";v=\"99\", \"Brave\";v=\"115\", \"Chromium\";v=\"115\"");
                wc.Headers.Add("sec-fetch-site", "same-origin");
                wc.Headers.Add("sec-fetch-dest", "document");
                wc.Headers.Add("sec-fetch-mode", "navigate");
                wc.Headers.Add("sec-fetch-user", "?1");
                wc.Headers.Add("Sec-Ch-Ua-Platform", "Windows");

                short retry = 3;

                wc.CookieContainer.Add(new Cookie("currency", "USD", "/", "coinmarketcap.com"));

                HtmlNode price = null;

                do
                {
                    String url = $"https://coinmarketcap.com/currencies/{entry.CurrencyTitle}/"; //?_t=" + DateTime.Now.Ticks;
                    String t = wc.DownloadString(url);

                    doc.LoadHtml(t);

                    retry--;

                    price = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'coin-stats-header')]/*/span[contains(., '$')]");

                    CultureInfo ci = new CultureInfo("en-gb");
                    decimal d = 0;

                    if (price != null)
                    {
                        entry.CurrentPriceString = price.InnerText;

                        if (decimal.TryParse(price.InnerText.Substring(1), NumberStyles.Currency, ci, out d))
                        {
                            entry.CurrentPrice = d;
                        }
                    }
                    else
                    {

                    }
                    //var change = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'iqsl6q-0')]");

                    //if (change != null)
                    //{
                    //    if (decimal.TryParse(change.InnerText.Substring(0, change.InnerText.Length - 1), NumberStyles.Float, ci, out d))
                    //    {
                    //        entry.PriceChange = d;
                    //    }
                    //}

                    var change = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'coin-stats-header')]//p[contains(., '%')]");


                    if (entry.StartPrice == 0)
                    {
                        if (change != null)
                        {
                            if (decimal.TryParse(change.InnerText.Substring(0, change.InnerText.IndexOf('%')), NumberStyles.Float, ci, out d))
                            {
                                entry.PriceChange = d;
                            }

                            if (change.GetAttributeValue("data-change", "up") == "down")
                            {
                                entry.PriceChange = entry.PriceChange * -1;
                            }
                        }
                    }
                    else
                    {
                        var pc = ((entry.CurrentPrice * 100) / entry.StartPrice) - 100;

                        entry.PriceChange = pc;
                    }

                    var logo = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'coin-stats-header')]//img");

                    if (logo != null)
                    {
                        entry.LogoUrl = logo.GetAttributeValue("src", null);
                    }

                    if (price == null && retry > 0)
                    {
                        Thread.Sleep(1000);
                    }

                } while (price == null && retry > 0);

                entry.LastUpdate = DateTime.Now;

                entry.NoNetwork = false;

                await Render(Context);
            }
            catch (WebException ex)
            {
                var h = ex.Response as HttpWebResponse;

                if (h != null && h.StatusCode == HttpStatusCode.NotFound)
                {
                    await connection.SetImageAsync(StaticImages.Not_Found, Context, SDKTarget.HardwareAndSoftware, null);
                }
                else
                {
                    if (entry == null)
                    {
                        await connection.SetImageAsync(StaticImages.No_Internet_2x, Context, SDKTarget.HardwareAndSoftware, null);
                    }
                    else
                    {
                        entry.NoNetwork = true;
                        await Render(Context);
                    }

                    //File.AppendAllText("debug.txt", ex.Message + "\r\n" + ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.txt", ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        static async Task Render(String Context)
        {
            if (!settings.ContainsKey(Context))
            {
                return;
            }

            var entry = CurrencyCache.FirstOrDefault(a => a.CurrencyTitle == settings[Context]["currencyName"].ToString());
            if (entry == null)
                return;

            //AnyBitmap bmp = GenerateBitmap(entry, 3);

            AnyBitmap bmp2 = GenerateBitmap2(entry, 3);

            await connection.SetImageAsync(bmp2, Context, SDKTarget.HardwareAndSoftware, null);
        }

        //static AnyBitmap GenerateBitmap(CurrencyEntry entry, int factor = 1)
        //{
        //    Bitmap bmp = new Bitmap(100 * factor, 100 * factor);

        //    using (Graphics g = Graphics.FromImage(bmp))
        //    {
        //        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        //        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        //        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        //        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        //        g.Clear(System.Drawing.Color.Black);

        //        if (entry != null && entry.LogoUrl != null)
        //        {
        //            WebClient wc = new WebClient();
        //            using (Stream st = wc.OpenRead(entry.LogoUrl))
        //            {
        //                Bitmap b = new Bitmap(st);

        //                g.DrawImage(b, new System.Drawing.Rectangle(5 * factor, 5 * factor, 24 * factor, 24 * factor));
        //            }
        //        }

        //        //No Network ?
        //        if (entry.NoNetwork)
        //        {
        //            var b = StaticImages.No_Internet;
        //            g.DrawImage(b, (100 * factor) - (b.Width * 2), 5 * factor);
        //        }

        //        FontFamily ff = new FontFamily(System.Drawing.Text.GenericFontFamilies.SansSerif);

        //        StringFormat sf = new StringFormat();

        //        sf.Alignment = StringAlignment.Center;

        //        g.DrawString(entry.CurrencyTitle.ToUpper(), new System.Drawing.Font(ff, 16 * factor, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, new System.Drawing.RectangleF(0, 32 * factor, 100 * factor, 20 * factor), sf);

        //        System.Drawing.Font f = new System.Drawing.Font(ff, 20 * factor, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
        //        if (entry.CurrentPrice > 100)
        //        {
        //            g.DrawString(entry.CurrentPrice.ToString("N0") + "$", f, Brushes.White, new System.Drawing.RectangleF(0, 53 * factor, 100 * factor, 20 * factor), sf);
        //        }
        //        else if (entry.CurrentPrice < 0.0001m)
        //        {
        //            //var font_size = GetFontSize(100 * factor, 20 * factor, entry.CurrentPrice.ToString(), 0, 3, 20, f);

        //            g.DrawString(entry.CurrentPrice.ToString() + "$", f, Brushes.White, new System.Drawing.RectangleF(0, 53 * factor, 100 * factor, 20 * factor), sf);
        //        }
        //        else if (entry.CurrentPrice < 10)
        //        {
        //            g.DrawString(entry.CurrentPrice.ToString("0.0000") + "$", f, Brushes.White, new System.Drawing.RectangleF(0, 53 * factor, 100 * factor, 20 * factor), sf);
        //        }
        //        else
        //        {
        //            g.DrawString(entry.CurrentPrice.ToString("N") + "$", f, Brushes.White, new System.Drawing.RectangleF(0, 53 * factor, 100 * factor, 20 * factor), sf);
        //        }

        //        f = new System.Drawing.Font(ff, 15 * factor, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
        //        if (entry.PriceChange > 0)
        //        {
        //            g.DrawString(entry.PriceChange.ToString("0.00") + "%", f, new Pen(System.Drawing.Color.FromArgb(22, 199, 132)).Brush, new System.Drawing.RectangleF(0, 79 * factor, 95 * factor, 20 * factor), sf);
        //        }
        //        else
        //        {
        //            g.DrawString(entry.PriceChange.ToString("0.00") + "%", f, new Pen(System.Drawing.Color.FromArgb(234, 57, 67)).Brush, new System.Drawing.RectangleF(0, 79 * factor, 95 * factor, 20 * factor), sf);
        //        }

        //    }

        //    bmp = ResizeImage(bmp, 100, 100);

        //    return bmp;
        //}

        static AnyBitmap GenerateBitmap2(CurrencyEntry entry, int factor = 1)
        {
            AnyBitmap bmp = new AnyBitmap(100 * factor, 100 * factor);

            var img = (SKImage)bmp;

            bmp = bmp.Fill(SKColor.Black);


            if (entry != null && entry.LogoUrl != null)
            {
                WebClient wc = new WebClient();
                AnyBitmap b = ImageTools.DownloadFromUrl(entry.LogoUrl);

                img.DrawImageScaled(b, new SKRectangle(5 * factor, 5 * factor, 24 * factor, 24 * factor));
            }

            //No Network ?
            if (entry.NoNetwork)
            {
                SKImage b = StaticImages.No_Internet;

                img.Mutate(a => a.DrawImage(b, new SKPoint((100 * factor) - (b.Width * 2), 5 * factor), 1));
            }

            SixLabors.Fonts.Font ff = Fonts.FirstOrDefault();

            SixLabors.Fonts.Font Scaled_Font_Small = new SixLabors.Fonts.Font(ff, 16 * factor);

            img.DrawText(Scaled_Font_Small, entry.CurrencyTitle.ToUpper(), SKColor.White, new SKPoint(50 * factor, 32 * factor));

            SixLabors.Fonts.Font Scaled_Font_Big = new SixLabors.Fonts.Font(ff, 20 * factor);

            if (entry.CurrentPrice > 100)
            {
                img.DrawText(Scaled_Font_Big, entry.CurrentPrice.ToString("N0", Culture) + "$", SKColor.White, new SKPoint(50 * factor, 53 * factor));
            }
            else if (entry.CurrentPrice < 0.0001m)
            {
                var measured_font_size = img.MeasureMaxFontSize(Scaled_Font_Big, entry.CurrentPrice.ToString() + "$") - factor;

                var small_font = new SKFont(Scaled_Font_Big, measured_font_size);

                img.DrawText(small_font, entry.CurrentPrice.ToString(Culture) + "$", SKColor.White, new SKPoint(50 * factor, 53 * factor));
            }
            else if (entry.CurrentPrice < 10)
            {
                img.DrawText(Scaled_Font_Big, entry.CurrentPrice.ToString("0.0000", Culture) + "$", SKColor.White, new SKPoint(50 * factor, 53 * factor));
            }
            else
            {
                img.DrawText(Scaled_Font_Big, entry.CurrentPrice.ToString("N", Culture) + "$", SKColor.White, new SKPoint(50 * factor, 53 * factor));
            }

            SixLabors.Fonts.Font Scaled_Font_Small2 = new SixLabors.Fonts.Font(ff, 15 * factor);

            if (entry.PriceChange > 0)
            {
                img.DrawText(Scaled_Font_Small2, entry.PriceChange.ToString("0.00", Culture) + "%", new SKColor(new SixLabors.ImageSharp.PixelFormats.Argb32(22, 199, 132, 255)), new SKPoint(50 * factor, 79 * factor));
            }
            else
            {
                img.DrawText(Scaled_Font_Small2, entry.PriceChange.ToString("0.00", Culture) + "%", new SKColor(new SixLabors.ImageSharp.PixelFormats.Argb32(234, 57, 67, 255)), new SKPoint(50 * factor, 79 * factor));
            }


            bmp = img;


            bmp = bmp.ResizeImage(100, 100);

            return bmp;
        }


        //// Return the largest font size that lets the text fit.
        //private static float GetFontSize(int width, int height, string text,
        //    int margin, float min_size, float max_size, Font font)
        //{
        //    // Only bother if there's text.
        //    if (text.Length == 0) return min_size;

        //    // See how much room we have, allowing a bit
        //    // for the Label's internal margin.
        //    int wid = width - margin;
        //    int hgt = height - margin;

        //    var bmp = new Bitmap(width, height);

        //    // Make a Graphics object to measure the text.
        //    using (Graphics gr = Graphics.FromImage(bmp))
        //    {
        //        while (max_size - min_size > 0.1f)
        //        {
        //            float pt = (min_size + max_size) / 2f;
        //            using (Font test_font = new Font(font.FontFamily, pt))
        //            {
        //                // See if this font is too big.
        //                SizeF text_size =
        //                    gr.MeasureString(text, test_font);
        //                if ((text_size.Width > wid) ||
        //                    (text_size.Height > hgt))
        //                    max_size = pt;
        //                else
        //                    min_size = pt;
        //            }
        //        }
        //        return min_size;
        //    }
        //}


        static List<SixLabors.Fonts.Font> CollectFonts()
        {
            List<SixLabors.Fonts.Font> fonts = new List<SixLabors.Fonts.Font>();

            try
            {
                SixLabors.Fonts.Font ff = new IronSoftware.Drawing.Font("Verdana", 14);
                if (ff != null)
                    fonts.Add(ff);
            }
            catch
            {

            }

            try
            {
                SixLabors.Fonts.Font ff = new IronSoftware.Drawing.Font("Times New Roman", 16);
                if (ff != null)
                    fonts.Add(ff);
            }
            catch
            {

            }

            try
            {
                SixLabors.Fonts.Font ff = new IronSoftware.Drawing.Font("Times", 16);
                if (ff != null)
                    fonts.Add(ff);
            }
            catch
            {

            }

            try
            {
                var ff = SixLabors.Fonts.SystemFonts.Families.FirstOrDefault().CreateFont(14);
                if (ff != null)
                    fonts.Add(ff);

            }
            catch
            {

            }

            return fonts;
        }


    }
}
