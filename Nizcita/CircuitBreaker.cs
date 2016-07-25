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
        private CancellationTokenSource combinedCancelTokenSource;
        private CancellationToken cancelToken;
        private CancellationToken timeCancelToken;        
        private Func<CancellationToken, Task<R>> alternateFn;
        private Action<Exception> exceptionIntercept;
        private volatile bool isOpen = true;

        public CircuitBreaker() {
            this.combinedCancelTokenSource = new CancellationTokenSource();           
        }

        public async Task<R> InvokeAysnc(Func<CancellationToken, Task<R>> f) {

            R r = default(R);

            if (!isOpen) {
                if (alternateFn != null) {
                    r = await alternateFn(cancelToken);
                }
                return r;
            }

            r = await invokeAsyncInternal(f);
            return r;
        }

        public bool IsOpen {
            get {
                return isOpen;
            }
        }

        public void Close() {
            isOpen = false;
        }

        private async Task<R> invokeAsyncInternal(Func<CancellationToken, Task<R>> f) {
            bool computeAlternate = false;
            R r = default(R);

            Stopwatch watch = new Stopwatch();


            try {
                watch.Start();
                r = await f(combinedCancelTokenSource.Token);
                watch.Stop();
            } catch (OperationCanceledException) {
                watch.Stop();
                if (timeCancelToken.IsCancellationRequested) {
                    // log as Fault
                    logTimedOut(watch.Elapsed);
                    computeAlternate = true;
                }
                //else - cancellation was requested by the callee, return default value, do not log as fault                
            } catch (Exception exp) {
                watch.Stop();
                exceptionIntercept?.Invoke(exp);
                logFault(watch.Elapsed, exp);
                computeAlternate = true;
            }

            if (computeAlternate && alternateFn != null) {
                r = await alternateFn(cancelToken);
            }

            return r;
        }

        private void logFault(TimeSpan elapsed, Exception exp) {
           // will implement later
        }

        private void logTimedOut(TimeSpan elapsed) {
            // will implement later
        }

        public CircuitBreaker<R> Cancellation(CancellationToken cancelToken) {
            this.cancelToken = cancelToken;
            this.combinedCancelTokenSource = combineTokens(this.cancelToken);
            return this;
        }
        
        
        public CircuitBreaker<R> WithinTime(TimeSpan timespan) {
            CancellationTokenSource timeCancelTokenSource = new CancellationTokenSource();
            timeCancelTokenSource.CancelAfter(timespan);
            this.timeCancelToken = timeCancelTokenSource.Token;

            this.combinedCancelTokenSource = combineTokens(this.timeCancelToken);
            return this;
        }

        public CircuitBreaker<R> InterceptException(Action<Exception> interceptor) {
            this.exceptionIntercept = interceptor;
            return this;
        }

        public CircuitBreaker<R> Alternate(Func<CancellationToken,Task<R>> alternateFn) {
            this.alternateFn = alternateFn;
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
