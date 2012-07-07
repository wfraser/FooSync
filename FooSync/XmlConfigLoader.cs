///
/// Codewise/FooSync/XmlConfigLoader.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Codewise.FooSync
{
    public static class XmlConfigLoader
    {
        public static T Load<T>(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
                serializer.UnknownElement += new XmlElementEventHandler(serializer_UnknownElement);
                serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);

                T value = (T)serializer.Deserialize(reader);

                return value;
            }
        }

        public static T Load<T>(string xmlFilename)
        {
            using (FileStream xml = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Load<T>(xml);
            }
        }

        static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            throw new XmlException(
                string.Format("Unknown node while deserializing {2}: \"{0}\", type {1}",
                    e.Name, e.NodeType, e.ObjectBeingDeserialized),
                null, e.LineNumber, e.LinePosition);
        }

        static void serializer_UnknownElement(object sender, XmlElementEventArgs e)
        {
            throw new XmlException(
                string.Format("Unknown element while deserializing {2}: \"{0}\", expecting: {1}",
                    e.Element, e.ExpectedElements, e.ObjectBeingDeserialized),
                null, e.LineNumber, e.LinePosition);
        }

        static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            throw new XmlException(
                string.Format("Unknown attribute while deserializing {2}: \"{0}\", expecting: {1}",
                    e.Attr, e.ExpectedAttributes, e.ObjectBeingDeserialized),
                null, e.LineNumber, e.LinePosition);
        }

        public static void Write<T>(T value, Stream stream)
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
            };

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, value);
            }
        }

        public static void Write<T>(T value, string xmlFilename)
        {
            using (FileStream stream = new FileStream(xmlFilename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Write(value, stream);
            }
        }

        public static bool Validate(Stream xml, string xsdFilename, out string failureMessage)
        {
            bool valid = true;
            failureMessage = string.Empty;

            if (!File.Exists(xsdFilename))
            {
                //
                // Don't fail if we can't validate due to missing XSD.
                //

                return true;
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Schemas.Add(null, xsdFilename);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;

            XmlReader reader = null;

            try
            {
                reader = XmlReader.Create(xml, settings);
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                doc.Validate(null);
            }
            catch (Exception ex)
            {
                failureMessage = FormatExceptionMessage(ex);
                valid = false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return valid;
        }

        public static bool Validate(string xmlFilename, string xsdFilename, out string failureMessage)
        {
            using (FileStream xml = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Validate(xml, xsdFilename, out failureMessage);
            }
        }

        public static string FormatExceptionMessage(Exception ex)
        {
            string failureMessage = string.Empty;

            XmlException xmlEx = ex as XmlException;
            if (xmlEx != null)
            {
                failureMessage = string.Format("XML Exception (at {1}:{2}): {0}", xmlEx.Message, xmlEx.LineNumber, xmlEx.LinePosition);
                goto inner;
            }

            XmlSchemaValidationException schemaEx = ex as XmlSchemaValidationException;
            if (schemaEx != null)
            {
                failureMessage = string.Format("XML Schema Validation Exception (at {1}:{2}): {0}", schemaEx.Message, schemaEx.LineNumber, schemaEx.LinePosition);
                goto inner;
            }

            FileNotFoundException fnfEx = ex as FileNotFoundException;
            if (fnfEx != null)
            {
                failureMessage = fnfEx.Message;
                goto inner;
            }

            failureMessage = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);

            inner:
            failureMessage = Regex.Replace(failureMessage, " in namespace '[^']+'", string.Empty);

            if (ex.InnerException != null)
            {
                failureMessage += "\n" + FormatExceptionMessage(ex.InnerException);
            }

            return failureMessage;
        }
    }
}
