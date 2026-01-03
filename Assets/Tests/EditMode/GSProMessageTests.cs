// ABOUTME: Unit tests for GSPro message classes.
// ABOUTME: Tests JSON serialization, message formatting, and data conversion.

using NUnit.Framework;
using Newtonsoft.Json.Linq;
using OpenRange.Network;
using OpenRange.GC2;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GSProMessageTests
    {
        #region GSProMessage Tests

        [Test]
        public void GSProMessage_DefaultValues_AreCorrect()
        {
            var message = new GSProMessage();

            Assert.AreEqual("GC2 Connect Unity", message.DeviceID);
            Assert.AreEqual("Yards", message.Units);
            Assert.AreEqual("1", message.APIversion);
            Assert.AreEqual(0, message.ShotNumber);
            Assert.IsNotNull(message.BallData);
            Assert.IsNotNull(message.ShotDataOptions);
            Assert.IsNull(message.ClubData);
        }

        [Test]
        public void GSProMessage_ToJson_ContainsRequiredFields()
        {
            var message = new GSProMessage { ShotNumber = 1 };
            var json = message.ToJson();

            Assert.IsTrue(json.Contains("\"DeviceID\""));
            Assert.IsTrue(json.Contains("\"Units\""));
            Assert.IsTrue(json.Contains("\"ShotNumber\""));
            Assert.IsTrue(json.Contains("\"APIversion\""));
            Assert.IsTrue(json.Contains("\"BallData\""));
            Assert.IsTrue(json.Contains("\"ShotDataOptions\""));
        }

        [Test]
        public void GSProMessage_ToJson_OmitsNullClubData()
        {
            var message = new GSProMessage { ShotNumber = 1 };
            var json = message.ToJson();

            Assert.IsFalse(json.Contains("\"ClubData\""));
        }

        [Test]
        public void GSProMessage_ToJson_IncludesClubDataWhenPresent()
        {
            var message = new GSProMessage
            {
                ShotNumber = 1,
                ClubData = new GSProClubData { Speed = 105f }
            };
            var json = message.ToJson();

            Assert.IsTrue(json.Contains("\"ClubData\""));
            Assert.IsTrue(json.Contains("105"));
        }

        [Test]
        public void GSProMessage_CreateHeartbeat_SetsCorrectOptions()
        {
            var message = GSProMessage.CreateHeartbeat(true, true);

            Assert.AreEqual(0, message.ShotNumber);
            Assert.IsFalse(message.ShotDataOptions.ContainsBallData);
            Assert.IsFalse(message.ShotDataOptions.ContainsClubData);
            Assert.IsTrue(message.ShotDataOptions.LaunchMonitorIsReady);
            Assert.IsTrue(message.ShotDataOptions.LaunchMonitorBallDetected);
            Assert.IsTrue(message.ShotDataOptions.IsHeartBeat);
        }

        [Test]
        public void GSProMessage_CreateHeartbeat_NotReady_SetsCorrectState()
        {
            var message = GSProMessage.CreateHeartbeat(false, false);

            Assert.IsFalse(message.ShotDataOptions.LaunchMonitorIsReady);
            Assert.IsFalse(message.ShotDataOptions.LaunchMonitorBallDetected);
        }

        #endregion

        #region JSON Format Tests

        [Test]
        public void GSProMessage_ToJson_MatchesGSProFormat()
        {
            var message = new GSProMessage
            {
                ShotNumber = 5,
                BallData = new GSProBallData
                {
                    Speed = 150.5f,
                    SpinAxis = 5.2f,
                    TotalSpin = 2800f,
                    BackSpin = 2750f,
                    SideSpin = 450f,
                    HLA = -2.5f,
                    VLA = 12.3f
                },
                ShotDataOptions = new GSProShotOptions
                {
                    ContainsBallData = true,
                    ContainsClubData = false,
                    LaunchMonitorIsReady = true,
                    LaunchMonitorBallDetected = true,
                    IsHeartBeat = false
                }
            };

            var json = message.ToJson();
            var parsed = JObject.Parse(json);

            Assert.AreEqual("GC2 Connect Unity", parsed["DeviceID"].Value<string>());
            Assert.AreEqual("Yards", parsed["Units"].Value<string>());
            Assert.AreEqual(5, parsed["ShotNumber"].Value<int>());
            Assert.AreEqual("1", parsed["APIversion"].Value<string>());
            Assert.AreEqual(150.5f, parsed["BallData"]["Speed"].Value<float>(), 0.01f);
            Assert.AreEqual(-2.5f, parsed["BallData"]["HLA"].Value<float>(), 0.01f);
            Assert.IsTrue(parsed["ShotDataOptions"]["ContainsBallData"].Value<bool>());
        }

        [Test]
        public void GSProMessage_HeartbeatJson_HasEmptyBallData()
        {
            var message = GSProMessage.CreateHeartbeat(true, false);
            var json = message.ToJson();
            var parsed = JObject.Parse(json);

            Assert.IsNotNull(parsed["BallData"]);
            Assert.AreEqual(0, parsed["ShotNumber"].Value<int>());
            Assert.IsTrue(parsed["ShotDataOptions"]["IsHeartBeat"].Value<bool>());
        }

        [Test]
        public void GSProMessage_WithClubData_IncludesAllClubFields()
        {
            var message = new GSProMessage
            {
                ShotNumber = 1,
                ClubData = new GSProClubData
                {
                    Speed = 105.2f,
                    AngleOfAttack = -3.5f,
                    FaceToTarget = 1.2f,
                    Lie = 0.5f,
                    Loft = 28.5f,
                    Path = 2.1f,
                    SpeedAtImpact = 104.8f,
                    VerticalFaceImpact = 0.3f,
                    HorizontalFaceImpact = -0.1f,
                    ClosureRate = 450f
                },
                ShotDataOptions = new GSProShotOptions { ContainsClubData = true }
            };

            var json = message.ToJson();
            var parsed = JObject.Parse(json);

            Assert.AreEqual(105.2f, parsed["ClubData"]["Speed"].Value<float>(), 0.01f);
            Assert.AreEqual(-3.5f, parsed["ClubData"]["AngleOfAttack"].Value<float>(), 0.01f);
            Assert.AreEqual(2.1f, parsed["ClubData"]["Path"].Value<float>(), 0.01f);
        }

        #endregion

        #region GSProBallData Tests

        [Test]
        public void GSProBallData_DefaultValues_AreZero()
        {
            var data = new GSProBallData();

            Assert.AreEqual(0f, data.Speed);
            Assert.AreEqual(0f, data.SpinAxis);
            Assert.AreEqual(0f, data.TotalSpin);
            Assert.AreEqual(0f, data.BackSpin);
            Assert.AreEqual(0f, data.SideSpin);
            Assert.AreEqual(0f, data.HLA);
            Assert.AreEqual(0f, data.VLA);
        }

        [Test]
        public void GSProBallData_SetValues_ArePreserved()
        {
            var data = new GSProBallData
            {
                Speed = 167f,
                SpinAxis = -3.5f,
                TotalSpin = 2686f,
                BackSpin = 2680f,
                SideSpin = -164f,
                HLA = -1.2f,
                VLA = 10.9f
            };

            Assert.AreEqual(167f, data.Speed);
            Assert.AreEqual(-3.5f, data.SpinAxis);
            Assert.AreEqual(2686f, data.TotalSpin);
            Assert.AreEqual(2680f, data.BackSpin);
            Assert.AreEqual(-164f, data.SideSpin);
            Assert.AreEqual(-1.2f, data.HLA);
            Assert.AreEqual(10.9f, data.VLA);
        }

        #endregion

        #region GSProClubData Tests

        [Test]
        public void GSProClubData_DefaultValues_AreZero()
        {
            var data = new GSProClubData();

            Assert.AreEqual(0f, data.Speed);
            Assert.AreEqual(0f, data.AngleOfAttack);
            Assert.AreEqual(0f, data.FaceToTarget);
            Assert.AreEqual(0f, data.Lie);
            Assert.AreEqual(0f, data.Loft);
            Assert.AreEqual(0f, data.Path);
            Assert.AreEqual(0f, data.SpeedAtImpact);
            Assert.AreEqual(0f, data.VerticalFaceImpact);
            Assert.AreEqual(0f, data.HorizontalFaceImpact);
            Assert.AreEqual(0f, data.ClosureRate);
        }

        [Test]
        public void GSProClubData_SetValues_ArePreserved()
        {
            var data = new GSProClubData
            {
                Speed = 87.5f,
                AngleOfAttack = -4.2f,
                FaceToTarget = 0.8f,
                Lie = 1.2f,
                Loft = 31.5f,
                Path = -2.1f
            };

            Assert.AreEqual(87.5f, data.Speed);
            Assert.AreEqual(-4.2f, data.AngleOfAttack);
            Assert.AreEqual(0.8f, data.FaceToTarget);
            Assert.AreEqual(1.2f, data.Lie);
            Assert.AreEqual(31.5f, data.Loft);
            Assert.AreEqual(-2.1f, data.Path);
        }

        #endregion

        #region GSProShotOptions Tests

        [Test]
        public void GSProShotOptions_DefaultLaunchMonitorIsReady_IsTrue()
        {
            var options = new GSProShotOptions();

            Assert.IsTrue(options.LaunchMonitorIsReady);
        }

        [Test]
        public void GSProShotOptions_DefaultValues_AreFalseExceptReady()
        {
            var options = new GSProShotOptions();

            Assert.IsFalse(options.ContainsBallData);
            Assert.IsFalse(options.ContainsClubData);
            Assert.IsFalse(options.LaunchMonitorBallDetected);
            Assert.IsFalse(options.IsHeartBeat);
        }

        #endregion
    }
}
