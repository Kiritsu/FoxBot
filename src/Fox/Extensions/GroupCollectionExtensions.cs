using System;
using System.Text.RegularExpressions;

namespace Fox.Extensions
{
    public static class GroupCollectionExtensions
    {
        public static TimeSpan ToTimeSpan(this GroupCollection matchGroups)
        {
            var time = "";

            if (matchGroups["days"].Success)
            {
                time += matchGroups["days"].ToString().Split(' ')[0] + "d";
            }

            if (matchGroups["hours"].Success)
            {
                time += matchGroups["hours"].ToString().Split(' ')[0] + "h";
            }

            if (matchGroups["minutes"].Success)
            {
                time += matchGroups["minutes"].ToString().Split(' ')[0] + "m";
            }

            if (matchGroups["seconds"].Success)
            {
                time += matchGroups["seconds"].ToString().Split(' ')[0] + "s";
            }

            return time.ToTimeSpan();
        }
    }
}
