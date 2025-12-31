using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Unit conversion utilities for physics calculations.
    /// </summary>
    public static class UnitConversions
    {
        #region Speed Conversions
        
        /// <summary>Miles per hour to meters per second</summary>
        public static float MphToMs(float mph) => mph * 0.44704f;
        
        /// <summary>Meters per second to miles per hour</summary>
        public static float MsToMph(float ms) => ms / 0.44704f;
        
        /// <summary>Kilometers per hour to meters per second</summary>
        public static float KphToMs(float kph) => kph / 3.6f;
        
        /// <summary>Meters per second to kilometers per hour</summary>
        public static float MsToKph(float ms) => ms * 3.6f;
        
        /// <summary>Miles per hour to kilometers per hour</summary>
        public static float MphToKph(float mph) => mph * 1.60934f;
        
        /// <summary>Kilometers per hour to miles per hour</summary>
        public static float KphToMph(float kph) => kph / 1.60934f;
        
        #endregion
        
        #region Distance Conversions
        
        /// <summary>Yards to meters</summary>
        public static float YardsToMeters(float yards) => yards * 0.9144f;
        
        /// <summary>Meters to yards</summary>
        public static float MetersToYards(float meters) => meters / 0.9144f;
        
        /// <summary>Feet to meters</summary>
        public static float FeetToMeters(float feet) => feet * 0.3048f;
        
        /// <summary>Meters to feet</summary>
        public static float MetersToFeet(float meters) => meters / 0.3048f;
        
        /// <summary>Inches to meters</summary>
        public static float InchesToMeters(float inches) => inches * 0.0254f;
        
        /// <summary>Meters to inches</summary>
        public static float MetersToInches(float meters) => meters / 0.0254f;
        
        /// <summary>Yards to feet</summary>
        public static float YardsToFeet(float yards) => yards * 3f;
        
        /// <summary>Feet to yards</summary>
        public static float FeetToYards(float feet) => feet / 3f;
        
        #endregion
        
        #region Angle Conversions
        
        /// <summary>Degrees to radians</summary>
        public static float DegToRad(float deg) => deg * Mathf.Deg2Rad;
        
        /// <summary>Radians to degrees</summary>
        public static float RadToDeg(float rad) => rad * Mathf.Rad2Deg;
        
        #endregion
        
        #region Spin Conversions
        
        /// <summary>RPM to radians per second</summary>
        public static float RpmToRadS(float rpm) => rpm * 2f * Mathf.PI / 60f;
        
        /// <summary>Radians per second to RPM</summary>
        public static float RadSToRpm(float radS) => radS * 60f / (2f * Mathf.PI);
        
        #endregion
        
        #region Temperature Conversions
        
        /// <summary>Fahrenheit to Celsius</summary>
        public static float FahrenheitToCelsius(float f) => (f - 32f) * 5f / 9f;
        
        /// <summary>Celsius to Fahrenheit</summary>
        public static float CelsiusToFahrenheit(float c) => c * 9f / 5f + 32f;
        
        /// <summary>Celsius to Kelvin</summary>
        public static float CelsiusToKelvin(float c) => c + 273.15f;
        
        /// <summary>Kelvin to Celsius</summary>
        public static float KelvinToCelsius(float k) => k - 273.15f;
        
        #endregion
        
        #region Pressure Conversions
        
        /// <summary>Inches of mercury to Pascals</summary>
        public static float InHgToPa(float inHg) => inHg * 3386.39f;
        
        /// <summary>Pascals to inches of mercury</summary>
        public static float PaToInHg(float pa) => pa / 3386.39f;
        
        /// <summary>Millibars (hPa) to Pascals</summary>
        public static float MbToPa(float mb) => mb * 100f;
        
        /// <summary>Pascals to millibars (hPa)</summary>
        public static float PaToMb(float pa) => pa / 100f;
        
        #endregion
    }
}
