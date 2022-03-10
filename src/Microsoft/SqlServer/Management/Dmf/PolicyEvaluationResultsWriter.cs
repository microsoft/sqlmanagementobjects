// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Diagnostics.STrace;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Writes evaluation histories into a single rooted xml document with a root element named PolicyEvaluationResults.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyEvaluationResultsWriter : IDisposable
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyEvaluationResultsWriter");
        private XmlWriter xmlWriter;
        // Track whether Dispose has been called.
        private bool disposed = false;

        /// <summary>
        /// A PolicyEvaluationResultsWriter writes evaluation histories into a single rooted xml document
        /// with the root element PolicyEvaluationResults.  Add histories to the xml by calling the 
        /// WriteEvaluationHistory method.  Close the root element and document by calling Dispose().
        /// </summary>
        /// <param name="xmlWriter"></param>
        public PolicyEvaluationResultsWriter(XmlWriter xmlWriter)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PolicyEvaluationResultsWriter", System.Diagnostics.TraceEventType.Information))
            {
                if (xmlWriter == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("xmlWriter"));
                }
                this.xmlWriter = xmlWriter;
                WriteAggergateEvaluationHistoryStart();
            }
        }

        /// <summary>
        /// Write an EvaluationHistory to the writer stream 
        /// </summary>
        /// <param name="history"></param>
        public void WriteEvaluationHistory(EvaluationHistory history)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("WriteEvaluationHistory", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(history);
                if (history == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("history"));
                }

                XmlWriterSettings settings = GetXmlWriterSettings();
                settings.OmitXmlDeclaration = true;

                StringBuilder stringBuilder = new StringBuilder();
                XmlWriter historyWriter = XmlTextWriter.Create(stringBuilder, settings);
                history.Serialize(historyWriter);
                historyWriter.Flush();
                historyWriter.Close();

                this.xmlWriter.WriteRaw(stringBuilder.ToString());
                this.xmlWriter.Flush();
            }
        }

        /// <summary>
        /// The settings an XmlWriter can use to create an aggregate evaluation history
        /// </summary>
        public static XmlWriterSettings GetXmlWriterSettings()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetXmlWriterSettings", System.Diagnostics.TraceEventType.Information))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = Encoding.UTF8;
                settings.NewLineOnAttributes = true;
                settings.Indent = true;
                methodTraceContext.TraceParameterOut("returnVal", settings);
                return settings;
            }
        }


        /// <summary>
        /// Start an aggregation of EvaluationHistories.  Add specific evaluationHistories to
        /// the stream by calling WriteEvaluationHistory(EvaluationHistory, XmlWriter)
        /// </summary>
        private void WriteAggergateEvaluationHistoryStart()
        {
            this.xmlWriter.WriteStartDocument();
            this.xmlWriter.WriteStartElement("PolicyEvaluationResults");
            this.xmlWriter.Flush();
        }

        /// <summary>
        /// End the aggregation of EvaluationHistories.  This method does not close the writer.
        /// </summary>
        private void WriteAggergateEvaluationHistoryEnd()
        {
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteEndDocument();
            this.xmlWriter.Flush();
        }

        /// <summary>
        /// End the document and release the reference to the xmlWriter
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }



        /// <summary>
        /// End the document and release the reference to the xmlWriter.
        /// 
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, close the document 
                if (disposing)
                {
                    this.WriteAggergateEvaluationHistoryEnd();
                }

                this.xmlWriter = null;

                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}
