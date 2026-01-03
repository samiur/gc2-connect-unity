// ABOUTME: Unit tests for GSProResponse parsing and validation.
// ABOUTME: Tests response codes, player info parsing, and concatenated JSON handling.

using NUnit.Framework;
using OpenRange.Network;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GSProResponseTests
    {
        #region FromJson Tests

        [Test]
        public void FromJson_ValidSuccessResponse_ParsesCorrectly()
        {
            var json = "{\"Code\":200,\"Message\":\"OK\"}";

            var response = GSProResponse.FromJson(json);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
            Assert.AreEqual("OK", response.Message);
        }

        [Test]
        public void FromJson_ValidErrorResponse_ParsesCorrectly()
        {
            var json = "{\"Code\":501,\"Message\":\"Player not ready\"}";

            var response = GSProResponse.FromJson(json);

            Assert.IsNotNull(response);
            Assert.AreEqual(501, response.Code);
            Assert.AreEqual("Player not ready", response.Message);
        }

        [Test]
        public void FromJson_WithPlayerInfo_ParsesPlayer()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"DR\",\"DistanceToTarget\":275.5}}";

            var response = GSProResponse.FromJson(json);

            Assert.IsNotNull(response);
            Assert.AreEqual(201, response.Code);
            Assert.IsNotNull(response.Player);
            Assert.AreEqual("RH", response.Player.Handed);
            Assert.AreEqual("DR", response.Player.Club);
            Assert.AreEqual(275.5f, response.Player.DistanceToTarget, 0.01f);
        }

        [Test]
        public void FromJson_NullString_ReturnsNull()
        {
            var response = GSProResponse.FromJson(null);

            Assert.IsNull(response);
        }

        [Test]
        public void FromJson_EmptyString_ReturnsNull()
        {
            var response = GSProResponse.FromJson("");

            Assert.IsNull(response);
        }

        [Test]
        public void FromJson_InvalidJson_ReturnsNull()
        {
            var response = GSProResponse.FromJson("not valid json");

            Assert.IsNull(response);
        }

        [Test]
        public void FromJson_MalformedJson_ReturnsNull()
        {
            var response = GSProResponse.FromJson("{\"Code\":200");

            Assert.IsNull(response);
        }

        #endregion

        #region IsSuccess Tests

        [Test]
        public void IsSuccess_Code200_ReturnsTrue()
        {
            var response = GSProResponse.FromJson("{\"Code\":200,\"Message\":\"OK\"}");

            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        public void IsSuccess_Code201_ReturnsTrue()
        {
            var response = GSProResponse.FromJson("{\"Code\":201,\"Message\":\"OK\"}");

            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        public void IsSuccess_Code299_ReturnsTrue()
        {
            var response = GSProResponse.FromJson("{\"Code\":299,\"Message\":\"OK\"}");

            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        public void IsSuccess_Code199_ReturnsFalse()
        {
            var response = GSProResponse.FromJson("{\"Code\":199,\"Message\":\"Info\"}");

            Assert.IsFalse(response.IsSuccess);
        }

        [Test]
        public void IsSuccess_Code300_ReturnsFalse()
        {
            var response = GSProResponse.FromJson("{\"Code\":300,\"Message\":\"Redirect\"}");

            Assert.IsFalse(response.IsSuccess);
        }

        [Test]
        public void IsSuccess_Code501_ReturnsFalse()
        {
            var response = GSProResponse.FromJson("{\"Code\":501,\"Message\":\"Error\"}");

            Assert.IsFalse(response.IsSuccess);
        }

        #endregion

        #region HasPlayerInfo Tests

        [Test]
        public void HasPlayerInfo_Code201WithPlayer_ReturnsTrue()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"DR\",\"DistanceToTarget\":150}}";
            var response = GSProResponse.FromJson(json);

            Assert.IsTrue(response.HasPlayerInfo);
        }

        [Test]
        public void HasPlayerInfo_Code201WithoutPlayer_ReturnsFalse()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\"}";
            var response = GSProResponse.FromJson(json);

            Assert.IsFalse(response.HasPlayerInfo);
        }

        [Test]
        public void HasPlayerInfo_Code200WithPlayer_ReturnsFalse()
        {
            // HasPlayerInfo only returns true for code 201
            var json = "{\"Code\":200,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"DR\",\"DistanceToTarget\":150}}";
            var response = GSProResponse.FromJson(json);

            Assert.IsFalse(response.HasPlayerInfo);
        }

        #endregion

        #region ParseFirstObject String Tests

        [Test]
        public void ParseFirstObject_SingleJson_ParsesCorrectly()
        {
            var data = "{\"Code\":200,\"Message\":\"OK\"}";

            var response = GSProResponse.ParseFirstObject(data);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
        }

        [Test]
        public void ParseFirstObject_ConcatenatedJson_ParsesOnlyFirst()
        {
            var data = "{\"Code\":200,\"Message\":\"First\"}{\"Code\":201,\"Message\":\"Second\"}";

            var response = GSProResponse.ParseFirstObject(data);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
            Assert.AreEqual("First", response.Message);
        }

        [Test]
        public void ParseFirstObject_ThreeJsonObjects_ParsesOnlyFirst()
        {
            var data = "{\"Code\":100,\"Message\":\"One\"}{\"Code\":200,\"Message\":\"Two\"}{\"Code\":300,\"Message\":\"Three\"}";

            var response = GSProResponse.ParseFirstObject(data);

            Assert.IsNotNull(response);
            Assert.AreEqual(100, response.Code);
            Assert.AreEqual("One", response.Message);
        }

        [Test]
        public void ParseFirstObject_WithLeadingWhitespace_ParsesCorrectly()
        {
            var data = "   \n\t{\"Code\":200,\"Message\":\"OK\"}";

            var response = GSProResponse.ParseFirstObject(data);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
        }

        [Test]
        public void ParseFirstObject_WithNestedBraces_ParsesCorrectly()
        {
            var data = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"DR\",\"DistanceToTarget\":150}}{\"Code\":200}";

            var response = GSProResponse.ParseFirstObject(data);

            Assert.IsNotNull(response);
            Assert.AreEqual(201, response.Code);
            Assert.IsNotNull(response.Player);
            Assert.AreEqual("RH", response.Player.Handed);
        }

        [Test]
        public void ParseFirstObject_NullString_ReturnsNull()
        {
            var response = GSProResponse.ParseFirstObject((string)null);

            Assert.IsNull(response);
        }

        [Test]
        public void ParseFirstObject_EmptyString_ReturnsNull()
        {
            var response = GSProResponse.ParseFirstObject("");

            Assert.IsNull(response);
        }

        [Test]
        public void ParseFirstObject_NoOpeningBrace_ReturnsNull()
        {
            var response = GSProResponse.ParseFirstObject("no json here");

            Assert.IsNull(response);
        }

        [Test]
        public void ParseFirstObject_UnclosedBrace_ReturnsNull()
        {
            var response = GSProResponse.ParseFirstObject("{\"Code\":200");

            Assert.IsNull(response);
        }

        #endregion

        #region ParseFirstObject Byte Buffer Tests

        [Test]
        public void ParseFirstObject_ByteBuffer_SingleJson_ParsesCorrectly()
        {
            var json = "{\"Code\":200,\"Message\":\"OK\"}";
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            var response = GSProResponse.ParseFirstObject(buffer, buffer.Length);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
        }

        [Test]
        public void ParseFirstObject_ByteBuffer_ConcatenatedJson_ParsesOnlyFirst()
        {
            var json = "{\"Code\":200,\"Message\":\"First\"}{\"Code\":201,\"Message\":\"Second\"}";
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            var response = GSProResponse.ParseFirstObject(buffer, buffer.Length);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
            Assert.AreEqual("First", response.Message);
        }

        [Test]
        public void ParseFirstObject_ByteBuffer_PartialLength_ParsesCorrectly()
        {
            var json = "{\"Code\":200,\"Message\":\"OK\"}{\"Code\":201}";
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            // Only read first object
            var firstObjectLength = "{\"Code\":200,\"Message\":\"OK\"}".Length;

            var response = GSProResponse.ParseFirstObject(buffer, firstObjectLength);

            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.Code);
        }

        [Test]
        public void ParseFirstObject_ByteBuffer_NullBuffer_ReturnsNull()
        {
            var response = GSProResponse.ParseFirstObject(null, 10);

            Assert.IsNull(response);
        }

        [Test]
        public void ParseFirstObject_ByteBuffer_ZeroLength_ReturnsNull()
        {
            var buffer = new byte[100];

            var response = GSProResponse.ParseFirstObject(buffer, 0);

            Assert.IsNull(response);
        }

        [Test]
        public void ParseFirstObject_ByteBuffer_NegativeLength_ReturnsNull()
        {
            var buffer = new byte[100];

            var response = GSProResponse.ParseFirstObject(buffer, -1);

            Assert.IsNull(response);
        }

        #endregion
    }

    [TestFixture]
    public class GSProPlayerInfoTests
    {
        #region Handedness Tests

        [Test]
        public void IsRightHanded_HandedRH_ReturnsTrue()
        {
            var player = new GSProPlayerInfo { Handed = "RH" };

            Assert.IsTrue(player.IsRightHanded);
        }

        [Test]
        public void IsRightHanded_HandedLH_ReturnsFalse()
        {
            var player = new GSProPlayerInfo { Handed = "LH" };

            Assert.IsFalse(player.IsRightHanded);
        }

        [Test]
        public void IsLeftHanded_HandedLH_ReturnsTrue()
        {
            var player = new GSProPlayerInfo { Handed = "LH" };

            Assert.IsTrue(player.IsLeftHanded);
        }

        [Test]
        public void IsLeftHanded_HandedRH_ReturnsFalse()
        {
            var player = new GSProPlayerInfo { Handed = "RH" };

            Assert.IsFalse(player.IsLeftHanded);
        }

        [Test]
        public void IsRightHanded_HandedNull_ReturnsFalse()
        {
            var player = new GSProPlayerInfo { Handed = null };

            Assert.IsFalse(player.IsRightHanded);
        }

        [Test]
        public void IsLeftHanded_HandedNull_ReturnsFalse()
        {
            var player = new GSProPlayerInfo { Handed = null };

            Assert.IsFalse(player.IsLeftHanded);
        }

        #endregion

        #region Club Tests

        [Test]
        public void Club_Driver_ParsesCorrectly()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"DR\",\"DistanceToTarget\":275}}";
            var response = GSProResponse.FromJson(json);

            Assert.AreEqual("DR", response.Player.Club);
        }

        [Test]
        public void Club_PitchingWedge_ParsesCorrectly()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"PW\",\"DistanceToTarget\":135}}";
            var response = GSProResponse.FromJson(json);

            Assert.AreEqual("PW", response.Player.Club);
        }

        [Test]
        public void Club_SevenIron_ParsesCorrectly()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"LH\",\"Club\":\"7I\",\"DistanceToTarget\":160}}";
            var response = GSProResponse.FromJson(json);

            Assert.AreEqual("7I", response.Player.Club);
        }

        [Test]
        public void Club_Putter_ParsesCorrectly()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"PT\",\"DistanceToTarget\":15}}";
            var response = GSProResponse.FromJson(json);

            Assert.AreEqual("PT", response.Player.Club);
        }

        #endregion

        #region DistanceToTarget Tests

        [Test]
        public void DistanceToTarget_ParsesFloat()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"DR\",\"DistanceToTarget\":275.5}}";
            var response = GSProResponse.FromJson(json);

            Assert.AreEqual(275.5f, response.Player.DistanceToTarget, 0.01f);
        }

        [Test]
        public void DistanceToTarget_Zero_ParsesCorrectly()
        {
            var json = "{\"Code\":201,\"Message\":\"OK\",\"Player\":{\"Handed\":\"RH\",\"Club\":\"PT\",\"DistanceToTarget\":0}}";
            var response = GSProResponse.FromJson(json);

            Assert.AreEqual(0f, response.Player.DistanceToTarget, 0.01f);
        }

        #endregion
    }
}
