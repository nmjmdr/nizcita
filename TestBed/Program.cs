using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestBed {
    class Program {

        private static Random r = new Random((int)DateTime.Now.Ticks);

        static void Main(string[] args) {

            DownloadAll();

            char key = ' ';
            for(;key != 'q';) {
                key = Console.ReadKey().KeyChar;
            }            
        }

        

        public static bool DownloadAll() {

            Task[] tasks = new Task[10];

            for(int i=0;i<10;i++) {
                tasks[i] = Download(i);
            }

            Task.WaitAll(tasks);

            return true;
        }



        public static async Task<bool> Download(int i) {
            Console.WriteLine("Will attemp to download {0}", i);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            bool ok;
            ok = await get(i);
            if (ok) {
                ok = await copy(i);
            }
            watch.Stop();

            Console.WriteLine("Downloaded {0}, time taken - {1}", i, watch.Elapsed.TotalMilliseconds);
            return ok;
        }

        private static Task<bool> copy(int i) {
            Console.WriteLine("Will copy: {0}", i);
            return Task.Run(() => {
                int s = r.Next(500, 2000);
                Thread.Sleep(s);
                return true;
            });
        }

        private static Task<bool> get(int i) {
            Console.WriteLine("Will get: {0}", i);
            return Task.Run(() => {
                int s = r.Next(1500, 5000);
                Thread.Sleep(s);
                return true;
            });
        }
    }
}
