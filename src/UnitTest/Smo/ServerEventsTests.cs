using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class ServerEventsTests : UnitTestBase
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void ServerEventSet_supports_all_server_events()
        {
            var eventSet = new ServerEventSet(ServerEvent.AddRoleMember);
            Assert.That(eventSet.GetBitAt((int)ServerEvent.AddRoleMember.Value), Is.True, "Event passed to ServerEventsetConstructor should set its bit");
            eventSet += ServerEvent.AddSensitivityClassification;
            Assert.That(eventSet.ToString(), Is.EqualTo($"{nameof(ServerEventSet)}: {nameof(ServerEvent.AddRoleMember)}, {nameof(ServerEvent.AddSensitivityClassification)}"), "ServerEventSet.ToString()");
            eventSet -= ServerEvent.AddRoleMember;
            Assert.That(eventSet.GetBitAt((int)ServerEvent.AddRoleMember.Value), Is.False, "eventSet.GetBitAt after -=");
            eventSet.Remove(ServerEvent.AddSensitivityClassification);
            Assert.That(eventSet.GetBitAt((int)ServerEvent.AddSensitivityClassification.Value), Is.False, "eventSet.GetBitAt after Remove");
            var eventSetType = typeof(ServerEvent);
            int eventCount = 0;
            Assert.Multiple(() =>
           {
               foreach (var staticEvent in
                   eventSetType.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                   .Where(p => p.PropertyType.Equals(typeof(ServerEvent))))
               {
                   var serverEvent = (ServerEvent)staticEvent.GetGetMethod().Invoke(null, null);
                   eventCount++;
                   Assert.That(eventSet.GetBitAt((int)serverEvent.Value), Is.False, $"{serverEvent.ToString()} not set yet");
                   eventSet.Add(serverEvent);
                   Assert.That(eventSet.GetBitAt((int)serverEvent.Value), Is.True, $"{serverEvent.ToString()} is set");
                   var localProp = typeof(ServerEventSet).GetProperty(staticEvent.Name);
                   Assert.That(localProp.GetGetMethod().Invoke(eventSet, null), Is.True, $"ServerEventSet.{localProp.Name} has been set");
                   localProp.GetSetMethod().Invoke(eventSet, new object[] { false });
                   Assert.That(localProp.GetGetMethod().Invoke(eventSet, null), Is.False, $"ServerEventSet.{localProp.Name} has been unset");
               }
               Assert.That(eventSet.NumberOfElements, Is.EqualTo(eventCount), "ServerEventSet.NumberOfElements");
           });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerEventSet_operator_tests()
        {
            Assert.That(ServerEvent.DropAssembly == new ServerEvent(ServerEventValues.DropAssembly), "ServerEvent ==");
            Assert.That(ServerEvent.DropAssembly.Equals(new ServerEvent(ServerEventValues.DropAssembly)), "ServerEvent.Equals");
            Assert.That(ServerEvent.DropAssembly != ServerEvent.DropView, "ServerEvent !=");
            var bitwiseOrSet = ServerEvent.BitwiseOr(ServerEvent.AddRoleMember, ServerEvent.AddSignature);
            Assert.That(bitwiseOrSet.AddRoleMember && bitwiseOrSet.AddSignature, Is.True, "ServerEvent.BitwiseOr creates correct ServerEventSet");
            var copySet = (ServerEventSet)bitwiseOrSet.Copy();
            Assert.That(copySet.AddRoleMember && copySet.AddSignature && !copySet.AddSignatureSchemaObject, Is.True, "ServerEventSet.Copy correct ServerEventSet");
        }
    }
}
