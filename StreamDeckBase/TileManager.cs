using IronSoftware.Drawing;
using Newtonsoft.Json.Linq;
using streamdeck_client_csharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace StreamDeckBase
{
    public class TileManager
    {
        public Dictionary<string, JObject> TileSettings { get; set; }

        public StreamDeckConnection connection { get; set; }

        public Options Options { get; set; }

        ManualResetEvent connectEvent;
        ManualResetEvent disconnectEvent;


        EventHandlerList __Events { get; set; } = new EventHandlerList();


        private static object __evPageOpened = new object();

        System.Timers.Timer __tmPageLoadings = null;


        public TileManager(Options options)
        {

            this.Options = options;

            this.TileSettings = new Dictionary<string, JObject>();

            __tmPageLoadings = new System.Timers.Timer();
            __tmPageLoadings.Interval = 100;
            __tmPageLoadings.AutoReset = false;
            __tmPageLoadings.Elapsed += __tmPageLoadings_Elapsed;

        }

        private void __tmPageLoadings_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPageOpened(new EventArgs());
        }

        public void Start()
        {
            connectEvent = new ManualResetEvent(false);
            disconnectEvent = new ManualResetEvent(false);

            connection = new StreamDeckConnection(Options.Port, Options.PluginUUID, Options.RegisterEvent);

            connection.OnConnected += (sender, args) =>
            {
                connectEvent.Set();

            };

            connection.OnDisconnected += (sender, args) =>
            {
                disconnectEvent.Set();
            };

            connection.OnWillAppear += (sender, args) =>
            {
                lock (TileSettings)
                {
                    TileSettings[args.Event.Context] = args.Event.Payload.Settings;
                    if (TileSettings[args.Event.Context] == null)
                    {
                        TileSettings[args.Event.Context] = new JObject();

                    }
                }

                //if (__tmPageLoadings.Enabled)
                //{

                //}
                //else
                //{
                //    __tmPageLoadings.Start();
                //}
                __tmPageLoadings.Stop();
                __tmPageLoadings.Start();
            };

            //connection.OnDidReceiveSettings += async (sender, args) =>
            //{

            //};

            //connection.OnSendToPlugin += async (sender, args) =>
            //{
            //    var evt = args.Event;

            //};

            connection.OnWillDisappear += async (sender, args) =>
            {
                lock (TileSettings)
                {
                    if (TileSettings.ContainsKey(args.Event.Context))
                    {
                        TileSettings.Remove(args.Event.Context);
                    }
                }
            };

            

            //connection.OnKeyUp += async (sender, args) =>
            //{




            //};


            // Start the connection
            connection.Run();

        }

        public void WaitForStop()
        {
            // Wait for up to 10 seconds to connect
            if (connectEvent.WaitOne(TimeSpan.FromSeconds(10)))
            {
                // We connected, loop every second until we disconnect
                while (!disconnectEvent.WaitOne(TimeSpan.FromMilliseconds(2000)))
                {





                }

            }
        }


        public void OnPageOpened(EventArgs e)
        {
            if (__Events[__evPageOpened] is EventHandler h)
                h(this, e);
        }

        public event EventHandler PageOpened
        {
            add
            {
                __Events.AddHandler(__evPageOpened, value);
            }
            remove
            {
                __Events.RemoveHandler(__evPageOpened, value);
            }
        }


        public async Task SetImageForAll(AnyBitmap image)
        {
            foreach (var c in TileSettings.Reverse())
            {
                if (TileSettings[c.Key] == null)
                {
                    continue;
                }

                AnyBitmap bmp = image;

                await connection.SetImageAsync(bmp, c.Key, SDKTarget.HardwareAndSoftware, null);
            }
        }

    }
}
