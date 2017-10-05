﻿using System;
using System.Collections.Generic;
using System.Timers;
using System.Web.UI;
using GalaSoft.MvvmLight.Messaging;
using Prime.Utility;
using System.Linq;

namespace Prime.Core.Exchange.Rates
{
    public class ExchangeRateProvider : IDisposable
    {
        private readonly ExchangeRateProviderContext _context;
        private readonly IPublicPriceProvider _provider;
        private readonly UniqueList<ExchangeRateRequest> _verifiedRequests = new UniqueList<ExchangeRateRequest>();
        private readonly UniqueList<AssetPair> _pairRequests = new UniqueList<AssetPair>();
        private readonly ExchangeRatesCoordinator _coordinator;
        private readonly IMessenger _messenger;
        private readonly Timer _timer;
        public readonly Network Network;
        private readonly object _timerLock = new object();

        public ExchangeRateProvider(ExchangeRateProviderContext context)
        {
            _context = context;
            _provider = context.Provider;
            Network = _provider.Network;
            _coordinator = context.Coordinator;
            _messenger = context.Coordinator.Messenger;
            _timer = new Timer(10) {AutoReset = false};
            _timer.Elapsed += delegate { UpdateTick(); };
            _timer.Start();
        }

        private DateTime _utcLastUpdate = DateTime.MinValue;
        protected void UpdateTick()
        {
            lock (_timerLock)
            {
                if (_pairRequests.Count != 0 && !_utcLastUpdate.IsWithinTheLast(_context.PollingSpan))
                {
                    _utcLastUpdate = DateTime.UtcNow;
                    
                        Update();
                        IsFailing = true;
                }

                if (!_isDisposed)
                    _timer.Start();
            }
        }

        public void ManualUpdate()
        {
            lock (_timerLock)
            {
                _utcLastUpdate = DateTime.UtcNow;
                Update();
            }
        }

        public bool IsFailing { get; private set; }

        public void SyncVerifiedRequests(IEnumerable<ExchangeRateRequest> requests)
        {
            lock (_timerLock)
            {
                _verifiedRequests.Clear();
                _pairRequests.Clear();

                foreach (var req in requests)
                    AddVerifiedRequest(req);
            }
        }

        public void AddVerifiedRequest(ExchangeRateRequest request)
        {
            lock (_timerLock)
            {
                if (!request.IsVerified)
                    throw new ArgumentException($"You cant add an un-verified {request.GetType()} to {GetType()}");

                _verifiedRequests.Add(request);
                _pairRequests.Add(request.PairRequestable);
            }
        }

        public bool HasRequests()
        {
            lock (_timerLock)
            {
                return _pairRequests.Any();
            }
        }

        private void Update()
        {
            if (_isDisposed)
                return;

            var hasresult = false;

            foreach (var pair in _pairRequests)
            {
                if (_isDisposed)
                    return;

                var r = ApiCoordinator.GetLatestPrice(_provider, new PublicPriceContext(pair));
                if (r.IsNull)
                {
                    IsFailing = true;
                    continue;
                }

                if (_isDisposed)
                    return;

                hasresult = true;
                AddResult(pair, r.Response);
            }

            if (hasresult && !_isDisposed)
                _messenger.Send(new ExchangeRatesUpdatedMessage());
        }

        private void AddResult(AssetPair pair, LatestPrice response)
        {
            var requests = _verifiedRequests.Where(x => x.PairRequestable.Equals(pair));

            foreach (var request in requests)
                Collect(response, request);
        }

        private void Collect(LatestPrice response, ExchangeRateRequest request)
        {
            var collected = request.LastCollected = new ExchangeRateCollected(request, _provider, request.IsConverted ? request.PairRequestable : request.Pair, response, request.Providers.IsReversed);

            _messenger.Send(collected);

            if (request.IsConverted)
                SendConverted(request, collected, request.ConvertedOther?.LastCollected);
        }

        private void SendConverted(ExchangeRateRequest request, ExchangeRateCollected recent, ExchangeRateCollected other)
        {
            if (other != null)
                _messenger.Send(new ExchangeRateCollected(request, recent, other));
        }

        private bool _isDisposed;
        public void Dispose()
        {
            _isDisposed = true;
               _timer?.Dispose();
        }
    }
}