using Nizcita;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestBed {
    class Program {

       
        static void Main(string[] args) {

            int failureThreshold = 2;
            List<Func<Point[], bool>> reducers = new List<Func<Point[], bool>>();
            reducers.Add((points) => {
                
                if (points.Count() > failureThreshold) {
                    return true;
                }
                return false;
            });

            CircuitBreaker<HttpResponseMessage> cb = new CircuitBreaker<HttpResponseMessage>(10, reducers).Alternate((token) => {

                HttpClient client = new HttpClient();
                return client.GetAsync("https://www.bing.com/news", HttpCompletionOption.ResponseContentRead);
            }).CheckResult((r) => {
                return r.IsSuccessStatusCode;
            }).WithinTime(new TimeSpan(0, 0, 0, 0, 200));

            cb.InvokeAsync((token) => {
                HttpClient client = new HttpClient();
                return client.GetAsync("https://google.com/news", HttpCompletionOption.ResponseContentRead);
            }).ContinueWith((t) => {
                Console.WriteLine("Got news: {0}",t.Result.StatusCode);
            });
            
            // do other work
            for(; Console.ReadKey().KeyChar != 'q';) {                
            }
            
        }

        

        
    }
}
