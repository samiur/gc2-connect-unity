// ABOUTME: Aerodynamic calculations for golf ball flight simulation.
// ABOUTME: Provides drag/lift coefficients, Reynolds number, and air density calculations.

using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Aerodynamic calculations for golf ball flight.
    /// Based on WSU wind tunnel research.
    /// </summary>
    public static class Aerodynamics
    {
        /// <summary>
        /// Calculate Reynolds number.
        /// </summary>
        /// <param name="velocityMs">Velocity in m/s</param>
        /// <param name="airDensity">Air density in kg/m³</param>
        /// <returns>Reynolds number</returns>
        public static float CalculateReynolds(float velocityMs, float airDensity)
        {
            // Re = V × D / ν
            return (velocityMs * PhysicsConstants.BallDiameterM) / PhysicsConstants.KinematicViscosity;
        }

        /// <summary>
        /// Get drag coefficient from Reynolds number using WSU data.
        /// </summary>
        /// <param name="reynolds">Reynolds number</param>
        /// <returns>Drag coefficient Cd</returns>
        public static float GetDragCoefficient(float reynolds)
        {
            // Convert to units of 10^5 for table lookup
            float re = reynolds / 100000f;

            // Above table range - use supercritical value
            if (re >= 2.0f)
            {
                return PhysicsConstants.CdSupercritical;
            }

            // Below table range - use first value
            if (re <= PhysicsConstants.CdTable[0].x)
            {
                return PhysicsConstants.CdTable[0].y;
            }

            // Linear interpolation in table
            for (int i = 0; i < PhysicsConstants.CdTable.Length - 1; i++)
            {
                if (re >= PhysicsConstants.CdTable[i].x && re < PhysicsConstants.CdTable[i + 1].x)
                {
                    float t = (re - PhysicsConstants.CdTable[i].x) /
                              (PhysicsConstants.CdTable[i + 1].x - PhysicsConstants.CdTable[i].x);
                    return Mathf.Lerp(PhysicsConstants.CdTable[i].y, PhysicsConstants.CdTable[i + 1].y, t);
                }
            }

            // Fallback
            return PhysicsConstants.CdSupercritical;
        }

        /// <summary>
        /// Get lift coefficient from spin factor using WSU data.
        /// </summary>
        /// <param name="spinFactor">Spin factor S = ωr/V</param>
        /// <returns>Lift coefficient Cl</returns>
        public static float GetLiftCoefficient(float spinFactor)
        {
            if (spinFactor <= 0)
            {
                return 0f;
            }

            // Above table range - use max value
            if (spinFactor >= PhysicsConstants.ClTable[PhysicsConstants.ClTable.Length - 1].x)
            {
                return PhysicsConstants.ClMax;
            }

            // Linear interpolation in table
            for (int i = 0; i < PhysicsConstants.ClTable.Length - 1; i++)
            {
                if (spinFactor >= PhysicsConstants.ClTable[i].x && spinFactor < PhysicsConstants.ClTable[i + 1].x)
                {
                    float t = (spinFactor - PhysicsConstants.ClTable[i].x) /
                              (PhysicsConstants.ClTable[i + 1].x - PhysicsConstants.ClTable[i].x);
                    return Mathf.Lerp(PhysicsConstants.ClTable[i].y, PhysicsConstants.ClTable[i + 1].y, t);
                }
            }

            return 0f;
        }

        /// <summary>
        /// Calculate air density based on environmental conditions.
        /// </summary>
        /// <param name="tempF">Temperature in Fahrenheit</param>
        /// <param name="elevationFt">Elevation in feet</param>
        /// <param name="humidityPct">Relative humidity percentage</param>
        /// <param name="pressureInHg">Barometric pressure in inches of mercury</param>
        /// <returns>Air density in kg/m³</returns>
        public static float CalculateAirDensity(
            float tempF,
            float elevationFt,
            float humidityPct,
            float pressureInHg = PhysicsConstants.StdPressureInHg)
        {
            // Convert temperature to Kelvin
            float tempC = (tempF - 32f) * 5f / 9f;
            float tempK = tempC + 273.15f;

            // Pressure adjustment for elevation (barometric formula)
            float elevationM = elevationFt * 0.3048f;
            float pressurePa = pressureInHg * 3386.39f;  // inHg to Pa
            float pressureAtAlt = pressurePa * Mathf.Exp(-0.0001185f * elevationM);

            // Saturation vapor pressure (Magnus formula)
            float es = 6.1078f * Mathf.Exp((17.27f * tempC) / (tempC + 237.3f));  // hPa
            float e = (humidityPct / 100f) * es;  // Actual vapor pressure
            float ePa = e * 100f;  // hPa to Pa

            // Air density (ideal gas law with humidity correction)
            const float Rd = 287.05f;   // Dry air gas constant
            const float Rv = 461.495f;  // Water vapor gas constant

            float pd = pressureAtAlt - ePa;  // Partial pressure of dry air
            float density = (pd / (Rd * tempK)) + (ePa / (Rv * tempK));

            return density;
        }

        /// <summary>
        /// Calculate spin factor (dimensionless).
        /// </summary>
        /// <param name="spinRpm">Total spin in rpm</param>
        /// <param name="velocityMs">Ball velocity in m/s</param>
        /// <returns>Spin factor S</returns>
        public static float CalculateSpinFactor(float spinRpm, float velocityMs)
        {
            if (velocityMs < 0.1f) return 0f;

            float omega = UnitConversions.RpmToRadS(spinRpm);
            return (omega * PhysicsConstants.BallRadiusM) / velocityMs;
        }
    }
}
