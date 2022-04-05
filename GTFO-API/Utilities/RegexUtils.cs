using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GTFO.API.Utilities
{
    /// <summary>
    /// Regex utilities class
    /// </summary>
    public static class RegexUtils
    {
        private static readonly Regex s_VectorRegex = new("-?[0-9.]+");

        /// <summary>
        /// Tries to parse an array of floats from json
        /// </summary>
        /// <param name="input">Json input string</param>
        /// <param name="vectorArray">Resulting array of floats</param>
        /// <returns>Whether parse was successful</returns>
        public static bool TryParseVectorString(string input, out float[] vectorArray)
        {
            try
            {
                MatchCollection matches = s_VectorRegex.Matches(input);
                int count = matches.Count;
                if (count < 1)
                    throw new Exception();

                vectorArray = new float[count];

                for (int i = 0; i < count; i++)
                {
                    Match match = matches[i];
                    vectorArray[i] = float.Parse(match.Value, CultureInfo.InvariantCulture);
                }

                return true;
            }
            catch
            {
                vectorArray = null;
                return false;
            }
        }
    }
}
