using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestBed {
    class Program {
        static void Main(string[] args) {
            CircuitBreaker<int> cb = new CircuitBreaker<int>(2);


            int x = cb.Invoke((token) => {
                return Task.Run(() => {
                    return 10;
                });
            }
            ).Result;


            int div = 0;

            int y = cb.Invoke((token) => {
                return Task.Run(() => {
                    return 1 / div;
                });
            }
            ).Result;


            cb.WithinTime(new TimeSpan(0, 0, 0, 0, 10));

            int z = cb.Invoke((token) => {
                return getValueAysnc(token);
            }).Result;

            Console.WriteLine(z);
        }

        private static Task<int> getValueAysnc(CancellationToken token) {
            return Task.Run(() => {
                for (int i = 0; i < 10; i++) {
                    if (token.IsCancellationRequested) {
                        throw new TaskCanceledException();
                    } else {
                        Thread.Sleep(10);
                    }
                }
                return 20;
            }, token);
        }
    }
}
