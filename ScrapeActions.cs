using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;

namespace ENGT_Scrape
{
    /// <summary>
    /// Class to perform various scrape tasks.</summary>
    public static class ScrapeActions
    {
        /// <summary>
        /// Scrape through Engine Search form.
        /// </summary>
        /// <param name="recData">Structure to hold point of progress to recover from.</param>
        public static void ScrapeEngineSearch(ref RecoveryData recData)
        {
            //TODO: convert to argument
            bool bPrice = Program.bPrice;
            Dictionary<string, bool> collectRule = new Dictionary<string, bool> { { "desc", false }, { "notes", false }, { "price", false } };
            if (bPrice)
            {
                collectRule = new Dictionary<string, bool> { { "desc", true }, { "notes", true }, { "price", true } };
            }
            //ulong count = 1; //counter for price
            //For notes analyze
            //SortedSet<string> notesSet = new SortedSet<string>();
            //HashSet<string> parseSet = new HashSet<string> {"ford - car", "ford - truck, van, suv", "jeep - truck, van, suv" };
            HashSet<string> parseSet = new HashSet<string>();
            //bool bAppend = false;

            //Looking for and following link to engine search
            IWebElement link = Program.driver.FindElement(By.Id("ctl00_lnkEngine"));
            link.Click();
            Program.wait.Until((d) =>
                {
                    return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete");
                });

            //Collecting a list of elements referring to cars in table
            Dictionary<string, string> cars = new Dictionary<string, string>();
            cars = Program.driver.FindElements(By.CssSelector("a[id^='ctl00_MainContent_GVVehicle_ct']")).ToDictionary(e => e.GetAttribute("id"), k => k.Text.ToLower());
            if (cars.Count > 0)
            {
                Program.logger.Write(String.Format("Found {0} cars in table.", cars.Count), Logger.LogType.INFO);
            }
            else
            {
                Program.logger.Write("No cars has been found in table.", Logger.LogType.ERROR);
            }

            //Clicking through entire list of cars
            string car_title = null;
            Dictionary<string, string> engines = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> car in cars)
            {
                //process only selected cars in parseSet
                if (parseSet.Count > 0 && !parseSet.Contains(car.Value)) continue;
                //recover from Internet connection problems
                if (Program.bRecover)
                {
                    if (recData.stage >= 1 && car.Value != recData.data[1])
                    {
                        continue;
                    }
                    else
                    {
                        if (recData.stage == 1) { Program.bRecover = false; }
                    }
                }
                else
                {
                    recData.stage = 1;
                    recData.data[1] = car.Value;
                }
                Program.logger.Write(String.Format("Collecting engines for car: {0}", car.Value), Logger.LogType.INFO);
                IWebElement car_el = Program.driver.FindElement(By.Id(car.Key));
                car_el.Click();
                Program.wait.Until((d) =>
                {
                    //Try taking car's title from engines tab to ensure page has been reloaded
                    try
                    {
                        car_title = d.FindElement(By.Id("ctl00_MainContent_lblVehicleConfigTitle")).Text.ToLower();
                        return car.Value == car_title;
                    }
                    catch (Exception ex)
                    {
                        if (ex is NoSuchElementException || ex is StaleElementReferenceException)
                        {
                            return false;
                        }
                        else throw;
                    }
                });
                IReadOnlyCollection<IWebElement> engine_els = Program.driver.FindElements(By.CssSelector("a[id^='ctl00_MainContent_GVVehicleConfig_ct']"));
                //Collecting a list of elements referring to engine of current car
                engines = engine_els.ToDictionary(e => e.GetAttribute("id"), k => k.Text
                    //{
                    //    String href = k.GetAttribute("href");
                    //    Match match = Regex.Match(href, @".*\?engPartno=(?<engPartno>\w+)");
                    //    if(match.Success)
                    //    {
                    //        return match.Groups["engPartno"].Value;
                    //    }
                    //    Program.logger.Write(
                    //        String.Format("Regex failed to get engine part number from href: {0}.{1}Using engine string instead.", href, Environment.NewLine),
                    //        Logger.LogType.ERROR);
                    //    return k.Text;
                    //}
                    );
                if (engines.Count > 0)
                {
                    Program.logger.Write(String.Format("Found {0} engines", engines.Count), Logger.LogType.INFO);
                }
                else
                {
                    Program.logger.Write("No engines has been found.", Logger.LogType.ERROR);
                }

                //Clicking through entire list of engines of current car to get additional info
                //string eng_title = null;
                string[] c = car.Value.Split(new string[] { " - ", "- " }, StringSplitOptions.None);
                List<string> output = new List<string>();
                foreach (KeyValuePair<string, string> engine in engines)
                {
                    //recover from Internet connection problems
                    if (Program.bRecover)
                    {
                        if (recData.stage >= 2 && engine.Value != recData.data[2])
                        {
                            continue;
                        }
                        else
                        {
                            if (recData.stage == 2) { Program.bRecover = false; }
                        }
                        if (recData.stage == 3)
                        {
                            Program.bRecover = false;
                            continue;
                        }
                    }
                    else
                    {
                        recData.stage = 2;
                        recData.data[2] = engine.Value;
                    }
                    Program.logger.Write(String.Format("Collecting parts for engine: {0}", engine.Value), Logger.LogType.INFO);
                    IWebElement eng_el = Program.driver.FindElement(By.Id(engine.Key));
                    //Clicking on engine
                    eng_el.Click();
                    Program.wait.Until((d) =>
                        {
                            return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete");
                        });

                    //Collecting engine parts
                    //parse through table
                    string prev_part = null;
                    string prev_name = null;
                    int count = 0;
                    IList<IWebElement> trs = Program.driver.FindElements(By.CssSelector("div[id='ctl00_MainContent_pnlPartdetails'] table tbody tr"));
                    if (trs.Count > 0)
                    {
                        Program.logger.Write(String.Format("Found {0} entries with part in table.", trs.Count), Logger.LogType.INFO);
                    }
                    else
                    {
                        Program.logger.Write("No entries in table.", Logger.LogType.ERROR);
                    }
                    foreach (IWebElement tr in trs)
                    {
                        IWebElement w = null;
                        ScrapeData.EnginePart part;
                        string number, desc, note, href, price;
                        number = desc = note = price = "";
                        //Scrape part number and URL
                        try
                        {
                            w = tr.FindElement(By.CssSelector("a[id$='_lnkPartno']"));
                            prev_part = w.Text;
                            number = prev_part;
                            href = w.GetAttribute("href");
                            //Add part to storage
                            if (!Program.scrapeData.parts.ContainsKey(number))
                            {
                                part = new ScrapeData.EnginePart(Program.scrapeData, number, href);
                            }
                        }
                        catch (NoSuchElementException)
                        {
                            number = prev_part;
                        }
                        //Scrape description
                        if (collectRule["desc"])
                        {
                            try
                            {
                                w = tr.FindElement(By.CssSelector("span[id$='_lblDescription']"));
                                prev_name = w.Text;
                                desc = prev_name;
                            }
                            catch (NoSuchElementException)
                            {
                                desc = prev_name;
                            }
                        }
                        //Scrape notes
                        if (collectRule["notes"])
                        {
                            note = tr.FindElement(By.CssSelector("span[id$='_lblNotes']")).Text;
                            //notesSet.Add(note);
                        }
                        //Scrape price
                        if (collectRule["price"])
                        {
                            price = tr.FindElement(By.CssSelector("span[id$='_lblRetail']")).Text;
                        }

                        if (bPrice)
                        {
                            //localize strings
                            string locType, locDesc, locPrice;
                            if (c[1] == "car")
                            {
                                locType = "Легковой";
                            }
                            else
                            {
                                locType = "Внедорожник/Фургон/Спортивный";
                            }
                            string upCar = c[0].First().ToString().ToUpper() + c[0].Substring(1);
                            if (price == "CALL")
                            {
                                locPrice = "Звоните!";
                            }
                            else
                            {
                                locPrice = price;
                            }

                            if (!Program.locDescDict.TryGetValue(desc, out locDesc))
                            {
                                locDesc = desc;
                            }
                            //locDesc = Program.locDescDict[desc];
                            count++;
                            string priceEngine = engine.Value.Substring(engine.Value.IndexOf('/') + 1);
                            output.Add(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", upCar, locType, priceEngine, count, locDesc, number, note, locPrice));
                        }
                    }
                    //Following back from engine detail page
                    IWebElement back = Program.driver.FindElement(By.Id("ctl00_MainContent_btnBack"));
                    recData.stage = 3;
                    back.Click();
                    Program.wait.Until((d) =>
                    {
                        return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete");
                    });
                }
                //Append price into file
                if (bPrice)
                {
                    string fileName = "price.tsv";
                    if (output.Count > 0)
                    {
                        Program.logger.Write(String.Format("Adding {0} lines to file {1}", output.Count, fileName), Logger.LogType.INFO);
                        StreamWriter writer = null;
                        try
                        {
                            writer = File.AppendText(fileName);
                            foreach (string s in output)
                            {
                                writer.WriteLine(s);
                            }
                        }
                        catch (Exception e)
                        {
                            Program.logger.Write(String.Format("Failed to append new lines into this file with error: {0}", e.Message), Logger.LogType.ERROR);
                        }
                        finally
                        {
                            if (writer != null)
                            {
                                writer.Dispose();
                            }
                        }
                    }

                }
            }
            //output all distinct and sorted notes
            //using (StreamWriter writer = File.CreateText("notes.txt"))
            //{
            //    foreach ( string s in notesSet )
            //    {
            //        writer.WriteLine(s);
            //    }
            //}
        } //end of method ScrapeForPrice()

