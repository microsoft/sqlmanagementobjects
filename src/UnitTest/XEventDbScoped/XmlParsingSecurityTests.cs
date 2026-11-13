// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.XEventDbScoped;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    [TestClass]
    public class XmlParsingSecurityTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void CreateSessionFromTemplate_imports_valid_template()
        {
            var templatePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(
                    templatePath,
                    "<event_sessions xmlns='http://schemas.microsoft.com/sqlserver/2008/07/extendedeventsconfig'><event_session name='template_session' /></event_sessions>");

                var store = new DatabaseXEStore();
                var session = store.CreateSessionFromTemplate("imported_session", templatePath);

                Assert.That(session, Is.Not.Null, "A valid Extended Events template should be imported");
                Assert.That(session.Name, Is.EqualTo("imported_session"), "The imported session should use the requested name");
            }
            finally
            {
                File.Delete(templatePath);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateSessionFromTemplate_rejects_external_file_entity()
        {
            var externalFilePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(externalFilePath, "resolved external entity");
                AssertTemplateRejectsExternalEntity(new Uri(externalFilePath).AbsoluteUri);
            }
            finally
            {
                File.Delete(externalFilePath);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateSessionFromTemplate_rejects_external_http_entity_without_requesting_it()
        {
            using var server = new LoopbackEntityServer("resolved external entity");

            AssertTemplateRejectsExternalEntity(server.EntityUri);

            Assert.That(server.WasRequested, Is.False, "The external HTTP entity should not be requested");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Event_Predicate_rejects_external_file_entity()
        {
            var externalFilePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(externalFilePath, "<not />");
                AssertPredicateRejectsExternalEntity(new Uri(externalFilePath).AbsoluteUri);
            }
            finally
            {
                File.Delete(externalFilePath);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Event_Predicate_rejects_external_http_entity_without_requesting_it()
        {
            using var server = new LoopbackEntityServer("<not />");

            AssertPredicateRejectsExternalEntity(server.EntityUri);

            Assert.That(server.WasRequested, Is.False, "The external HTTP entity should not be requested");
        }

        private static void AssertTemplateRejectsExternalEntity(string externalEntityUri)
        {
            var templatePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(
                    templatePath,
                    $"<!DOCTYPE event_sessions [<!ENTITY external SYSTEM '{externalEntityUri}'>]>" +
                    "<event_sessions xmlns='http://schemas.microsoft.com/sqlserver/2008/07/extendedeventsconfig'>" +
                    "<event_session name='template_session'><templateDescription>&external;</templateDescription></event_session>" +
                    "</event_sessions>");

                var store = new DatabaseXEStore();

                Assert.That(
                    () => store.CreateSessionFromTemplate("security_test", templatePath),
                    Throws.TypeOf<XmlException>(),
                    $"CreateSessionFromTemplate should reject DTDs before resolving {externalEntityUri}");
            }
            finally
            {
                File.Delete(templatePath);
            }
        }

        private static void AssertPredicateRejectsExternalEntity(string externalEntityUri)
        {
            var store = new DatabaseXEStore();
            var session = new Session(store, "security_test");
            var xevent = new Event
            {
                Parent = session
            };
            var predicateXml =
                $"<!DOCTYPE predicate [<!ENTITY external SYSTEM '{externalEntityUri}'>]>" +
                "<not>&external;</not>";
            xevent.Properties["PredicateXml"].Value = predicateXml;

            Assert.That(
                () => _ = xevent.Predicate,
                Throws.TypeOf<XmlException>(),
                $"Event.Predicate should reject DTDs before resolving {externalEntityUri}");
        }

        private sealed class LoopbackEntityServer : IDisposable
        {
            private readonly TcpListener listener;
            private readonly Task serverTask;

            public LoopbackEntityServer(string responseBody)
            {
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                EntityUri = $"http://127.0.0.1:{port}/external-entity";
                serverTask = ServeResponseAsync(responseBody);
            }

            public string EntityUri { get; }

            public bool WasRequested { get; private set; }

            public void Dispose()
            {
                listener.Stop();

                Assert.That(serverTask.Wait(TimeSpan.FromSeconds(5)), Is.True, "The loopback entity server should stop within the timeout");
            }

            private async Task ServeResponseAsync(string responseBody)
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    WasRequested = true;
                    var responseBytes = Encoding.UTF8.GetBytes(responseBody);
                    var headers = Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 200 OK\r\nContent-Type: application/xml\r\nContent-Length: {responseBytes.Length}\r\nConnection: close\r\n\r\n");
                    using var stream = client.GetStream();
                    stream.Write(headers, 0, headers.Length);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}