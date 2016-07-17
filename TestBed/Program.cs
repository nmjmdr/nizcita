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
            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => {
                Thread.Sleep(2000);
                return -1;
            });

            Task t = cb.Invoke(() => {
                throw new DivideByZeroException();
            }).ContinueWith((c) => {
            Console.WriteLine("Got value: " + c.Result);
            });
            
            for (;;) {
                if( Console.ReadKey().Key == ConsoleKey.Q) {
                    break;
                }
            }

            t.Wait();
        }
    }
}
