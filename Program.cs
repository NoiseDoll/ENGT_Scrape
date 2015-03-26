using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace ENGT_Scrape
{
    /// <summary>
    /// Structure to hold point of progress in tasks.</summary>
    public struct RecoveryData
    {
        public int stage;
        public string[] data;
        public string href;

        public RecoveryData(int stage, string[] data, string href)
        {
            this.stage = stage;
            this.data = data;
            this.href = href;
        }
    }

    //Main programm
    class Program
    {
        public static IWebDriver driver;
        public static WebDriverWait wait;
        public static string url = "http://enginetechcatalog.com/";
        static string output = "output.xml";
        static string phantomPath = "";
        static int waitTime = 10;
        public static ScrapeData scrapeData;
        public static Logger logger;
        static bool bDone;
        public static bool bImages = false;
        public static bool bPrice = false;
        public static bool bResetCache = false;
        public static bool bRecover;
        public static Dictionary<string, string> locDescDict = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            //initialize objects;
            scrapeData = new ScrapeData();
            logger = new Logger();
            bRecover = false;
            //string recHref = "";
            Dictionary<string, string> options = new Dictionary<string, string>();
            
            //Checking args
            if (args.Length != 0)
            {
                foreach (string arg in args)
                {
                    Match match = Regex.Match(arg, @"\-(?<argname>\w+):(?<argvalue>.+)");
                    if(match.Success) 
                    {
                        options.Add(match.Groups["argname"].Value, match.Groups["argvalue"].Value);
                    }
                }
            }

            //Collect localization dictionary
            string locFile = "parts.loc";
            if(File.Exists(locFile))
            {
                using (StreamReader sr = File.OpenText(locFile))
                {
                    string[] pair;
                    while (sr.Peek() >= 0)
                    {
                        pair = sr.ReadLine().Split(new char[] { '\t' });
                        locDescDict.Add(pair[0], pair[1]);
                    }
                }
            }
            //Read config
            string confFile = "engt.cfg";
            if(File.Exists(confFile))
            {
                using (StreamReader sr = File.OpenText(confFile))
                {
                    string[] pair;
                    while (sr.Peek() >= 0)
                    {
                        pair = sr.ReadLine().Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                        options.Add(pair[0], pair[1]);
                    }
                }
            }
            //applying recognized args and config params
            foreach (KeyValuePair<string, string> option in options)
            {
                switch (option.Key)
                {
                    case "url": url = option.Value;
                        break;
                    case "phantompath": phantomPath = option.Value;
                        break;
                    case "wait": Int32.TryParse(option.Value, out waitTime);
                        break;
                    case "output": output = option.Value;
                        break;
                    case "config": //TODO: read options from file;
                        break;
                    case "images": Boolean.TryParse(option.Value, out bImages);
                        break;
                    case "price": Boolean.TryParse(option.Value, out bPrice);
                        break;
                    case "resetcache": Boolean.TryParse(option.Value, out bResetCache);
                        break;
                    //case "search":
                    //    bSearch = true;
                    //    search = option.Value.Split(new char[] { '.' });
                    //    break;
                    default: Console.WriteLine("Found unrecognized option " + option.Key + ". Possibly a mistake.");
                        break;
                }
            }

            //Starting web-browser
            try
            {
                if (phantomPath != "")
                {
                    driver = new PhantomJSDriver(phantomPath);
                }
                else driver = new PhantomJSDriver();
            }
            catch (DriverServiceNotFoundException)
            {
                Console.WriteLine("Can't find phantomjs.exe. Try specify the path. Commandline arg is: -phantompath:<path>");
                return;
            }
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitTime));

            if (File.Exists("price.tsv"))
            {
                File.Delete("price.tsv");
            }

            //Start tasks in recovery loops
            RecoveryData rd = new RecoveryData(0, new string[4], String.Empty);
            if (!File.Exists("parts.cache")) bResetCache = true;
            if (bResetCache)
            {
                bDone = false;
                while (!bDone)
                {
                    try
                    {
                        //Opening web-site
                        driver.Navigate().GoToUrl(url);
                        wait.Until((d) =>
                        {
                            return d.Title == "Engine Parts Catalog";
                        });
                        //Start task
                        ScrapeActions.ScrapeEngineSearch(ref rd);
                        bDone = true;
                    }
                    catch (Exception ex)
                    {
                        if (ex is WebDriverException || ex is WebDriverTimeoutException || ex is StaleElementReferenceException)
                        {
                            if (!bRecover)
                            {
                                bRecover = true;
                                logger.Write(String.Format("Encountered internet connection problem on: {2}. Reloading parcer.", driver.Url), Logger.LogType.ERROR);
                            }
                        }
                        else throw;
                    }
                }
                //Write engine parts cache
                using (StreamWriter writer = File.CreateText("parts.cache"))
                {
                    foreach (KeyValuePair<string, ScrapeData.EnginePart> part in Program.scrapeData.parts)
                    {
                        writer.WriteLine("{0}\t{1}", part.Key, part.Value.href);
                    }
                }
            }
            else
            {
                //Read from cache
                using (StreamReader reader = File.OpenText("parts.cache"))
                {
                    string[] pair;
                    while (reader.Peek() >= 0)
                    {
                        pair = reader.ReadLine().Split(new char[] { '\t' });
                        new ScrapeData.EnginePart(scrapeData, pair[0], pair[1]);
                    }
                }
            }
            //debug
            //new ScrapeData.EnginePart(Program.scrapeData, "P5071(6)", 
            //    "http://enginetechcatalog.com/Interchange/InterchangeResult.aspx?Partno=P5071(6)&Product=Piston+Set");
            if (bImages)
            {
                //Create directory for images
                if (!Directory.Exists("Images"))
                {
                    Directory.CreateDirectory("Images");
                }
            }
            //Start tasks in recovery loops
            rd = new RecoveryData(0, new string[4], String.Empty);
            bDone = !bImages;
            while (!bDone)
            {
                try
                {
                    //Opening web-site
                    driver.Navigate().GoToUrl(url);
                    wait.Until((d) =>
                    {
                        return d.Title == "Engine Parts Catalog";
                    });
                    //Start task
                    ScrapeActions.ScrapePartDetails(ref rd);
                    bDone = true;
                }
                catch (Exception ex)
                {
                    if (ex is WebDriverException || ex is WebDriverTimeoutException || ex is StaleElementReferenceException)
                    {
                        if (!bRecover)
                        {
                            bRecover = true;
                            logger.Write(String.Format("Encountered internet connection problem on: {2}. Reloading parcer.", driver.Url), Logger.LogType.ERROR);
                        }
                    }
                    else throw;
                }
            }

            //Console.WriteLine("Scrape done! Writing to XML.");
            ////Writing an XML
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            //settings.NewLineChars = Environment.NewLine;
            //settings.NewLineHandling = NewLineHandling.Replace;
            //using (XmlWriter writer = XmlWriter.Create(output, settings))
            //{
            //    scrapeData.WriteXml(writer);
            //}
            //using (XmlWriter writer = XmlWriter.Create("out_engines.xml", settings))
            //{
            //    scrapeData.WriteEngines(writer);
            //}
            //using (XmlWriter writer = XmlWriter.Create("out_parts.xml", settings))
            //{
            //    scrapeData.WritePartNames(writer);
            //}

            //Shuting down driver
            driver.Quit();
            //System.Console.ReadKey();
        } //end of method Main()

        //static bool TryTask(Action<RecoveryData> task)
        //{

        //}

    } //end of class Program
}
