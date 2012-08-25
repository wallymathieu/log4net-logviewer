﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LogViewer;

namespace Core
{
    public class LogEntryParser
    {
        public IEnumerable<LogEntry> Parse(Stream s)
        {
            //todo? http://www.hanselman.com/blog/XmlAndTheNametable.aspx
            var doc = new XmlDocument();
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("log4j", "http://jakarta.apache.org/log4j/");
            nsmgr.AddNamespace("log4net", "http://logging.apache.org/log4net/");
            // Create the XmlParserContext.
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            // Create the reader. 
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;

            var xmlreader = XmlReader.Create(s, settings, context);
            int iIndex = 1;

            while (xmlreader.Read())
            {
                LogEntry logentry = ParseElement(doc.ReadNode(xmlreader));
                logentry.Item = iIndex++;
                yield return logentry;
            }
        }
        private readonly DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        private LogEntry ParseElement(XmlNode xElement)
        {
            LogEntry logentry = new LogEntry();

            string timestamp = xElement.Attributes["timestamp"].Value;
            double dSeconds;
            if (Double.TryParse(timestamp, out dSeconds))
            {
                logentry.TimeStamp = dt.AddMilliseconds(dSeconds).ToLocalTime();
            }
            else
            {
                logentry.TimeStamp = DateTime.Parse(timestamp).ToLocalTime();
            }
            logentry.Thread = xElement.Attributes["thread"].Value;

            if (null != xElement.Attributes["domain"])
                logentry.App = xElement.Attributes["domain"].Value;

            #region get level

            ////////////////////////////////////////////////////////////////////////////////
            logentry.Level = xElement.Attributes["level"].Value;
            logentry.Image = ParseImageType(logentry.Level);
            ////////////////////////////////////////////////////////////////////////////////

            #endregion

            #region read xml

            var elements = xElement.ChildNodes;
            foreach (XmlNode element in elements)
            {
                switch (element.Name.Split(':')[1])
                {
                    case "message":
                        {
                            logentry.Message = element.InnerText;
                            break;
                        }
                    case "properties":
                        {
                            var properties = element.ChildNodes;

                            foreach (XmlNode property in properties)
                                switch (property.Attributes["name"].Value)
                                {
                                    case ("log4jmachinename"):
                                        logentry.MachineName = property.Attributes["value"].Value;
                                        break;
                                    case ("log4net:HostName"):
                                        logentry.HostName = property.Attributes["value"].Value;
                                        break;
                                    case ("log4net:UserName"):
                                        logentry.UserName = property.Attributes["value"].Value;
                                        break;
                                    case ("log4japp"):
                                        logentry.App = property.Attributes["value"].Value;
                                        break;
                                    default:
                                        throw new NotImplementedException(property.Attributes["name"].Value);
                                }

                            break;
                        }

                    case "throwable":
                    case "exception":
                        {
                            logentry.Throwable = element.InnerText;
                            break;
                        }
                    case ("locationInfo"):
                        {
                            logentry.Class = element.Attributes["class"].Value;
                            logentry.Method = element.Attributes["method"].Value;
                            logentry.File = element.Attributes["file"].Value;
                            logentry.Line = element.Attributes["line"].Value;
                            break;
                        }
                    default:
                        throw new NotImplementedException(element.Name);
                }

            }

            #endregion

            return logentry;
        }



        public static LogEntry.ImageType ParseImageType(string level)
        {
            switch (level)
            {
                case "ERROR":
                    return LogEntry.ImageType.Error;
                case "INFO":
                    return LogEntry.ImageType.Info;
                case "DEBUG":
                    return LogEntry.ImageType.Debug;
                case "WARN":
                    return LogEntry.ImageType.Warn;
                case "FATAL":
                    return LogEntry.ImageType.Fatal;
                default:
                    return LogEntry.ImageType.Custom;
            }
        }
    }
}
