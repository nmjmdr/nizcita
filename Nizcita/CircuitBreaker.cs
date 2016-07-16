using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita
{
    public class CircuitBreaker<R>
    {
       
        public interface IEvalReturn { CircuitBreaker<R> Return(Func<R> fn); };

        private class EvalReturn : IEvalReturn {
            public CircuitBreaker<R> parent;
            public Func<R> fn { get; set; }
            public CircuitBreaker<R> Return(Func<R> fn) {
                this.fn = fn;
                return parent;
            }
        }
        private class ConditionalReturn : EvalReturn {
            public Func<R, bool> cond { get; set; }
        }
        private List<ConditionalReturn> conditionals = new List<ConditionalReturn>();
        private CancellationToken timeCancelToken;
        private CancellationToken calleeCancelToken;
        private CancellationTokenSource combinedCancelTokenSource = new CancellationTokenSource();
        private Func<R> alternativeFn;
                    
       

        public IEvalReturn OnResult(Func<R,bool> cd) {
            ConditionalReturn c = new ConditionalReturn { cond = cd, parent=this };
            this.conditionals.Add(c);
            return c;
        }
                

        public async Task<R> Invoke(Func<R> f) {

            R r = default(R);

            try {
                return await Task.Run(() => f(), combinedCancelTokenSource.Token).ContinueWith((t) => {
                    if (t.IsCompleted) {
                        r = conditionalToReturn(t.Result);
                        return r;
                    } else {
                        return r;
                    }
                });
            } catch (AggregateException tc) {

                var exceptions = tc.Flatten().InnerExceptions;
                List<Exception> toRethrow = new List<Exception>();
                foreach (Exception e in exceptions) {
                    if( e is TaskCanceledException && timeCancelToken.IsCancellationRequested) {
                        // timed out, invoke the alternate
                        return await Task.Run(() => {
                            return alternativeFn != null ? alternativeFn() : default(R);
                        });
                    } else if (e is TaskCanceledException && calleeCancelToken.IsCancellationRequested) {
                        // callee cancelled, return default
                        return r;
                    } else {
                        toRethrow.Add(e);
                    }
                }

                if(toRethrow.Count > 0) {
                    throw new AggregateException(toRethrow);
                }                            
                return r;
            }
            
        }

        private R conditionalToReturn(R r) {
            foreach (ConditionalReturn c in conditionals) {
                if (c.cond(r)) {
                    r = c.fn();
                    break;
                }
            }

            return r;
        }

        private async Task<R> handleTimeout() {
            R r;
            // timeout, return the alternative
            r = await Task<R>.Run(() => {
                return this.alternativeFn != null ? this.alternativeFn() : default(R);
            });
            return r;
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
