
namespace WordlistSmith
{
    public class Smither
    {
        public string Url, Output, Agent, User, Pass;
        public int Min, Max, Depth, Delay, Threads, MaxPages, Timeout;

        public Smither()
        {
            this.Url = Program.Options.Instance.Url;
            this.Min = Program.Options.Instance.Minimum;
            this.Max = Program.Options.Instance.Maximum;
            this.Output = Program.Options.Instance.Output;
            this.Depth = Program.Options.Instance.Depth;
            this.Threads = Program.Options.Instance.Threads;
            this.Delay = Program.Options.Instance.Delay;
            this.Agent = Program.Options.Instance.Agent;
            this.MaxPages = Program.Options.Instance.MaxPages;
            this.Timeout = Program.Options.Instance.Timeout;
            this.User = Program.Options.Instance.User;
            this.Pass = Program.Options.Instance.Pass;
        }
    }
}