        /// <summary>
        /// Scrape additional part info from Part Details Page.</summary>
        /// <param name="sd">Storage used to get engine parts URLs.</param>
        /// <param name="recHref">Structure to hold point of progress.</param>
        public static void ScrapePartDetails(ref RecoveryData recData)
        {
            //TODO: get from argument
            bool bSaveImage = Program.bImages;
            bool bSaveInterchage = Program.bCompetitor;
            foreach (KeyValuePair<string, ScrapeData.EnginePart> part in Program.scrapeData.parts)
            {
                //Recover after connection problems
                if (Program.bRecover)
                {
                    if (part.Value.href != recData.href)
                    {
                        continue;
                    }
                    else
                    {
                        Program.bRecover = false;
                    }
                }
                else
                {
                    recData.href = part.Value.href;
                }
                //fix URL with '+' in Part Number
                if (part.Key.Contains('+'))
                {
                    string newPartNumber = part.Key.Replace("+", "%2B");
                    string newHref = part.Value.href.Replace(part.Key, newPartNumber);
                    part.Value.href = newHref;
                }

                Program.driver.Navigate().GoToUrl(part.Value.href);
                //Wait page to be loaded
                bool bError = false;
                Program.wait.Until((d) =>
                {
                    return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete");
                });
                //Check if there is server error page
                try
                {
                    string er = Program.driver.FindElement(By.TagName("h1")).Text;
                    if (er == "Server Error in '/' Application.")
                    {
                        bError = true;
                    }
                }
                catch (NoSuchElementException) { } //there is no server error
                if (bError)
                {
                    Program.logger.Write(String.Format("Skipping engine part {0} by server error", part.Key), Logger.LogType.WARNING);
                    continue; //Server error - skip this engine part
                }
                //Try take part number to ensure page has been reloaded
                Program.wait.Until((d) =>
                {
                    try
                    {
                        string partNumber = d.FindElement(By.Id("ctl00_MainContent_lblPartDetailsText")).Text;
                        return part.Key == partNumber;
                    }
                    catch (Exception ex)
                    {
                        if (ex is NoSuchElementException || ex is StaleElementReferenceException)
                        {
                            return false;
                        }
                        else throw;
                    }
                });
                //log
                Program.logger.Write(String.Format("Scraping engine part: {0}", part.Key), Logger.LogType.INFO);
                //write progress bar
                Program.progressCount++;
                int count = Program.progressCount;
                Console.WriteLine("Progress: {0}/{1} ({2:P2})", count, Program.scrapeData.parts.Count, (float)count / (float)Program.scrapeData.parts.Count);

                //Get part description
                //part.Value.description = Program.driver.FindElement(By.Id("ctl00_MainContent_lblDescriptionText")).Text;
                //Get size variations
                //string sizes = Program.driver.FindElement(By.Id("ctl00_MainContent_lblSizeText")).Text;
                //if (sizes != "")
                //{
                //    part.Value.sizes.UnionWith(sizes.Split(new string[] { ", " }, StringSplitOptions.None));
                //}
                //Get image
                if (bSaveImage)
                {
                    try
                    {
                        IWebElement pictures = Program.driver.FindElement(By.Id("ctl00_MainContent_tdImage1"));
                        foreach (IWebElement wel in pictures.FindElements(By.CssSelector("input")))
                        {
                            part.Value.imgUrl.Add(wel.GetAttribute("src"));
                        }
                    }
                    catch (NoSuchElementException) { }
                    if (part.Value.imgUrl.Count > 0)
                    {
                        WebClient client = new WebClient();
                        foreach (string src in part.Value.imgUrl)
                        {
                            SaveImage(client, src, part.Key);
                        }
                        client.Dispose();
                    }
                }

                //Get interchange list
                if (bSaveInterchage)
                {
                    IWebElement table;
                    try
                    {
                        table = Program.driver.FindElement(By.Id("ctl00_MainContent_trInterchangeRepeater"));
                        foreach (IWebElement wel in table.FindElements(By.CssSelector(
                            "table[class='dimensionRepeaterData'] tbody tr")))
                        {
                            string name, number;
                            name = wel.FindElement(By.CssSelector("span[id$='_lblName']")).Text;
                            number = wel.FindElement(By.CssSelector("span[id$='_lblPartno']")).Text;
                            if (part.Value.compInter.ContainsKey(name))
                            {
                                part.Value.compInter[name] += ", " + number;
                            }
                            else
                            {
                                part.Value.compInter[name] = number;
                            }
                        }
                    }
                    catch (NoSuchElementException) { }
                }
                ////Get set contents
                //try
                //{
                //    table = Program.driver.FindElement(By.Id("ctl00_MainContent_trGasketsetcontents"));
                //    foreach (IWebElement wel in table.FindElements(By.CssSelector("span[id*='setcontents_ct']")))
                //    {
                //        part.Value.setContents.Add(wel.Text);
                //    }
                //}
                //catch (NoSuchElementException) { }
            }
        }

