using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool isOpen = true;
        

        public CircuitBreaker(int bufferSz) {
            this.combinedCancelTokenSource = new CancellationTokenSource();
            this.bufferSz = bufferSz;
        }

        public async Task<R> Invoke(Func<CancellationToken, Task<R>> f) {
            
            Stopwatch watch = new Stopwatch();
            watch.Start();
            return await f(combinedCancelTokenSource.Token).ContinueWith<R>((t) => {
                watch.Stop();
                return processTaskReturn(t, watch.Elapsed);
            });
            
        }

        private R processTaskReturn(Task<R> t,TimeSpan ts) {

            if (timeCancelToken.IsCancellationRequested || t.IsCanceled) {
                return default(R);
            } else if (t.Status == TaskStatus.RanToCompletion && t.Status != TaskStatus.Faulted) {
                return t.Result;
            } else {
                return default(R);
            }
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
