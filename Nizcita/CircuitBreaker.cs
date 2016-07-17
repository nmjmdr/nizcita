using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita
{
    public class CircuitBreaker<R> {

        private CancellationToken timeCancelToken;
        private CancellationToken calleeCancelToken;
        private CancellationTokenSource combinedCancelTokenSource = new CancellationTokenSource();
        private Func<R> alternativeFn;
        private bool isOpen = true;
        private Action<IEnumerable<Exception>> exceptionsHandler;

        public CircuitBreaker(int bufferSize) {
        }


        public async Task<R> Invoke(Func<R> f) {
                      

            if(isOpen) {
                return await Task.Run(() => {
                    return f();
                },combinedCancelTokenSource.Token).ContinueWith((t) => {
                    if (t.Status == TaskStatus.RanToCompletion) {
                        return t.Result;
                    } else if (t.Status == TaskStatus.Canceled && timeCancelToken.IsCancellationRequested) {
                        // log as timed out
                        return this.alternativeFn();
                    } else if (t.Status == TaskStatus.Canceled && calleeCancelToken.IsCancellationRequested) {
                        // callee requested cancel, just return the default
                        return default(R);
                    } else {
                        // log faults
                        if(this.exceptionsHandler != null && t.Exception != null) {
                            exceptionsHandler(t.Exception.Flatten().InnerExceptions);
                        }
                        return this.alternativeFn();
                    }
                });
            } else {
                return await Task.Run(() => {
                    return this.alternativeFn();
                });
            }                     
        }

        public CircuitBreaker<R> OnExceptions(Action<IEnumerable<Exception>> exceptionsHandler) {
            this.exceptionsHandler = exceptionsHandler;
            return this;
        }

        public void Close() {
            isOpen = false;
        }      
        
       
        public CircuitBreaker<R> WithinTime(TimeSpan t) {
            this.timeCancelToken = new CancellationTokenSource(t).Token;            
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

        

        public CircuitBreaker<R> Alternate(Func<R> f) {
            this.alternativeFn = f;
            return this;
        }

        public CircuitBreaker<R> CancelToken(CancellationToken ct) {
            this.calleeCancelToken = ct;
            this.combinedCancelTokenSource = combineTokens(this.calleeCancelToken);
            return this;            
        }
    }
}
