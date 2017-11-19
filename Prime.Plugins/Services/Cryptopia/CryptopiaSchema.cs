﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Prime.Plugins.Services.Cryptopia
{
    internal class CryptopiaSchema
    {
        internal class MarketResponse
        {
            public MarketEntryResponse[] Data;
        }

        internal class MarketEntryResponse
        {
            public int TradePairId;
            public string Label;
            public decimal AskPrice;
            public decimal BidPrice;
            public decimal Low;
            public decimal High;
            public decimal Volume;
            public decimal LastPrice;
            public decimal BuyVolume;
            public decimal SellVolume;
            public decimal Change;
            public decimal Open;
            public decimal Close;
            public decimal BaseVolume;
            public decimal BaseBuyVolume;
            public decimal BaseSellVolume;
        }
    }
}