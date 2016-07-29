using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita
{
    public enum State {
        Open,
        Close
    }
    public delegate void CircuitStateChangedHandler(State state);
        

    public class CircuitBreaker<T>
    {
        private const int DefaultProbeThreshold = 5;

        private CancellationTokenSource combinedCancelTokenSource;
        private CancellationToken calleeCancelToken;
        private CancellationToken timeCancelToken;        
        private Func<CancellationToken, Task<T>> alternateFn;
        private Action<Exception> exceptionIntercept;
        private volatile bool isOpen = true;
        private Func<T, bool> checkResult;
        private IMonitor monitor;
        private TimeSpan? withinTimespan;
        private CircuitStateChangedHandler circuitStateChangedEvt;
        private Func<int, bool> probeStrategy;
        private volatile int closedCallCounter = 0;
        private object lockObj = new object();

        public CircuitBreaker(int bufferSz,IEnumerable<Func<Point[],bool>> reducers) : this(new Monitor(new ConcurrentLimitedBuffer<Point>(bufferSz), reducers)) {
        }

        public CircuitBreaker(int bufferSz, int reduceAfterNFailures, IEnumerable<Func<Point[], bool>> reducers) : this(new Monitor(new ConcurrentLimitedBuffer<Point>(bufferSz), reducers, reduceAfterNFailures)) {
        }

        public CircuitBreaker(IMonitor monitor) {
            this.combinedCancelTokenSource = new CancellationTokenSource();
            this.monitor = monitor;
            this.monitor.Listen(alarmHandler);
        }

        private void alarmHandler(Alarm alarm) {
            // close the gate here
            isOpen = false;
            circuitStateChangedEvt?.Invoke(State.Close);
        }

        public CircuitBreaker<T> OnCircuitStateChanged(CircuitStateChangedHandler handler) {
            this.circuitStateChangedEvt += handler;
            return this;
        }

        public CircuitBreaker<T> ProbeStrategy(Func<int,bool> p) {
            this.probeStrategy = p;
            return this;
        }

        public async Task<T> InvokeAsync(Func<CancellationToken, Task<T>> f) {

            // set time cancel token at this stage,
            // other wise there could be a delay between when the "WithinTimne" is invoked and when "InvokeAsync" is invoked
            setupTimeCancelToken();

            T r = default(T);
            

            if (!isOpen) {
                bool probe = shouldProbe(closedCallCounter);

                if (!probe) {
                    closedCallCounter++;

                    if (alternateFn != null) {
                        r = await alternateFn(calleeCancelToken);
                    }
                    return r;
                } else {
                    lock (lockObj) {
                        closedCallCounter = 0;
                    }
                    r = await attemptProbe(f);
                }
            } else {

                r = await invokeAsyncInternal(f, (p) => {
                    monitor.Log(p);
                    if (p.FailureType == FailureType.Fault) {
                        exceptionIntercept?.Invoke(p.Fault);
                    }
                });
            }
            return r;
        }

        private async Task<T> attemptProbe(Func<CancellationToken, Task<T>> f) {
            bool probeSuccessful = true;
            T r = await invokeAsyncInternal(f, (p) => {
                // it is a probe, do not log to monitor
                // invoke exception handlers
                if (p.FailureType == FailureType.Fault) {
                    exceptionIntercept?.Invoke(p.Fault);
                }
                // we got a failure, probe failed
                probeSuccessful = false;
            });

            if(probeSuccessful) {
                // open the gate
                isOpen = true;
                circuitStateChangedEvt?.Invoke(State.Open);
            }
            return r;
        }


        private bool shouldProbe(int counter) {
            if(probeStrategy != null) {
                return probeStrategy(counter);
            }
            // apply the default proble strategy
            return defaulProbeStrategy(counter);
        }

        private bool defaulProbeStrategy(int counter) {
            return counter >= DefaultProbeThreshold;
        }

        public bool IsOpen {
            get {
                return isOpen;
            }
        }

        public void Close() {
            isOpen = false;
        }

        private async Task<T> invokeAsyncInternal(Func<CancellationToken, Task<T>> f,Action<Point> onInvokeFailed) {

            bool computeAlternate = false;
            T r = default(T);

            // check if the callee has requested cancel already
            if(calleeCancelToken.IsCancellationRequested) {
                return default(T);
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

                    if (checkResult != null && !checkResult(r)) {
                        computeAlternate = true;
                        onInvokeFailed(new Point { FailureType = FailureType.InvalidResult, TimeTaken = watch.Elapsed });
                    }

                } catch (OperationCanceledException) {
                    watch.Stop();
                    if (timeCancelToken.IsCancellationRequested) {
                        // log as Fault
                        computeAlternate = true;
                        onInvokeFailed(new Point { FailureType = FailureType.TimedOut, TimeTaken = watch.Elapsed });                        
                    }
                    //else - cancellation was requested by the callee, return default value, do not log as fault                
                } catch (Exception exp) {
                    watch.Stop();
                    computeAlternate = true;
                    onInvokeFailed(new Point { FailureType = FailureType.Fault, TimeTaken = watch.Elapsed, Fault = exp });
                }
            }

            if (computeAlternate && alternateFn != null) {
                r = await alternateFn(calleeCancelToken);
            }

            return r;
        }

        private void invokeFailedHandler(Point point) {
            
        }

        private void invokeFailedHandlerForProbe(Point point) {

        }

        private void logInvalidResult(TimeSpan elapsed) {
            monitor.Log(new Point { FailureType = FailureType.InvalidResult, TimeTaken = elapsed });
        }

        private void logFault(TimeSpan elapsed, Exception exp) {
            monitor.Log(new Point { Fault = exp, FailureType = FailureType.Fault, TimeTaken=elapsed });
        }

        private void logTimedOut(TimeSpan elapsed) {
            monitor.Log(new Point { FailureType = FailureType.TimedOut, TimeTaken = elapsed });
        }

        public CircuitBreaker<T> Cancellation(CancellationToken cancelToken) {
            this.calleeCancelToken = cancelToken;
            this.combinedCancelTokenSource = combineTokens(this.calleeCancelToken);
            return this;
        }
        
        
        public CircuitBreaker<T> WithinTime(TimeSpan timespan) {

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

        public CircuitBreaker<T> InterceptException(Action<Exception> interceptor) {
            this.exceptionIntercept = interceptor;
            return this;
        }

        public CircuitBreaker<T> Alternate(Func<CancellationToken,Task<T>> alternateFn) {
            this.alternateFn = alternateFn;
            return this;
        }

        public CircuitBreaker<T> CheckResult(Func<T,bool> check) {
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
