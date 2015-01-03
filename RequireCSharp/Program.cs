using System;
using System.Linq;

using RequireCSharp;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            TestAsyncTask();
            Console.ReadLine();
        }

        private static void TestAsyncTask()
        {
            var asyncTask = new AsyncTask();

            try
            {
                asyncTask.Execute<MainWorkflow>(3, 4);

                while (asyncTask.IsRunning)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.White;
                    var progressList = asyncTask.Collect<UserEvent.Progress>();
                    if (progressList.Count == 0)
                    {
                        continue;
                    }
                    Console.WriteLine(progressList.Last());

                    System.Threading.Thread.Sleep(50);
                }

                string result = asyncTask.Result;
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
        }

        private static void TestSyncCall()
        {
            var syncTask = new SyncTask();

            try
            {
                string result = syncTask.Execute<MainWorkflow>(3, 4);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.White;
                syncTask.Collect<UserEvent.Information>().ForEach(Console.WriteLine);
            }
        }
    }
}
