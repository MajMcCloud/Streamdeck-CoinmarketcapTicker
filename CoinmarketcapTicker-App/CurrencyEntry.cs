using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CoinmarketcapTicker
{
    public class CurrencyEntry
    {
        public String CurrencyTitle { get; set; }

        public DateTime LastUpdate { get; set; }

        public decimal CurrentPrice { get; set; }

        public String CurrentPriceString { get; set; }

        public decimal PriceChange { get; set; }

        public String LogoUrl { get; set; }

        public Decimal StartPrice { get; set; }

        [JsonIgnore]
        public bool NoNetwork { get; set; } = false;

    }
}
