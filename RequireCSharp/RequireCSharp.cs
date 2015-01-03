using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace RequireCSharp
{
    public sealed class AsyncTask
    {
        private bool isRunning;
        private Thread taskThread;
        private Exception threadException;

        private readonly MessageContainer container = new MessageContainer();

        public bool IsRunning
        {
            get
            {
                if (threadException != null)
                {
                    throw threadException;
                }

                return isRunning;
            }
        }

        public dynamic Result
        {
            get;
            private set;
        }

        public void Execute<T>(params dynamic[] parameters) where T : Subtask
        {
            isRunning = true;

            taskThread = new Thread(() =>
            {
                try
                {
                    TaskCore.Begin();
                    TaskCore.Bind(container.Add);

                    Result = TaskCore.Require<T>(parameters);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
                finally
                {
                    TaskCore.End();
                    isRunning = false;
                }
            });

            taskThread.Start();
        }

        public List<string> Collect<T>()
        {
            return container.Collect(typeof(T).FullName);
        }

        public void Abort()
        {
            taskThread.Abort();
        }
    }

    public sealed class SyncTask
    {
        private readonly MessageContainer container = new MessageContainer();

        public dynamic Execute<T>(params dynamic[] parameters) where T : Subtask
        {
            try
            {
                TaskCore.Begin();
                TaskCore.Bind(container.Add);

                return TaskCore.Require<T>(parameters);
            }
            finally
            {
                TaskCore.End();
            }
        }

        public List<string> Collect<T>()
        {
            return container.Collect(typeof(T).FullName);
        }
    }

    public abstract class Subtask
    {
        public dynamic[] Arguments { get; set; }

        public dynamic Export { get; set; }

        public abstract void Execute();

        public dynamic Require<T>(params dynamic[] parameters) where T : Subtask
        {
            return TaskCore.Require<T>(parameters);
        }

        public static dynamic Require<T1, T2>(ref bool received, params dynamic[] parameters)
            where T1 : Subtask
            where T2 : Subtask
        {
            return TaskCore.Require<T1, T2>(ref received, parameters);
        }

        public void Trigger<T>(string message)
        {
            TaskCore.Trigger<T>(message);
        }
    }

    internal static partial class TaskCore
    {
        public static void Begin()
        {
            TaskStack.Push(new
            {
                HistoryDict = new HistoryDict(),
                EventManager = new EventManager()
            });
        }

        public static void End()
        {
            TaskStack.ReleaseTailResource();
        }
    }

    internal static partial class TaskCore
    {
        public static void Bind(Action<string, string> collector)
        {
            TaskStack.Tail.EventManager.Bind(collector);
        }

        public static void Trigger<T>(string message)
        {
            TaskStack.Tail.EventManager.Trigger(typeof(T).FullName, message);
        }
    }

    internal static partial class TaskCore
    {
        public static dynamic Require<T>(params dynamic[] parameters) where T : Subtask
        {
            HistoryDict historyDict = TaskStack.Tail.HistoryDict;

            var taskName = typeof(T).FullName;
            var existTask = historyDict.Contain(taskName);

            if (existTask && parameters.Length == 0)
            {
                return historyDict.Get(taskName).Export;
            }

            var task = existTask
                ? historyDict.Get(taskName)
                : historyDict.Add(typeof(T).FullName, Activator.CreateInstance<T>());

            task.Arguments = parameters;
            task.Execute();

            return task.Export;
        }
    }

    internal static partial class TaskCore
    {
        public static dynamic Require<T1, T2>(ref bool received, params dynamic[] parameters)
            where T1 : Subtask
            where T2 : Subtask
        {
            if (received)
            {
                return null;
            }

            bool satisfyCondition = Require<T1>(parameters);
            if (!satisfyCondition)
            {
                return null;
            }

            received = true;
            return Require<T2>(parameters);
        }
    }

    internal static class TaskStack
    {
        private static readonly List<dynamic> taskStack = new List<dynamic>();

        public static void Push(object history)
        {
            taskStack.Add(history);
        }

        public static dynamic Tail
        {
            get
            {
                return taskStack.Last();
            }
        }

        public static void ReleaseTailResource()
        {
            taskStack.Last().HistoryDict.Clear();
            taskStack.Last().EventManager.Unbind();

            taskStack.RemoveAt(taskStack.Count - 1);
        }
    }

    internal class HistoryDict
    {
        private readonly Dictionary<string, Subtask> historyDict = new Dictionary<string, Subtask>();

        public void Clear()
        {
            historyDict.Clear();
        }

        public bool Contain(string taskName)
        {
            return historyDict.ContainsKey(taskName);
        }

        public Subtask Add(string taskName, Subtask task)
        {
            historyDict[taskName] = task;

            return task;
        }

        public Subtask Get(string taskName)
        {
            return historyDict[taskName];
        }
    }

    internal class EventManager
    {
        private Action<string, string> collector;

        public void Bind(Action<string, string> collector)
        {
            this.collector = collector;
        }

        public void Unbind()
        {
            collector = null;
        }

        public void Trigger(string eventType, string message)
        {
            if (collector == null)
            {
                return;
            }

            collector.Invoke(eventType, message);
        }
    }

    internal class MessageContainer
    {
        private readonly List<KeyValuePair<string, string>> container = new List<KeyValuePair<string, string>>();
        private static readonly object asyncLock = new object();

        public void Add(string eventType, string message)
        {
            lock (asyncLock)
            {
                container.Add(new KeyValuePair<string, string>(eventType, message));
            }
        }

        public List<string> Collect(string eventType)
        {
            lock (asyncLock)
            {
                return (from pair in container
                        where pair.Key == eventType
                        select pair.Value).ToList();
            }
        }
    }
}