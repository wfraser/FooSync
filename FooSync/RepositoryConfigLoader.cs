using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace FooSync
{
    /// <summary>
    /// Responsible for loading FooSync repository configuration data from XML files.
    /// </summary>
    public class RepositoryConfigLoader
    {
        /// <summary>
        /// Name of the RepositoryConfig XML schema. It should be located in the same directory as this assembly.
        /// </summary>
        const string RepositoryConfigXSD = "RepositoryConfig.xsd";

        /// <summary>
        /// Get a RepositoryConfig object, given the repository config XML file.
        /// </summary>
        /// <param name="configXmlFilename">Path of the XML file describing the repository.</param>
        /// <param name="error">If there is a validation or unserialization error, this string will contain more info.</param>
        /// <returns>An instance of RepositoryConfig on success, null on error.</returns>
        public static RepositoryConfig GetRepositoryConfig(string configXmlFilename, out string error)
        {
            if (!ValidateAgainstSchema(
                    configXmlFilename,
                    System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        RepositoryConfigXSD),
                    out error))
            {
                return null;
            }

            var reader = XmlReader.Create(configXmlFilename);
            var serializer = new XmlSerializer(typeof(RepositoryConfig));

            var repo = (RepositoryConfig)serializer.Deserialize(reader);
            reader.Close();

            if (System.IO.Path.DirectorySeparatorChar != '/')
            {
                foreach (var directory in repo.Directories)
                {
                    directory.Path = directory.Path.Replace('/', System.IO.Path.DirectorySeparatorChar);
                }
            }

            return repo;
        }

        /// <summary>
        /// Validate a given XML file against a given XSD.
        /// </summary>
        /// <param name="xmlFileName">Path to XML file to validate.</param>
        /// <param name="schemaFileName">Path to XSD schema to validate against.</param>
        /// <param name="failureMessage">String with information on the error, if any.</param>
        /// <returns>true if validation succeeds, false otherwise (and fills in failureMessage).</returns>
        public static bool ValidateAgainstSchema(string xmlFileName, string schemaFileName, out string failureMessage)
        {
            bool valid = true;
            failureMessage = string.Empty;

            var settings = new XmlReaderSettings();
            settings.Schemas.Add(null, schemaFileName);
            settings.ValidationType = ValidationType.Schema;

            XmlReader reader = null;

            try
            {
                reader = XmlReader.Create(xmlFileName, settings);
                var document = new XmlDocument();
                document.Load(reader);
                document.Validate(null);
            }
            catch (XmlException ex)
            {
                failureMessage = string.Format("XML Exception (at {1}:{2}): {0}", ex.Message, ex.LineNumber, ex.LinePosition);
                valid = false;
            }
            catch (XmlSchemaValidationException ex)
            {
                failureMessage = string.Format("XML Schema Validation Exception (at {1}:{2}): {0}", ex.Message, ex.LineNumber, ex.LinePosition);
                valid = false;
            }
            catch (Exception ex)
            {
                failureMessage = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
                valid = false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            failureMessage = Regex.Replace(failureMessage, " in namespace '[^']+'", "");

            return valid;
        }
    }
}
