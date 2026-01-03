// ABOUTME: GSPro Open Connect API v1 response classes for JSON deserialization.
// ABOUTME: Defines response codes, messages, and player info from GSPro.

using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenRange.Network
{
    /// <summary>
    /// Response from GSPro after sending shot data.
    /// GSPro responds with success/error codes and optional player info.
    /// </summary>
    [Serializable]
    public class GSProResponse
    {
        /// <summary>Response code from GSPro.</summary>
        /// <remarks>
        /// 200 = Success
        /// 201 = Success with player info
        /// 5xx = Error
        /// </remarks>
        [JsonProperty("Code")]
        public int Code { get; set; }

        /// <summary>Status message from GSPro.</summary>
        [JsonProperty("Message")]
        public string Message { get; set; }

        /// <summary>Optional player info (present when Code = 201).</summary>
        [JsonProperty("Player", NullValueHandling = NullValueHandling.Ignore)]
        public GSProPlayerInfo Player { get; set; }

        /// <summary>Whether the response indicates success (2xx code).</summary>
        public bool IsSuccess => Code >= 200 && Code < 300;

        /// <summary>Whether the response includes player info (code 201).</summary>
        public bool HasPlayerInfo => Code == 201 && Player != null;

        /// <summary>
        /// Parse a GSPro response from JSON string.
        /// </summary>
        /// <param name="json">JSON string to parse.</param>
        /// <returns>Parsed response or null if parsing fails.</returns>
        public static GSProResponse FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<GSProResponse>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract and parse the first JSON object from a buffer that may contain
        /// multiple concatenated JSON objects.
        /// </summary>
        /// <param name="data">Buffer potentially containing multiple JSON objects.</param>
        /// <returns>Parsed response from first JSON object, or null if parsing fails.</returns>
        /// <remarks>
        /// GSPro may buffer and concatenate responses, sending them together:
        /// {"Code":200,"Message":"OK"}{"Code":200,"Message":"OK"}
        /// This method extracts only the first complete JSON object.
        /// </remarks>
        public static GSProResponse ParseFirstObject(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            // Find the first JSON object by matching braces
            int startIndex = data.IndexOf('{');
            if (startIndex < 0)
                return null;

            int braceCount = 0;
            int endIndex = -1;

            for (int i = startIndex; i < data.Length; i++)
            {
                char c = data[i];
                if (c == '{')
                {
                    braceCount++;
                }
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            if (endIndex < 0)
                return null;

            string firstJson = data.Substring(startIndex, endIndex - startIndex + 1);
            return FromJson(firstJson);
        }

        /// <summary>
        /// Extract and parse the first JSON object from a byte buffer.
        /// </summary>
        /// <param name="buffer">Byte buffer potentially containing multiple JSON objects.</param>
        /// <param name="length">Number of bytes to read from buffer.</param>
        /// <returns>Parsed response from first JSON object, or null if parsing fails.</returns>
        public static GSProResponse ParseFirstObject(byte[] buffer, int length)
        {
            if (buffer == null || length <= 0)
                return null;

            string data = Encoding.UTF8.GetString(buffer, 0, length);
            return ParseFirstObject(data);
        }
    }

    /// <summary>
    /// Player information from GSPro response.
    /// Included when response code is 201.
    /// </summary>
    [Serializable]
    public class GSProPlayerInfo
    {
        /// <summary>Player handedness ("RH" = right-handed, "LH" = left-handed).</summary>
        [JsonProperty("Handed")]
        public string Handed { get; set; }

        /// <summary>
        /// Current club selection in GSPro.
        /// Common values: "DR", "3W", "5W", "4I"-"9I", "PW", "GW", "SW", "LW", "PT"
        /// </summary>
        [JsonProperty("Club")]
        public string Club { get; set; }

        /// <summary>Distance to target/pin in yards.</summary>
        [JsonProperty("DistanceToTarget")]
        public float DistanceToTarget { get; set; }

        /// <summary>Whether player is right-handed.</summary>
        public bool IsRightHanded => Handed == "RH";

        /// <summary>Whether player is left-handed.</summary>
        public bool IsLeftHanded => Handed == "LH";
    }
}
