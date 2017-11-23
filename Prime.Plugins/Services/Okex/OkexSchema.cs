﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Prime.Plugins.Services.Okex
{
    internal class OkexSchema
    {
        internal class TickerResponse
        {
            public long date;
            public TickerEntryResponse ticker;
        }

        internal class TickerEntryResponse
        {
            public decimal buy;
            public decimal high;
            public decimal low;
            public decimal last;
            public decimal sell;
            public decimal vol;
        }
    }
}