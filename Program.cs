using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Main program class</summary>
    class Program
    {
        public static IWebDriver driver;
        public static WebDriverWait wait;
        public static string url = "http://enginetechcatalog.com/";
        static string output = "output.xml";
        static string price = "price.tsv";
        static string phantomPath = "";
        public static int waitTime = 60;
        public static ScrapeData scrapeData;
        public static Logger logger;
        static bool bDone;
        public static bool bImages = false;
        public static bool bPrice = false;
        public static bool bResetCache = false;
        public static bool bRecover;
        public static Dictionary<string, string> locDescDict = new Dictionary<string, string>();
        public static bool bOverrideImages = false;
        public static bool bCompetitor = false;
        public static int progressCount = 0;
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
                    if (match.Success)
                    {
                        options.Add(match.Groups["argname"].Value, match.Groups["argvalue"].Value);
                    }
                }
            }

            //Collect localization dictionary
            string locFile = "parts.loc";
            if (File.Exists(locFile))
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
            if (File.Exists(confFile))
            {
                using (StreamReader sr = File.OpenText(confFile))
                {
                    string[] pair;
                    while (sr.Peek() >= 0)
                    {
                        pair = sr.ReadLine().Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                        options[pair[0]] = pair[1];
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
                    case "override_images": Boolean.TryParse(option.Value, out bOverrideImages);
                        break;
                    case "competitors": Boolean.TryParse(option.Value, out bCompetitor);
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
                Console.WriteLine("Can't find phantomjs.exe. Try specify the path. Command-line arg is: -phantompath:<path>");
                return;
            }
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitTime));

            if (bPrice && File.Exists(price))
            {
                File.Delete(price);
            }

            //goto m;

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
                            bRecover = true;
                            logger.Write(String.Format("Encountered Internet connection problem on: {0}. Reloading parser.", driver.Url), Logger.LogType.ERROR);
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
            if (bImages == true || bCompetitor == true)
            {
                bDone = false;
            }
            else
            {
                bDone = true;
            }
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
                        bRecover = true;
                        logger.Write("Encountered Internet connection problem. Reloading parser.", Logger.LogType.ERROR);
                    }
                    else throw;
                }
            }

            //m:
            //ScrapeData.EnginePart p = new ScrapeData.EnginePart(Program.scrapeData, "BB2208", "href");
            //p.compInter.Add(new KeyValuePair<string, string>("ITM", "4B"));
            //HashSet<string> competitors = new HashSet<string>();
            //foreach(KeyValuePair<string, ScrapeData.EnginePart> p in scrapeData.parts)
            //{
            //    foreach(KeyValuePair<string, string> c in p.Value.compInter)
            //    {
            //        competitors.Add(c.Key);
            //    }
            //}
            //
            //string[] sortedCompetitors = competitors.OrderBy(i => i).ToArray();

            //Get competitors list from file
            List<List<string>> competitors = new List<List<string>>(36);
            using (StreamReader reader = File.OpenText("competitors"))
            {
                string[] temp;
                while (reader.Peek() >= 0)
                {
                    temp = reader.ReadLine().Split(new char[] { '\t' });
                    competitors.Add(new List<string>(temp));
                }
            }
            //Append competitor table to price-list
            if (bCompetitor && File.Exists(price))
            {
                using (StreamWriter writer = File.CreateText(Path.GetFileNameWithoutExtension(price) + "_comp" + Path.GetExtension(price)))
                using (StreamReader reader = File.OpenText(price))
                {
                    //string[] competitors = { "ACL", "Avon", "Beck Arnley", "Clemex", "Clevite", "Cloyes", "DNJ Rock", "Detroit", "Dura-Bond", "Dynagear", "Elgin", "FM", "Fed. Mogul", "Fel-Pro", "Hastings", "ITM", "King", "Mahle", "Manley", "Melling", "Perfect Circle", "Pioneer", "QualCast", "ROL", "S.A. Gear", "SBI", "Safety", "Silvolite", "Tiger", "Topline", "Vandervell", "Victor Reinz" };
                    //Write table header
                    writer.Write("\t\t\t\t\t\t\t\t");
                    writer.WriteLine(String.Join("\t", competitors.Select(
                        list => list.FirstOrDefault()
                        )));
                    string temp;
                    string partNumber;
                    ScrapeData.EnginePart part = null;
                    while (reader.Peek() >= 0)
                    {
                        temp = reader.ReadLine();
                        writer.Flush();
                        writer.Write(temp); //write initial line
                        writer.Write("\t");
                        partNumber = temp.Split(new char[] { '\t' })[5]; //get Part Number from line
                        part = scrapeData.parts[partNumber];
                        string[] compNumbers = new string[competitors.Count];
                        foreach (KeyValuePair<string, string> comp in part.compInter)
                        {
                            //Place competitors part number into right column in array
                            int index = competitors.FindIndex(list => list.Contains(comp.Key));
                            if (index != -1)
                            {
                                compNumbers[index] = comp.Value;
                            }
                            else
                            {
                                logger.Write(String.Format(
                                    "Found unrecognized or missing competitor! Competitor name: {0}, Enginetech part number: {1}", comp.Key, part.partNumber
                                    ), Logger.LogType.WARNING);
                            }
                        }
                        writer.WriteLine(String.Join("\t", compNumbers));
                    }

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

            logger.Write("End of log.", Logger.LogType.INFO);
            //Shutting down driver
            driver.Quit();
            //System.Console.ReadKey();
        } //end of method Main()

        //static bool TryTask(Action<RecoveryData> task)
        //{

        //}

    } //end of class Program
}
