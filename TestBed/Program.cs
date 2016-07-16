using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBed {
    class Program {
        static void Main(string[] args) {
            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => {
                return -1;
            }).OnResult((r) => {
                return r == 10;
            }).Return(() => {
                return 20;
            });

            int x =cb.Invoke(() => {
                return 10;
            }).Result;
            
        }
    }
}
