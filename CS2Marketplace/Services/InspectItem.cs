using System;
using System.Text.RegularExpressions;

namespace CS2Marketplace.Services
{
    public class InspectItem
    {
        public ulong ParamS { get; set; }
        public ulong ParamA { get; set; }
        public ulong ParamD { get; set; }
        public ulong ParamM { get; set; }

        private static readonly Regex LINK_REGEX = new Regex(@"(?:s(?<S>\d+)|m(?<M>\d+))a(?<A>\d+)d(?<D>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static InspectItem FromLink(string link)
        {
            var match = LINK_REGEX.Match(link.ToLower());
            if (!match.Success)
                throw new ArgumentException("Bad inspection link");

            return new InspectItem
            {
                ParamS = match.Groups["S"].Success ? Convert.ToUInt64(match.Groups["S"].Value) : 0,
                ParamA = Convert.ToUInt64(match.Groups["A"].Value),
                ParamD = Convert.ToUInt64(match.Groups["D"].Value),
                ParamM = match.Groups["M"].Success ? Convert.ToUInt64(match.Groups["M"].Value) : 0,
            };
        }
    }
}
