using System.Collections.Generic;
using System.Xml;

namespace ENGT_Scrape
{
    /// <summary>
    /// Class to store scraped data.</summary>
    class ScrapeData
    {
        /// <summary>
        /// List of stored engine parts.</summary>
        public Dictionary<string, ScrapeData.EnginePart> parts;
        /// <summary>
        /// List of stored engines.</summary>
        public Dictionary<string, ScrapeData.Engine> engines;
        public SortedSet<string> partNames;

        /// <summary>
        /// Constructor to initialize collections.</summary>
        public ScrapeData()
        {
            parts = new Dictionary<string, ScrapeData.EnginePart>();
            engines = new Dictionary<string, ScrapeData.Engine>();
            partNames = new SortedSet<string>();
        }

        /// <summary>
        /// Method to output in XML-file with list of engines.</summary>
        /// <param name="w">Object of an XmlWriter to perform writing.</param>
        public void WriteEngines(XmlWriter w)
        {
            w.WriteStartElement("Engines");
            foreach (KeyValuePair<string, ScrapeData.Engine> eng in this.engines)
            {
                w.WriteStartElement("Engine");
                w.WriteString(eng.Key);
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        /// <summary>
        /// Method to output in XML-file with list of part names.</summary>
        /// <param name="w">Object of an XmlWriter to perform writing.</param>
        public void WritePartNames(XmlWriter w)
        {
            w.WriteStartElement("PartNames");
            foreach (string name in this.partNames)
            {
                w.WriteStartElement("name");
                w.WriteString(name);
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        /// <summary>
        /// Method to output in XML-file with entire data.</summary>
        /// <param name="w">Object of an XmlWriter to perform writing.</param>
        public void WriteXml(XmlWriter w)
        {
            w.WriteStartElement("Catalog");
            foreach (KeyValuePair<string, ScrapeData.EnginePart> partKVP in this.parts)
            {
                ScrapeData.EnginePart part = partKVP.Value;
                w.WriteStartElement("Part");
                w.WriteStartElement("PartNumber");
                w.WriteString(partKVP.Key);
                w.WriteEndElement();
                w.WriteStartElement("Description");
                w.WriteString(part.description);
                w.WriteEndElement();
                w.WriteStartElement("Price");
                w.WriteString(part.price.ToString());
                w.WriteEndElement();
                w.WriteStartElement("OriginalURL");
                w.WriteString(part.href);
                w.WriteEndElement();
                //w.WriteStartElement("PictureURL"); //TODO: output list of all pictures
                //w.WriteString(part.imgUrl);
                //w.WriteEndElement();
                if (part.sizes.Count > 0)
                {
                    w.WriteStartElement("SizeVariations");
                    foreach (string s in part.sizes)
                    {
                        w.WriteStartElement("Size");
                        w.WriteString(s);
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }
                w.WriteStartElement("CarMakers");
                foreach (string car in part.carMakes)
                {
                    w.WriteStartElement("Make");
                    w.WriteString(car);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                w.WriteStartElement("CarTypes");
                foreach (string type in part.carTypes)
                {
                    w.WriteStartElement("Type");
                    w.WriteString(type);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                w.WriteStartElement("CarModels");
                foreach (string model in part.carModels)
                {
                    w.WriteStartElement("Model");
                    w.WriteString(model);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                if (part.Years.Count > 0)
                {
                    w.WriteStartElement("Years");
                    foreach (uint s in part.Years)
                    {
                        w.WriteStartElement("Year");
                        w.WriteString(s.ToString());
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }
                w.WriteStartElement("SetContents");
                foreach (string content in part.setContents)
                {
                    w.WriteStartElement("Content");
                    w.WriteString(content);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                if (part.compInter.Count > 0)
                {
                    w.WriteStartElement("CompetitiveInterchange");
                    foreach (KeyValuePair<string, string> ci in part.compInter)
                    {
                        w.WriteStartElement("CompetitivePart");
                        w.WriteStartElement("Name");
                        w.WriteString(ci.Key);
                        w.WriteEndElement();
                        w.WriteStartElement("Part");
                        w.WriteString(ci.Value);
                        w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }
                w.WriteStartElement("CompitableEngines");
                foreach (KeyValuePair<ScrapeData.Engine, string> engine in part.engines)
                {
                    w.WriteStartElement("Engine");
                    w.WriteStartElement("Name");
                    w.WriteString(engine.Key.fullDesc);
                    w.WriteEndElement();
                    w.WriteStartElement("Notes");
                    w.WriteString(engine.Value);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }
        /// <summary>
        /// Class representing engine parts.</summary>
        public class EnginePart
        {
            /// <summary>
            /// Refers to storage.</summary>
            public ScrapeData parent;
            public string partNumber;
            public string description;
            public float price;
            public HashSet<string> sizes;
            public HashSet<string> imgUrl;
            public string href;
            public HashSet<string> setContents;
            public Dictionary<string, string> compInter;
            public Dictionary<ScrapeData.Engine, string> engines;
            public SortedSet<string> carMakes;
            public SortedSet<string> carTypes;
            public SortedSet<string> carModels;
            private SortedSet<uint> years;
            public SortedSet<uint> Years
            {
                get { return years; }
            }

            /// <summary>
            /// Constructor with minimal set of required parameters.</summary>
            /// <param name="parent">Storage object.</param>
            /// <param name="partN">Part number.</param>
            /// <param name="href">URL of this part on the ET catalog site.</param>
            public EnginePart(ScrapeData parent, string partN, string href)
            {
                this.partNumber = partN;
                this.parent = parent;
                this.href = href;
                this.sizes = new HashSet<string>();
                this.imgUrl = new HashSet<string>();
                this.setContents = new HashSet<string>();
                this.compInter = new Dictionary<string, string>();
                this.engines = new Dictionary<ScrapeData.Engine, string>();
                this.carMakes = new SortedSet<string>();
                this.carTypes = new SortedSet<string>();
                this.carModels = new SortedSet<string>();
                this.years = new SortedSet<uint>();
                this.parent.parts.Add(partN, this);
            }
            /// <summary>
            /// Method to add years associated with this engine part.</summary>
            /// <param name="years">String representing years span in format "yy-yy".</param>
            /// <param name="curYear">Current year to properly switch from yy to yyyy.</param>
            public void AddYears(string years, int curYear)
            {
                uint[] y = new uint[2];
                int count = 0;
                string[] sy = years.Split(new char[] { '-' });
                if (sy.Length != 2) return; //there is no year info
                foreach (string year in sy)
                {
                    uint.TryParse(year, out y[count]);
                    uint cy2 = (uint)(curYear % 100);
                    uint cy4 = (uint)(curYear - cy2);
                    if (y[count] <= cy2)
                    {
                        y[count] += cy4;
                    }
                    else
                    {
                        y[count] += (cy4 - 100);
                    }
                    count++;
                }
                for (uint i = 0; i <= y[1] - y[0]; i++)
                {
                    this.years.Add(y[0] + i);
                }
            }
        }

        /// <summary>
        /// Class representing engine.</summary>
        public class Engine
        {
            /// <summary>
            /// Refers to storage.</summary>
            public ScrapeData parent;
            public string fullDesc;

            /// <summary>
            /// Constructor with minimal set of parameters.</summary>
            /// <param name="parent">Storage object.</param>
            /// <param name="fullDesc">String representing name of an engine.</param>
            public Engine(ScrapeData parent, string fullDesc)
            {
                this.parent = parent;
                this.fullDesc = fullDesc;
                this.parent.engines.Add(fullDesc, this);
            }
        }
    }

}
