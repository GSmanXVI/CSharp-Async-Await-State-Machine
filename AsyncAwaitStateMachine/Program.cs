using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitStateMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            StartFunc();
            Console.ReadKey();
        }

        static private async Task<double> Func1(double input)
        {
            return await Task.Run(() =>
            {
                Thread.Sleep(1000);
                return Math.Pow(input, 3);
            });
        }

        static private async Task<double> Func2(double input)
        {
            return await Task.Run(() =>
            {
                Thread.Sleep(1000);
                return Math.Sqrt(input);
            });
        }

        ////Before compilation
        //static private async void StartFunc()
        //{
        //    Console.WriteLine("Start");
        //    var number = 42;
        //    var result1 = await Func1(number);
        //    Console.WriteLine($"Result1: {result1}");
        //    var result2 = await Func2(number);
        //    Console.WriteLine($"Result2: {result2}");
        //    Console.WriteLine("End");
        //}

        //After compilation
        static private void StartFunc()
        {
            var stateMachine = new StartFuncStateMachine
            {
                State = -1,
                Builder = AsyncVoidMethodBuilder.Create()
            };
            stateMachine.Builder.Start(ref stateMachine);
        }

        struct StartFuncStateMachine : IAsyncStateMachine
        {
            // Runs the state machine
            public AsyncVoidMethodBuilder Builder;

            // State of the state machine
            // -1           Start
            //  0, 1, 2...  After first, second, third etc. await
            // -2           Finish
            public int State;

            // Each awaiter encapsulates awaiting method
            private TaskAwaiter<double> awaiter1;
            private TaskAwaiter<double> awaiter2;

            // All local variables from the async method
            private double number;
            private double result1;
            private double result2;

            public void MoveNext()
            {
                try
                {
                    switch (State)
                    {
                        // Initial state
                        case -1:
                            // Run code before first await
                            Console.WriteLine("Start");
                            number = 42;
                            // Initiliaze first await
                            awaiter1 = Func1(number).GetAwaiter();
                            if (awaiter1.IsCompleted)
                            {
                                // Go to the next state if task is already completed
                                goto case 0;
                            }
                            else
                            {
                                State = 0;
                                // Builder runs task and calls MoveNext() method again 
                                Builder.AwaitUnsafeOnCompleted(ref awaiter1, ref this);
                                // 1. Run the task encapsulated in awaiter in another thread
                                // 2. Switch SynchronizationContext to the main (UI) thread
                                // 3. Run MoveNext() method in the main (UI) thread
                                return;
                            }
                        //State after first await
                        case 0:
                            // Get result from the first awaited method
                            result1 = awaiter1.GetResult();
                            // Run code after first await
                            Console.WriteLine($"Result1: {result1}");
                            // Initiliaze second await
                            awaiter2 = Func2(number).GetAwaiter();
                            if (awaiter2.IsCompleted)
                            {
                                goto case 1;
                            }
                            else
                            {
                                State = 1;
                                Builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
                                return;
                            }
                        //State after second await
                        case 1:
                            result2 = awaiter2.GetResult();
                            Console.WriteLine($"Result2: {result2}");
                            Console.WriteLine("End");
                            State = -2; // End state
                            return;
                    }
                }
                catch (Exception exception)
                {
                    State = -2;
                    Builder.SetException(exception);
                }
            }

            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
                Builder.SetStateMachine(stateMachine);
            }
        }
    }
}