        private static void SaveImage(WebClient client, string src, string part)
        {
            //Files must be less than 2 MB.
            //Allowed file types: png gif jpg jpeg.
            //Images must be between 400x550 and 1200x1200 pixels.
            //string ext = Path.GetExtension(src).ToLower();
            string name = Path.GetFileNameWithoutExtension(src);
            string fileName = String.Format("Images{0}{1}_{2}.png", Path.DirectorySeparatorChar.ToString(), part.Replace('*', '@'), name);
            if (!Program.bOverrideImages)
            {
                if (File.Exists(fileName))
                {
                    Program.logger.Write(String.Format("File already exists. Leaving the old one."), Logger.LogType.INFO);
                    return;
                }
            }
            Program.logger.Write(String.Format("Downloading image: {0}.", src), Logger.LogType.INFO);
            //string[] validExts = new string[] { ".png", ".gif", ".jpg", ".jpeg" };
            Bitmap image = null;
            MemoryStream stream = null;
            Bitmap cropImage = null;
            Image newImage = null;
            Graphics g = null;
            FileStream fs = null;
            try
            {
                bool bDownload = false;
                ushort count = 0;
                while (count < 10)
                {
                    try
                    {
                        stream = new MemoryStream(client.DownloadData(src));
                        bDownload = true;
                        break;
                    }
                    catch (WebException)
                    {
                        Program.logger.Write(String.Format("Failed to download image. Retrying in {0} seconds.", Program.waitTime), Logger.LogType.ERROR);
                        count++;
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(Program.waitTime));
                    }
                }
                if (!bDownload)
                {
                    Program.logger.Write("Skipping this image.", Logger.LogType.ERROR);
                    return;
                }
                image = new Bitmap(stream);
                //Rid of ugly borders
                Color col = image.GetPixel(0, 0);
                if (col.R < 210 || col.G < 210 || col.B < 210)
                {
                    Program.logger.Write("Borders detected. Trying to find border's width.", Logger.LogType.INFO);
                    Point point = new Point(0, 0);
                    while ((point.X <= image.Width && point.Y <= image.Height) && (col.R < 210 || col.G < 210 || col.B < 210))
                    {
                        point.X++;
                        point.Y++;
                        col = image.GetPixel(point.X, point.Y);
                    }
                    if (point.X > image.Width || point.Y > image.Height)
                    {
                        Program.logger.Write("Failed to find width.", Logger.LogType.ERROR);
                        point = new Point(0, 0);
                    }
                    int newW, newH;
                    newW = image.Width - point.X * 2;
                    newH = image.Height - point.Y * 2;
                    Rectangle destRect = new Rectangle(0, 0, newW, newH);
                    Rectangle srcRect = new Rectangle(point.X, point.Y, image.Width - point.X * 2, image.Height - point.Y * 2);
                    cropImage = new Bitmap(newW, newH);
                    Graphics.FromImage(cropImage).DrawImage(image, destRect, srcRect, GraphicsUnit.Pixel);
                    image = new Bitmap(cropImage);
                }
                int w = image.Width;
                int h = image.Height;
                int bw = image.Width;
                int bh = image.Height;
                double ratio;
                int max = Math.Max(image.Height, image.Width);
                int min = Math.Min((image.Height - 150), image.Width);
                double side = 400.0f;
                Rectangle r;
                if (image.Height - 150 <= image.Width)
                {
                    side = 550.0f;
                    min += 150;
                }
                // Upscale if needed
                if (min < side)
                {
                    ratio = side / min;
                    w = Convert.ToInt32(image.Width * ratio);
                    h = Convert.ToInt32(image.Height * ratio);
                    bw = w;
                    bh = h;
                    max = Math.Max(w, h);
                    Program.logger.Write(String.Format("Image upscaled from: {0}x{1} to: {2}x{3}", image.Width, image.Height, bw, bh), Logger.LogType.INFO);
                }
                // Downscale if needed including previously upscaled to handle bad aspect ratios
                if (max > 1200)
                {
                    ratio = 1200.0f / max;
                    w = Convert.ToInt32(w * ratio);
                    h = Convert.ToInt32(h * ratio);
                    //this will add white borders if aspect ratio is bad
                    bw = (w < 400) ? 400 : w;
                    bh = (h < 550) ? 550 : h;
                    Program.logger.Write(String.Format("Image downscaled from: {0}x{1} to: {2}x{3}", image.Width, image.Height, bw, bh), Logger.LogType.INFO);
                }
                bool bResize = true;
                double maxSize = 2097152.0f;
                while (bResize)
                {
                    r = new Rectangle((bw - w) / 2, (bh - h) / 2, w, h);
                    newImage = new Bitmap(bw, bh);
                    g = Graphics.FromImage(newImage);
                    g.FillRectangle(Brushes.White, 0, 0, bw, bh);
                    g.DrawImage(image, r);
                    stream = new MemoryStream();
                    newImage.Save(stream, ImageFormat.Png);
                    //Check file size after compressing with png                    
                    if (stream.Length >= maxSize)
                    {
                        //Try reduce image dimensions
                        ratio = (maxSize - 1) / stream.Length;
                        w = (int)Math.Truncate(w * ratio);
                        h = (int)Math.Truncate(h * ratio);
                        bw = (w < 400) ? 400 : w;
                        bh = (h < 550) ? 550 : h;
                        Program.logger.Write(String.Format("Reached file size limit. Image downscaled to: {0}x{1}", bw, bh), Logger.LogType.INFO);
                    }
                    else
                    {
                        //File size is ok: write stream to file
                        bResize = false;
                        fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                        stream.WriteTo(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.logger.Write(String.Format("Error while saving image: {0}", ex.Message), Logger.LogType.ERROR);
            }
            finally
            {
                if (image != null)
                {
                    image.Dispose();
                }
                if (stream != null)
                {
                    stream.Dispose();
                }
                if (cropImage != null)
                {
                    cropImage.Dispose();
                }
                if (newImage != null)
                {
                    newImage.Dispose();
                }
                if (g != null)
                {
                    g.Dispose();
                }
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        //private static void WritePrice(StreamWriter writer, string engine, List<string> parts)
        //{
        //    //Write engine
        //    writer.WriteLine(engine);
        //    writer.WriteLine("№\tНаименование\tАртикул\tПримечание\tРозничная цена");
        //    foreach(string s in parts)
        //    {
        //        writer.WriteLine(s);
        //    }
        //    writer.WriteLine();
        //}
    } //end of class ScrapeActions
}

////Scrape Year Search form to assigne every engine part a list of car models
//private static void ScrapeCarModels(ref string[] recData, ref int stage)
//{
//    //Following to Year Search page
//    driver.FindElement(By.Id("ctl00_lnkAAIAFlow")).Click();
//    wait.Until((d) =>
//    {
//        return d.Url == url + "AAIAFlow/AAIAFlow.aspx";
//    });
//    System.Console.WriteLine("Page title is: " + driver.Title);

//    //Filling search form
//    string[] dropIds = new string[4] { "ctl00_MainContent_drpYear", "ctl00_MainContent_drpMake", "ctl00_MainContent_drpModel", "ctl00_MainContent_drpEngine" };
//    string model = "";
//    RollOptions(dropIds, 0, ref model, ref recData, ref stage);

//}

////Scrape additional part info from Part Details Page
//private static void ScrapePartDetails(ScrapeData sd, ref string recHref)
//{
//    foreach(KeyValuePair<string, ScrapeData.EnginePart> part in sd.parts)
//    {
//        //Recover after connection problems
//        if (bRecover)
//        {
//            if (part.Value.href != recHref)
//            {
//                continue;
//            }
//            else
//            {
//                bRecover = false;
//            }
//        }
//        else
//        {
//            recHref = part.Value.href;
//        }
//        //fix url with '+' in Part Number
//        if (part.Key.Contains('+'))
//        {
//            string newPartNumber = part.Key.Replace("+", "%2B");
//            string newHref = part.Value.href.Replace(part.Key, newPartNumber);
//            part.Value.href = newHref;
//        }

//        driver.Navigate().GoToUrl(part.Value.href);
//        //Wait page to be loaded
//        wait.Until((d) =>
//        {
//            try
//            {
//                string partNumber = d.FindElement(By.Id("ctl00_MainContent_lblPartDetailsText")).Text;
//                return part.Key == partNumber;
//            }
//            catch (Exception ex)
//            {
//                if (ex is NoSuchElementException || ex is StaleElementReferenceException)
//                {
//                    return false;
//                }
//                else throw;
//            }
//        });
//        //log
//        Console.WriteLine("Current engine part: {0}", part.Key);
//        //Get part description
//        part.Value.description = driver.FindElement(By.Id("ctl00_MainContent_lblDescriptionText")).Text;
//        //Get size varations
//        string sizes = driver.FindElement(By.Id("ctl00_MainContent_lblSizeText")).Text;
//        if (sizes != "")
//        {
//            part.Value.sizes.UnionWith(sizes.Split(new string[] { ", " }, StringSplitOptions.None));
//        }
//        //Get image
//        try
//        {
//            part.Value.imgUrl = driver.FindElement(By.Id("ctl00_MainContent_imgBtnDimensions")).GetAttribute("src");
//        }
//        catch(NoSuchElementException)
//        {
//            part.Value.imgUrl = "";
//        }
//        //Get intechange list
//        IWebElement table;
//        try
//        {
//            table = driver.FindElement(By.Id("ctl00_MainContent_trInterchangeRepeater"));
//            foreach (IWebElement wel in table.FindElements(By.CssSelector(
//                "table[class='dimensionRepeaterData'] tbody tr")))
//            {
//                part.Value.compInter.Add(new KeyValuePair<string, string>(
//                    wel.FindElement(By.CssSelector("span[id$='_lblName']")).Text,
//                    wel.FindElement(By.CssSelector("span[id$='_lblPartno']")).Text));
//            }
//        }
//        catch(NoSuchElementException){}
//        //Get set contents
//        try
//        {
//            table = driver.FindElement(By.Id("ctl00_MainContent_trGasketsetcontents"));
//            foreach (IWebElement wel in table.FindElements(By.CssSelector("span[id*='setcontents_ct']")))
//            {
//                part.Value.setContents.Add(wel.Text);
//            }
//        }
//        catch(NoSuchElementException){}
//    }
//}

////Scrape entire site
//private static void ScrapeEntireSite(ref string[] recData, ref int stage)
//{
//    //Looking for and following link to engine search
//    IWebElement link = driver.FindElement(By.Id("ctl00_lnkEngine"));
//    link.Click();
//    wait.Until((d) =>
//    {
//        return d.Url == url + "EngineProgramlication/EngineTechFlow.aspx";
//    });
//    System.Console.WriteLine("Page title is: " + driver.Title);

//    //Collecting a list of elements referering to cars in table
//    Dictionary<string, string> cars = new Dictionary<string, string>();
//    cars = driver.FindElements(By.CssSelector("a[id^='ctl00_MainContent_GVVehicle_ct']")).ToDictionary(e => e.GetAttribute("id"), k => k.Text.ToLower());

//    //Clicking through entire list of cars
//    string car_title = null;
//    Dictionary<string, string> engines = new Dictionary<string, string>();                      
//    foreach (KeyValuePair<string, string> car in cars)
//    {
//        //recover from internet connetion problems
//        if (bRecover)
//        {
//            if (stage >= 1 && car.Value != recData[1])
//            {
//                continue;
//            }
//            else
//            {
//                if (stage == 1) { bRecover = false; }
//            }
//        }
//        else
//        {
//            stage = 1;
//            recData[1] = car.Value;
//        }
//        IWebElement car_el = driver.FindElement(By.Id(car.Key));                
//        car_el.Click();
//        wait.Until((d) =>
//        {
//            //Try taking car's title from engines tab to ensure page has been reloaded
//            try
//            {
//                car_title = d.FindElement(By.Id("ctl00_MainContent_lblVehicleConfigTitle")).Text.ToLower();
//                return car.Value == car_title;
//            }
//            catch (Exception ex)
//            {
//                if (ex is NoSuchElementException || ex is StaleElementReferenceException)
//                {
//                    return false;
//                }
//                else throw;
//            }
//        });
//        //Collecting a list of elements refering to engine of current car
//        engines = driver.FindElements(By.CssSelector("a[id^='ctl00_MainContent_GVVehicleConfig_ct']"))
//            .ToDictionary(e => e.GetAttribute("id"), k => k.Text);

//        //Clicking through entire list of engines of current car to get additional info
//        string eng_title = null;
//        foreach (KeyValuePair<string, string> engine in engines)
//        {
//            //recover from internet connection problems
//            if (bRecover)
//            {
//                if (stage >= 2 && engine.Value != recData[2])
//                {
//                    continue;
//                }
//                else
//                {
//                    if (stage == 2) { bRecover = false; }
//                }
//                if (stage == 3)
//                {
//                    bRecover = false;
//                    continue;
//                }
//            }
//            else
//            {
//                stage = 2;
//                recData[2] = engine.Value;
//            }
//            IWebElement eng_el = driver.FindElement(By.Id(engine.Key));
//            eng_el.Click();
//            wait.Until((d) =>
//            {
//                //Try taking engine's title from new table to ensure page has been reloaded
//                try
//                {
//                    eng_title = d.FindElement(By.Id("ctl00_MainContent_lblConfig")).Text;
//                    return engine.Value == eng_title;
//                }
//                catch (Exception ex)
//                {
//                    if( ex is NoSuchElementException || ex is StaleElementReferenceException)
//                    {
//                        return false;
//                    }
//                    else throw;
//                }
//            });
//            //debug
//            System.Console.WriteLine("Current car is: " + car.Value);

//            //Start collecting data
//            //Check if current engine is already in stored data
//            ScrapeData.Engine engData;
//            if (scrapeData.engines.ContainsKey(engine.Value))
//            {
//                engData = scrapeData.engines[engine.Value];
//                Console.WriteLine("Engine: {0} already exists", engData.fullDesc);
//            }
//            else
//            {
//                engData = new ScrapeData.Engine(scrapeData, engine.Value);
//                Console.WriteLine("Engine: {0} added to collection", engData.fullDesc);
//            }
//            //Collecting engine parts
//            //parse through table
//            string prev_part = null;
//            IList<IWebElement> trs = driver.FindElements(By.CssSelector("div[id='ctl00_MainContent_pnlPartdetails'] table tbody tr"));                    
//            foreach (IWebElement tr in trs)
//            {
//                IWebElement w = null;
//                //collect part names
//                //try
//                //{
//                //    w = tr.FindElement(By.CssSelector("span[id$='_lblDescription']"));
//                //    scrapeData.partNames.Add(w.Text);
//                //    Console.WriteLine(w.Text);
//                //}
//                //catch (NoSuchElementException){}
//                //continue;
//                bool bNote = false;
//                ScrapeData.EnginePart part;
//                try
//                {
//                    w = tr.FindElement(By.CssSelector("a[id$='_lnkPartno']"));
//                    prev_part = w.Text;
//                }
//                catch (NoSuchElementException) //for row with missing partnumber we take previuos
//                {
//                    bNote = true;
//                }
//                //Check if current engine part is already in stored data
//                if(scrapeData.parts.ContainsKey(prev_part))
//                {
//                    part = scrapeData.parts[prev_part];
//                    Console.WriteLine("Part: {0} already exists", part.partNumber);
//                }
//                else //this is new engine part
//                {
//                    part = new ScrapeData.EnginePart(scrapeData, w.Text, w.GetAttribute("href"));
//                    string price = tr.FindElement(By.CssSelector("span[id$='_lblRetail']")).Text;
//                    if (price == "CALL")
//                    {
//                        part.price = 0.0f;
//                    }
//                    else
//                    {
//                        NumberStyles style = NumberStyles.Currency;
//                        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
//                        part.price = (float)Double.Parse(price, style, culture);
//                    }
//                    Console.WriteLine("Part: {0} added to collection", part.partNumber);
//                }

//                //for all parts
//                if (bNote || part.engines.ContainsKey(engData)) //if there additional note for existing engine
//                {
//                    part.engines[engData] += (Environment.NewLine + tr.FindElement(By.CssSelector("span[id$='_lblNotes']")).Text);
//                }
//                else //else new engine:note pair
//                {
//                    part.engines.Add(engData, tr.FindElement(By.CssSelector("span[id$='_lblNotes']")).Text);
//                }
//                part.AddYears(engine.Value.Substring(engine.Value.Length - 5), dateTime.Year);
//                string[] c = car.Value.Split(new string[] { " - ", "- " }, StringSplitOptions.None);
//                part.carMakes.Add(c[0]);
//                part.carTypes.Add(c[1]);
//            }

//            //Following back from engine detail page
//            IWebElement back = driver.FindElement(By.Id("ctl00_MainContent_btnBack"));
//            stage = 3;
//            back.Click();
//            wait.Until((d) =>
//            {
//                //Try taking car's title from engines tab to ensure page has been reloaded
//                try
//                {
//                    car_title = d.FindElement(By.Id("ctl00_MainContent_lblVehicleConfigTitle")).Text.ToLower();
//                    return car.Value == car_title;
//                }
//                catch (Exception ex)
//                {
//                    if (ex is NoSuchElementException || ex is StaleElementReferenceException)
//                    {
//                        return false;
//                    }
//                    else throw;
//                }
//            });
//        }
//    }
//}

////Function that rolls through all options in drop-down menu or takes one from search query
//private static void RollOptions(string[] dropIds, int searchPos, ref string model, ref string[] recData, ref int stage)
//{
//    List<string> options = new List<string>();
//    foreach (IWebElement s in driver.FindElements(By.CssSelector("select[id='" + dropIds[searchPos] + "'] option")))
//    {
//        string v = s.GetAttribute("value");
//        if (!v.Contains("Select"))
//        {
//            options.Add(s.GetAttribute("value"));
//        }
//    }
//    //}
//    //Clicking through all options in List
//    IWebElement s_el = null;
//    foreach (string option in options)
//    {
//        //if (searchPos == 0 && option != "2008") continue;
//        if (bRecover)
//        {
//            if (option != recData[searchPos])
//            {
//                continue;
//            }
//            else
//            {
//                if (stage == searchPos) { bRecover = false; }
//            }
//        }
//        else
//        {
//            stage = searchPos;
//            recData[searchPos] = option;
//        }
//        //Saving model title
//        if (searchPos == 2)
//        {
//            model = option;
//        }
//        ClickOption(s_el, dropIds[searchPos], option);
//        //If there is anothere drop-down menu go into it
//        if (searchPos < 3)
//        {
//            Console.WriteLine("{0}: {1}", searchPos, option); //debug
//            RollOptions(dropIds, searchPos + 1, ref model, ref recData, ref stage);
//        }
//        else
//        {
//            //check for table is present
//            bool bEmpty;
//            try
//            {
//                driver.FindElement(By.Id("ctl00_MainContent_pnlPartdetails1"));
//                bEmpty = false; 
//            }
//            catch (NoSuchElementException)
//            {
//                bEmpty = true;
//            }
//            if (!bEmpty) //if present - ready to parse table with engine parts
//            {
//                Console.WriteLine("Added to parts:");
//                foreach(IWebElement wel in driver.FindElements(By.CssSelector("a[id^='ctl00_MainContent_rptAAIA_ct']")))
//                {
//                    if(scrapeData.parts.ContainsKey(wel.Text))
//                    {
//                        scrapeData.parts[wel.Text].carModels.Add(model);
//                        Console.WriteLine(wel.Text);
//                    }
//                    else
//                    {
//                        Console.WriteLine("Found new engine part: {0}", wel.Text);
//                    }                         
//                }
//            }
//        }
//    }
//}

////Routine for clicking options from dropdown menu
//private static void ClickOption(IWebElement el, string id, string item)
//{
//    el = driver.FindElement(By.CssSelector("select[id='" + id + "'] option[value='" + item + "']"));
//    el.Click();
//    wait.Until((d) =>
//    {
//        //Check the page has been reloaded
//        try
//        {
//            return d.FindElement(By.Id(id)).GetAttribute("title") == item;
//        }
//        catch (Exception ex)
//        {
//            if (ex is NoSuchElementException || ex is StaleElementReferenceException)
//            {
//                return false;
//            }
//            else throw;
//        }
//    });
//}

////Routine to get text by element's id or return null if no such element;
//private static string TryGetText(string id)
//{
//    try
//    {
//        return driver.FindElement(By.Id(id)).Text;
//    }
//    catch (NoSuchElementException)
//    {
//        return null;
//    }
//}

////routine to take screenshots
//private static void TakeScreenshot(string str=null)
//{
//    try
//    {
//        Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
//        if (str == null) str = "";
//        ss.SaveAsFile(str, System.Drawing.Imaging.ImageFormat.Jpeg);
//    }
//    catch (Exception e)
//    {
//        Console.WriteLine(e.Message);
//        throw;
//    }
//}