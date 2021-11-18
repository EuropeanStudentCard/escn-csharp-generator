using System;
using System.Text.RegularExpressions;

namespace esc_uuid
{
    /// <summary>  
    /// This class is used to generate unique European Student Card Number (ESCN) for the "european student card"
    /// This number is an UUID of 16 bytes (128 bits) and the generation algorithm is describe in RFC 4122 :
    /// Octet 0-3: time_low The low field of the timestamp
    /// Octet 4-5: time_mid The middle field of the timestamp
    /// Octet 6-7: time_hi_and_version The high field of the timestamp multiplexed with the version number
    /// Octet 8: clock_seq_hi_and_reserved The high field of the clock sequence multiplexed with the variant
    /// Octet 9: clock_seq_low The low field of the clock sequence
    /// Octet 10-15: node The spatially unique node identifier
    /// </summary>
    public class UuidFactory
    {
        // Offset from 15 Oct 1582 to 1 Jan 1970
        private static long OFFSET_MILLIS = 12219292800000L;
        private static readonly DateTime Jan1st1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long time = 0, oldSysTime = 0;
        private static int clock, hits = 0;

        /// <summary>
        /// This method calculate an UUID from 2 parameters.</summary>
        /// <param name="prefixe">To distinguish servers of a same institution.</param>
        /// <param name="pic">Participant Identification Code.</param>
        /// <returns>
        /// a unique ESCN.</returns>
        /// <see cref="UuidFactory.GetNode(int, string)"/>
        public string GetUuid(int prefixe, String pic)
        {
            string nodeStr = "";
            string node = new UuidFactory().GetNode(prefixe, pic);

            System.Console.WriteLine("GetUuid : node = {0}", node);

            if (--hits > 0) ++time;
            else
            {
                TimeSpan tsEpoch = DateTime.UtcNow.Subtract(Jan1st1970);
                long sysTime = (long)tsEpoch.TotalMilliseconds;

                System.Console.WriteLine("GetUuid : sysTime = {0}", sysTime);

                hits = 10000;

                if (sysTime <= oldSysTime)
                {
                    if (sysTime < oldSysTime)
                    {       // SYSTEM CLOCK WAS SET BACK
                        clock = (++clock & 0x3fff) | 0x8000;
                    }
                    else
                    {           // REQUESTING UUIDs TOO FAST FOR SYSTEM CLOCK
                        try
                        {
                            System.Threading.Thread.Sleep(1);
                        }
                        catch (Exception e)
                        {
                        }
                        sysTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    }
                }

                time = sysTime * 10000L + OFFSET_MILLIS;
                oldSysTime = sysTime;
            }

            int low = (int)time;
            int mid = (int)(time >> 32) & 0xffff;

            // 12 bit hi, set high 4 bits to '0001' for RFC 4122 version 1
            int hi = ((int)(time >> 48) & 0x0fff) | 0x1000;

            String lowStr = String.Format("{0:X}", low);
            String midStr = String.Format("{0:X}", mid);
            String hiStr = String.Format("{0:X}", hi);
            String clockStr = String.Format("{0:X}", clock);

            nodeStr = String.Format("{0}-{1}-{2}-{3}-{4}", lowStr.PadLeft(8, '0'), midStr.PadLeft(4, '0'), hiStr.PadLeft(4, '0'), clockStr.PadLeft(4, '0'), node.PadLeft(12, '0')).ToLower();

            return nodeStr;

        }

        /// <summary>
        /// This method calculate a node used for the ESCN creation and initiate the clock
        /// node = Prefixe + PIC
        /// Initialise également l'horloge.</summary>
        /// <param name="intPrefixe">To distinguish servers of a same institution.</param>
        /// <param name="pic">Participant Identification Code.</param>
        /// <returns>
        /// node.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Exception throw
        /// if <paramref name="intPrefixe"/>  is malformed
        /// if <paramref name="pic"/>  is malformed.
        /// </exception>

        private string GetNode(int intPrefixe, string pic)
        {
            string concatID;
            string prefixe;

            prefixe = intPrefixe.ToString("000");

            if (!Regex.IsMatch(prefixe, @"\d{3}"))
            {
                throw new ArgumentOutOfRangeException ("Invalid Prefixe format!");
            }
            if (!Regex.IsMatch(pic, @"\d{9}"))
            {
                throw new ArgumentOutOfRangeException ("Invalid PIC format!");
            }

            concatID = String.Concat(prefixe, pic);


            // 14 bit clock, set high 2 bits to '0001' for RFC 4122 variant 2
            Random random = new System.Random();
            clock = ((int)(random.NextDouble() * 0x3fff)) | 0x8000;

            return concatID;
        }
    }
}
