using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita
{
    public class CircuitBreaker<R>
    {
        private int bufferSz;
        private CancellationTokenSource combinedCancelTokenSource;
        private CancellationToken cancelToken;
        private CancellationToken timeCancelToken;

        

        public CircuitBreaker(int bufferSz) {
            this.bufferSz = bufferSz;
        }

        public async Task<R> Invoke(Func<CancellationToken, Task<R>> f) {
            R r = await f(combinedCancelTokenSource.Token);

            return r;
        }

        public CircuitBreaker<R> WithinTime(TimeSpan timespan) {
            CancellationTokenSource timeCancelTokenSource = new CancellationTokenSource();
            timeCancelTokenSource.CancelAfter(timespan);
            this.timeCancelToken = timeCancelTokenSource.Token;

            this.combinedCancelTokenSource = combineTokens(this.timeCancelToken);
            return this;
        }

        private CancellationTokenSource combineTokens(CancellationToken t) {
            CancellationToken[] tokens;
            if (combinedCancelTokenSource != null) {
                tokens = new CancellationToken[] { combinedCancelTokenSource.Token, t };
            } else {
                tokens = new CancellationToken[] { t };
            }
            return CancellationTokenSource.CreateLinkedTokenSource(tokens);
        }
    }
}
