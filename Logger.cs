using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENGT_Scrape
{
    /// <summary>
    /// Class to log program process
    /// </summary>
    class Logger
    {
        private String logOutput;

        public enum LogType
        {
            INFO, WARNING, ERROR
        }

        public String LogOutput
        {
            get { return logOutput; }
            set { logOutput = value; }
        }
        public Logger()
        {
            LoggerConstructor("engt.log");
        }
        public Logger(String file)
        {
            LoggerConstructor(file);
        }
        private void LoggerConstructor(String file)
        {
            this.logOutput = file;
            StreamWriter writer = null;
            try
            {
                writer = File.AppendText(this.logOutput);
                writer.WriteLine("");
                writer.WriteLine("Start logging for ENGT at {0}", DateTime.Now.ToShortDateString());
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to write log into file with error: {0}", ex.Message);
            }
            finally
            {
                if( writer != null )
                {
                    writer.Dispose();
                }
            }
        }

        public void Write(String message, LogType type)
        {
            StreamWriter writer = null;
            try 
            {
                writer = File.AppendText(this.logOutput);
                writer.WriteLine("[{0}] {1}: {2}", type.ToString(), DateTime.Now.ToShortTimeString(), message);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to write log into file with error: {0}", ex.Message);
            }
            finally
            {
                if( writer != null )
                {
                    writer.Dispose();
                }
            }
        }

    }
}
