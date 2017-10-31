﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;
using Prime.Common;
using Prime.Utility;

namespace Prime.Plugins.Services.Bithumb
{
    public class BithumbProvider : IExchangeProvider, IPublicPricesProvider
    {
        private const string BithumbApiUrl = "https://api.bithumb.com/";
        private RestApiClientProvider<IBithumbApi> ApiProvider { get; }

        private static readonly ObjectId IdHash = "prime:Bithumb".GetObjectIdHashCode();
        public ObjectId Id => IdHash;
        public Network Network { get; } = new Network("Bithumb");
        public bool Disabled => false;
        public int Priority => 100;
        public string AggregatorName => null;
        public string Title => Network.Name;

        // 20 requests available per second.
        // See https://www.bithumb.com/u1/US127.
        private static readonly IRateLimiter Limiter = new PerMinuteRateLimiter(1200, 1);

        public IRateLimiter RateLimiter => Limiter;

        public BithumbProvider()
        {
            ApiProvider = new RestApiClientProvider<IBithumbApi>(BithumbApiUrl, this, k => null);
        }

        public IAssetCodeConverter GetAssetCodeConverter()
        {
            return null;
        }

        public async Task<AssetPairs> GetAssetPairs(NetworkProviderContext context)
        {
            var api = ApiProvider.GetApi(context);
            var r = await api.GetTickers();

            var pairs = new AssetPairs();
            var krwAsset = "KRW".ToAsset(this);

            foreach (var rTicker in r.data)
            {
                var pair = new AssetPair(rTicker.Key.ToAsset(this), krwAsset);
                pairs.Add(pair);
            }

            return pairs;
        }

        public async Task<LatestPrice> GetPriceAsync(PublicPriceContext context)
        {
            var api = ApiProvider.GetApi(context);
            var currency = context.Pair.Asset1.ToRemoteCode(this);

            var r = await api.GetTicker(currency);

            var latestPrice = new LatestPrice()
            {
                UtcCreated = DateTime.UtcNow,
                Price = new Money((r.data.max_price + r.data.min_price) / 2, "KRW".ToAssetRaw()),
                QuoteAsset = context.Pair.Asset1
            };

            return latestPrice;
        }

        public async Task<List<LatestPrice>> GetAssetPricesAsync(PublicAssetPricesContext context)
        {
            return await GetPricesAsync(context);
        }

        public async Task<List<LatestPrice>> GetPricesAsync(PublicPricesContext context)
        {
            var api = ApiProvider.GetApi(context);
            var rRaw = await api.GetTickers();
            var r = ParseTickerResponse(rRaw);

            var krwAsset = "KRW".ToAsset(this);

            var prices = new List<LatestPrice>();

            foreach (var pair in context.Pairs)
            {
                if (!pair.Asset2.Equals(krwAsset))
                    throw new ApiResponseException("Exchange does not support quote currencies other than KRW", this);

                var rTickers = r.Where(x => x.Key.ToAsset(this).Equals(pair.Asset1)).ToArray();
                if (rTickers.Length < 1)
                    throw new ApiResponseException($"Exchange does not support {pair} currency pair", this);

                var rTiker = rTickers[0];
                prices.Add(new LatestPrice()
                {
                    UtcCreated = DateTime.UtcNow,
                    Price = new Money((rTiker.Value.max_price + rTiker.Value.min_price) / 2, pair.Asset2),
                    QuoteAsset = pair.Asset1
                });
            }

            return prices;
        }

        private Dictionary<string, BithumbSchema.TickerResponse> ParseTickerResponse(BithumbSchema.TickersResponse raw)
        {
            var dict = new Dictionary<string, BithumbSchema.TickerResponse>();

            foreach (var rPair in raw.data)
            {
                try
                {
                    var parsedValue = JsonConvert.DeserializeObject<BithumbSchema.TickerResponse>(rPair.Value.ToString());

                    dict.Add(rPair.Key, parsedValue);
                }
                catch (Exception e)
                {
                    continue;
                }
            }

            return dict;
        }

        public BuyResult Buy(BuyContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public SellResult Sell(SellContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}