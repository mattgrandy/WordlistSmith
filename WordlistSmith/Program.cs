using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using CommandLine;
using CommandLine.Text;
using NUglify;
using Serilog;

namespace WordlistSmith
{
    class Program
    {
        private static HashSet<string> wordlist = new HashSet<string>();
        public static Smithy smithy;

        static async Task Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Parse arguments passed
            Parser parser = new Parser(with =>
            {
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.HelpWriter = null;
            });

            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithParsed<Options>(o => { Options.Instance = o; })
                .WithNotParsed(errs => DisplayHelp(parserResult, errs));
            Options options = Options.Instance;

            try
            {
                smithy = new Smithy();

                if (options.Quiet)
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Error()
                        .WriteTo.Console()
                        .CreateLogger();
                }

                else
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Console()
                        .CreateLogger();
                }

                await DoCrawl(smithy);

                Uri uri = new Uri(smithy.Url);
                string safeUri = uri.Authority;
                safeUri = safeUri.Replace('.', '_');

                if (string.IsNullOrEmpty(smithy.Output))
                {
                    smithy.Output = "wordlist_" + safeUri + DateTime.Now.ToString("_HH-mm-ss") + ".txt";
                    
                    if (smithy.Output.Length > 250)
                        smithy.Output = "wordlist_" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + ".txt";
                }

                Console.WriteLine($"\n[+] Crawl finished, writing to file: {smithy.Output}\n");

                using (StreamWriter outfile = new StreamWriter(smithy.Output))
                {
                    foreach (var word in wordlist)
                        await outfile.WriteLineAsync(word);
                }

                watch.Stop();
                Console.WriteLine("Execution time: " + watch.ElapsedMilliseconds / 1000 + " Seconds");
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task DoCrawl(Smithy smithy)
        {
            var config = new CrawlConfiguration();
            if (!string.IsNullOrEmpty(smithy.User) && !string.IsNullOrEmpty(smithy.Pass))
            {
                config.MaxConcurrentThreads = smithy.Threads;
                config.MaxCrawlDepth = smithy.Depth;
                config.MinCrawlDelayPerDomainMilliSeconds = smithy.Delay;
                config.MaxPagesToCrawl = smithy.MaxPages;
                config.MaxRetryCount = 1;
                //HttpServicePointConnectionLimit = 2000,
                config.HttpRequestTimeoutInSeconds = smithy.Timeout;
                config.LoginUser = smithy.User;
                config.LoginPassword = smithy.Pass;
            }

            if (!string.IsNullOrEmpty(smithy.User) || !string.IsNullOrEmpty(smithy.Pass))
            {
                if (string.IsNullOrEmpty(smithy.Pass) || string.IsNullOrEmpty(smithy.User))
                {
                    Console.WriteLine("Please specify both a username and a password if using basic auth");
                    System.Environment.Exit(1);
                }
            }

            else
            {
                config.MaxConcurrentThreads = smithy.Threads;
                config.MaxCrawlDepth = smithy.Depth;
                config.MinCrawlDelayPerDomainMilliSeconds = smithy.Delay;
                config.MaxPagesToCrawl = smithy.MaxPages;
                config.MaxRetryCount = 1;
                //HttpServicePointConnectionLimit = 2000,
                config.HttpRequestTimeoutInSeconds = smithy.Timeout;
            }

            var crawler = new PoliteWebCrawler(config);

            crawler.PageCrawlCompleted += PageCrawlCompleted;

            var crawlResult = await crawler.CrawlAsync(new Uri(smithy.Url));
        }

        private static async Task DemoSinglePageRequest()
        {
            var pageRequester = new PageRequester(new CrawlConfiguration(), new WebContentExtractor());

            var crawledPage = await pageRequester.MakeRequestAsync(new Uri("http://msn.com"));
            Log.Logger.Information("{result}", new
            {
                url = crawledPage.Uri,
                status = Convert.ToInt32(crawledPage.HttpResponseMessage.StatusCode)
            });
        }

        private static void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var httpStatus = e.CrawledPage.HttpResponseMessage?.StatusCode;
            var rawPageText = e.CrawledPage.Content.Text;
            char[] charsToRemove =
            {
                ' ', ';', '.', '!', ':', '=', '@', '#', '$', '"', '%', '^', '&', '*', '(', ')', '<', '>', '?', '\'',
                ',', '-'
            };

            if (httpStatus == HttpStatusCode.OK)
            {
                //Log.Logger.Information("{result}", new
                //{
                // url = e.CrawledPage.Uri,
                // status = Convert.ToInt32(e.CrawledPage.HttpResponseMessage.StatusCode)
                //});

                //lock (rawPageText)
                //{
                List<string> tempList = new List<string>();

                var result = Uglify.HtmlToText(rawPageText).ToString();
                foreach (var word in result.Split())
                {
                    string cleanWord = word.ToLower().Trim(charsToRemove);

                    if (cleanWord.Length > smithy.Min && cleanWord.Length < 256)
                    {
                        if (!wordlist.Contains(cleanWord))
                            tempList.Add(cleanWord);
                    }
                }

                foreach (var word in tempList)
                    wordlist.Add(word);
                //}
            }
        }

        public class Options
        {
            public static Options Instance { get; set; }

            // Command line options
            [Option('q', "quiet", Required = false, HelpText = "Do not log anything to the screen")]
            public bool Quiet { get; set; }

            [Option('u', "url", Required = true, HelpText = "Specify a URL to scrape words from")]
            public string Url { get; set; }

            [Option('o', "output", Required = false, HelpText = "Specify a file to output the wordlist to",
                Default = null)]
            public string Output { get; set; }

            [Option("depth", Required = false, HelpText = "Specify the depth to crawl", Default = 3)]
            public int Depth { get; set; }

            [Option("max-pages", Required = false, HelpText = "Specify the maximum pages to crawl", Default = null)]
            public int MaxPages { get; set; }

            [Option('t', "threads", Required = false, HelpText = "Specify the number of concurrent threads",
                Default = 10)]
            public int Threads { get; set; }

            [Option("min-length", Required = false, HelpText = "Specify a minimum word length to save", Default = 3)]
            public int Minimum { get; set; }

            [Option("max-length", Required = false, HelpText = "Specify a maximum word length to save", Default = null)]
            public int Maximum { get; set; }

            [Option("delay", Required = false, HelpText = "Specify a delay between requests", Default = 100)]
            public int Delay { get; set; }

            [Option("timeout", Required = false, HelpText = "Specify a timeout for each request", Default = 15)]
            public int Timeout { get; set; }

            [Option("user", Required = false, HelpText = "Specify a username for basic auth")]
            public string User { get; set; }

            [Option("pass", Required = false, HelpText = "Specify a password for basic auth")]
            public string Pass { get; set; }

            [Option('a', "agent", Required = false, HelpText = "Specify a User Agent to use",
                Default =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36")]
            public string Agent { get; set; }
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "WordSmith C# Version 0.1"; //change header
                h.Copyright = ""; //change copyright text
                h.AutoVersion = false;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
            System.Environment.Exit(1);
        }
    }
}