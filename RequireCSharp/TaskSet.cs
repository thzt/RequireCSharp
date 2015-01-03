using RequireCSharp;

namespace ConsoleApplication1
{
    public static class UserEvent
    {
        public sealed class Information { }

        public sealed class Progress { }
    }

    public class MainWorkflow : Subtask
    {
        public override void Execute()
        {
            Require<Task1>(Arguments);
            Export = "OK";
        }
    }


    public class Task1 : Subtask
    {
        public override void Execute()
        {
            int beginI = Arguments[0];
            int beginJ = Arguments[1];

            for (var i = beginI; i < 10; i++)
            {
                Require<Task2>(i, beginJ);
            }
        }
    }

    public class Task2 : Subtask
    {
        public override void Execute()
        {
            int currentI = Arguments[0];
            int beginJ = Arguments[1];

            for (var j = beginJ; j < 10; j++)
            {
                Require<Task3>(currentI, j);
            }
        }
    }

    public class Task3 : Subtask
    {
        public override void Execute()
        {
            int i = Arguments[0];
            int j = Arguments[1];

            System.Threading.Thread.Sleep(50);

            var product = i * j;
            Trigger<UserEvent.Progress>(product.ToString());
            Trigger<UserEvent.Information>(product.ToString());

            Export = product;
        }
    }
}
