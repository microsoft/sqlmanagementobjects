// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Serializes instances of System.Version to Sfc XML 
    /// </summary>
    public class VersionSerializationAdapter : IXmlSerializationAdapter
    {
        public void WriteXml(XmlWriter writer, object objectToSerialize)
        {
            if(objectToSerialize == null)
            {
                throw new ArgumentNullException("objectToSerialize");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            Version version = objectToSerialize as Version;
            if (version == null)
            {
                throw new ArgumentException(ExceptionTemplates.InvalidSerializerAdapterFound("VersionSerializationAdapter", 
                                    typeof(Version).FullName, objectToSerialize.GetType().FullName));
            }

            writer.WriteElementString(typeof(Version).FullName, version.ToString());
        }

        public void ReadXml(XmlReader reader, out object deserializedObject)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            //skips the document type comment
            reader.MoveToContent(); 

            string versionString = reader.ReadElementContentAsString();

            try
            {
                Version version = new Version(versionString);
                deserializedObject = version;
            }
            catch (ArgumentNullException)
            {
                throw new FailedOperationException(ExceptionTemplates.InvalidConversionError("VersionSerializationAdapter",
                    "null", typeof(Version).FullName));
            }
            catch (ArgumentException)
            {
                throw new FailedOperationException(ExceptionTemplates.InvalidConversionError("VersionSerializationAdapter",
                   versionString, typeof(Version).FullName));
            }
            
        }
    }

    /// <summary>
    /// Serializes objects that represent sql_variant instances to SFC XML
    /// </summary>
    public class SqlVariantSerializationAdapter : IXmlSerializationAdapter
    {
        public void ReadXml(XmlReader reader, out object deserializedObject)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer, object objectToSerialize)
        {
            var type = objectToSerialize?.GetType();
            if (type == typeof(object[]))
            {
                writer.WriteStartElement("SqlVariantArray");
                foreach (var obj in (object[]) objectToSerialize)
                {
                    WriteXml(writer, obj);
                }

                writer.WriteEndElement();
                return;
            }

            var typeName = type?.FullName ?? nameof(DBNull);
            writer.WriteStartElement("SqlVariant");
            writer.WriteAttributeString(nameof(Type),typeName);
            // System.Data.SqlTypes types can self-serialize
            var serializable = objectToSerialize as IXmlSerializable;
            if (serializable != null)
            {
                serializable.WriteXml(writer);
            }
            else
            {
                writer.WriteString(StringFromSqlVariant(objectToSerialize));
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// This is a serialization version of SqlSmoObject.FormatSqlVariant
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static string StringFromSqlVariant(object sqlVariant)
        {
            // the TypeName attribute distinguishes empty from null
            if (sqlVariant == null)
            {
                return string.Empty;
            }

            var type = sqlVariant.GetType();
            if (type == typeof(Int32))
            {
                return ((Int32)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(byte))
            {
                return ((byte) sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(decimal))
            {
                return ((decimal)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(Int16))
            {
                return ((Int16)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(Int64))
            {
                return ((Int64)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(double))
            {
                return ((double)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(float))
            {
                return ((float)sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            if (type == typeof(DateTime))
            {
                return SqlSmoObject.SqlDateString((DateTime)sqlVariant, "yyyy-MM-ddTHH:mm:ss.fff");
            }
            if (type == typeof(DateTimeOffset))
            {
                return  SqlSmoObject.SqlDateString((DateTimeOffset)sqlVariant);
            }
            
            if (type == typeof(byte[]))
            {
                return Convert.ToBase64String((byte[])sqlVariant);
            }
            if (type == typeof(System.Boolean))
            {
                ((Boolean) sqlVariant).ToString(SmoApplication.DefaultCulture);
            }
            
            // This included all cases not caught above, such as unsigned integers etc
            return sqlVariant.ToString();
        }
    }
}
