using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClosedXML.Report.Tests.Utils
{
    /// <summary>
    ///     Help methods for work with streams
    /// </summary>
    public static class StreamHelper
    {
        /// <summary>
        ///     Convert stream to byte array
        /// </summary>
        /// <param name="pStream">Stream</param>
        /// <returns>Byte array</returns>
        public static byte[] StreamToArray(Stream pStream)
        {
            long iLength = pStream.Length;
            var bytes = new byte[iLength];
            for (int i = 0; i < iLength; i++)
            {
                bytes[i] = (byte)pStream.ReadByte();
            }
            pStream.Close();
            return bytes;
        }

        /// <summary>
        ///     Convert byte array to stream
        /// </summary>
        /// <param name="pBynaryArray">Byte array</param>
        /// <param name="pStream">Open stream</param>
        /// <returns></returns>
        public static Stream ArrayToStreamAppend(byte[] pBynaryArray, Stream pStream)
        {
            #region Check params

            if (ReferenceEquals(pBynaryArray, null))
            {
                throw new ArgumentNullException("pBynaryArray");
            }
            if (ReferenceEquals(pStream, null))
            {
                throw new ArgumentNullException("pStream");
            }
            if (!pStream.CanWrite)
            {
                throw new ArgumentException("Can't write to stream", "pStream");
            }

            #endregion

            foreach (byte b in pBynaryArray)
            {
                pStream.WriteByte(b);
            }
            return pStream;
        }

        public static void StreamToStreamAppend(Stream streamIn, Stream streamToWrite)
        {
            StreamToStreamAppend(streamIn, streamToWrite, 0);
        }

        public static void StreamToStreamAppend(Stream streamIn, Stream streamToWrite, long dataLength)
        {
            #region Check params

            if (ReferenceEquals(streamIn, null))
            {
                throw new ArgumentNullException("streamIn");
            }
            if (ReferenceEquals(streamToWrite, null))
            {
                throw new ArgumentNullException("streamToWrite");
            }
            if (!streamIn.CanRead)
            {
                throw new ArgumentException("Can't read from stream", "streamIn");
            }
            if (!streamToWrite.CanWrite)
            {
                throw new ArgumentException("Can't write to stream", "streamToWrite");
            }

            #endregion

            var buf = new byte[512];
            long length;
            if (dataLength == 0)
            {
                length = streamIn.Length - streamIn.Position;
            }
            else
            {
                length = dataLength;
            }
            long rest = length;
            while (rest > 0)
            {
                int len1 = streamIn.Read(buf, 0, rest >= 512 ? 512 : (int)rest);
                streamToWrite.Write(buf, 0, len1);
                rest -= len1;
            }
        }

        /// <summary>
        ///     Compare two streams by converting them to strings and comparing the strings
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// /// <param name="stripColumnWidths"></param>
        /// <returns></returns>
        public static bool Compare(Stream one, Stream other, bool stripColumnWidths)
        {
            #region Check

            if (one == null)
            {
                throw new ArgumentNullException("one");
            }
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (one.Position != 0)
            {
                throw new ArgumentException("Must be in position 0", "one");
            }
            if (other.Position != 0)
            {
                throw new ArgumentException("Must be in position 0", "other");
            }

            #endregion

            var stringOne = new StreamReader(one).ReadToEnd().RemoveIgnoredParts(stripColumnWidths, ignoreGuids: true);
            var stringOther = new StreamReader(other).ReadToEnd().RemoveIgnoredParts(stripColumnWidths, ignoreGuids: true);
            var equal = stringOne == stringOther;
            if (!equal)
                LogDiff(stringOne, stringOther);
            return equal;
        }

        private static string RemoveIgnoredParts(this string s, Boolean ignoreColumnWidths, Boolean ignoreGuids)
        {
            if (ignoreColumnWidths)
                s = RemoveColumnWidths(s);

            if (ignoreGuids)
                s = RemoveGuids(s);

            return s;
        }

        private static void LogDiff(string stringOne, string stringOther)
        {
            var len = stringOne.Length;
            int idx;
            for (idx = 0; idx < len; idx++)
            {
                if (stringOne[idx] != stringOther[idx])
                    break;
            }
            if (idx < len)
            {
                var l = 40;
                System.Diagnostics.Debug.WriteLine("Fonded differense: \n{0}>>>|<<<{1} \n{2}>>>|<<<{3}",
                    stringOne.Substring(Math.Max(0, idx - l), Math.Min(l, idx)),
                    stringOne.Substring(idx, Math.Min(l, stringOne.Length - idx)),
                    stringOther.Substring(Math.Max(0, idx - l), Math.Min(l, idx)),
                    stringOther.Substring(Math.Min(idx, stringOther.Length), Math.Min(l, stringOther.Length-idx)));
            }
        }

        private static Regex columnRegex = new Regex("<x:col.*?width=\"\\d+(\\.\\d+)?\".*?\\/>", RegexOptions.Compiled);
        private static Regex widthRegex = new Regex("width=\"\\d+(\\.\\d+)?\"\\s+", RegexOptions.Compiled);

        private static String RemoveColumnWidths(String s)
        {
                var replacements = new Dictionary<string, string>();

                foreach (var m in columnRegex.Matches(s).OfType<Match>())
                {
                    var original = m.Groups[0].Value;
                    var replacement = widthRegex.Replace(original, "");
                    replacements.Add(original, replacement);
                }

                foreach (var r in replacements)
                {
                    s = s.Replace(r.Key, r.Value);
                }
                return s;
            }

        private static Regex guidRegex = new Regex(@"{[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}}", RegexOptions.Compiled | RegexOptions.Multiline);

        private static String RemoveGuids(String s)
        {
            return guidRegex.Replace(s, delegate (Match m)
            {
                return string.Empty;
            });
        }
    }
}