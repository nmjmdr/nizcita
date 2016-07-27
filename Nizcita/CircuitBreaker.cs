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
        private CancellationToken calleeCancelToken;
        private CancellationToken timeCancelToken;        
        private Func<CancellationToken, Task<R>> alternateFn;
        private Action<Exception> exceptionIntercept;
        private volatile bool isOpen = true;
        private Func<R, bool> checkResult;
        private IMonitor monitor;
        private TimeSpan? withinTimespan;

        public CircuitBreaker(int bufferSz,IEnumerable<Func<Point[],bool>> reducers) : this(new Monitor(new ConcurrentLimitedBuffer<Point>(bufferSz), reducers)) {
        }        

        public CircuitBreaker(IMonitor monitor) {
            this.combinedCancelTokenSource = new CancellationTokenSource();
            this.monitor = monitor;
            this.monitor.Listen(alarmHandler);
        }

        private void alarmHandler(Alarm alarm) {
            // close the gate here
            isOpen = false;
        }

        public async Task<R> InvokeAsync(Func<CancellationToken, Task<R>> f) {

            // set time cancel token at this stage,
            // other wise there could be a delay between when the "WithinTimne" is invoked and when "InvokeAsync" is invoked
            setupTimeCancelToken();

            R r = default(R);                       

            if (!isOpen) {
                if (alternateFn != null) {
                    r = await alternateFn(calleeCancelToken);
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

            // check if the callee has requested cancel already
            if(calleeCancelToken.IsCancellationRequested) {
                return default(R);
            }

            // has the timeout already expired??
            if (timeCancelToken.IsCancellationRequested) {
                computeAlternate = true;
            } else {

                Stopwatch watch = new Stopwatch();
                try {
                    watch.Start();
                    r = await f(combinedCancelTokenSource.Token);
                    watch.Stop();

                    if (!checkResult(r)) {
                        computeAlternate = true;
                        logInvalidResult(watch.Elapsed, r);
                    }

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
            }

            if (computeAlternate && alternateFn != null) {
                r = await alternateFn(calleeCancelToken);
            }

            return r;
        }

        private void logInvalidResult(TimeSpan elapsed, R r) {
            monitor.Log(new Point { FailureType = FailureType.InvalidResult, TimeTaken = elapsed });
        }

        private void logFault(TimeSpan elapsed, Exception exp) {
            monitor.Log(new Point { Fault = exp, FailureType = FailureType.Fault, TimeTaken=elapsed });
        }

        private void logTimedOut(TimeSpan elapsed) {
            monitor.Log(new Point { FailureType = FailureType.TimedOut, TimeTaken = elapsed });
        }

        public CircuitBreaker<R> Cancellation(CancellationToken cancelToken) {
            this.calleeCancelToken = cancelToken;
            this.combinedCancelTokenSource = combineTokens(this.calleeCancelToken);
            return this;
        }
        
        
        public CircuitBreaker<R> WithinTime(TimeSpan timespan) {

            // Store the time stamp,
            withinTimespan = timespan;
            return this;
        }

        private void setupTimeCancelToken() {
            
            if(!withinTimespan.HasValue) {
                return;
            }
            CancellationTokenSource timeCancelTokenSource = new CancellationTokenSource();
            timeCancelTokenSource.CancelAfter(withinTimespan.Value);
            this.timeCancelToken = timeCancelTokenSource.Token;
            this.combinedCancelTokenSource = combineTokens(this.timeCancelToken);            
        }

        public CircuitBreaker<R> InterceptException(Action<Exception> interceptor) {
            this.exceptionIntercept = interceptor;
            return this;
        }

        public CircuitBreaker<R> Alternate(Func<CancellationToken,Task<R>> alternateFn) {
            this.alternateFn = alternateFn;
            return this;
        }

        public CircuitBreaker<R> CheckResult(Func<R,bool> check) {
            this.checkResult = check;
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
