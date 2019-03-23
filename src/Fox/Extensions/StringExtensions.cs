using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fox.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Converts the string representation of a time interval with 'd h m s' format to its <see cref="TimeSpan" />
        ///     equivalent.
        /// </summary>
        /// <param name="time">A string that specify the time interval to convert.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <returns></returns>
        public static TimeSpan ToTimeSpan(this string time)
        {
            time = time.Replace(" ", "");
            string[] tab = { "0", "0", "0", "0" };
            var temp = "";
            foreach (var chr in time.ToLower())
            {
                if (char.IsNumber(chr))
                {
                    temp += chr;
                }
                else
                {
                    switch (chr)
                    {
                        case 'd':
                            tab[0] = temp;
                            temp = "";
                            break;
                        case 'h':
                            tab[1] = temp;
                            temp = "";
                            break;
                        case 'm':
                            tab[2] = temp;
                            temp = "";
                            break;
                        case 's':
                            tab[3] = temp;
                            temp = "";
                            break;
                        default:
                            throw new FormatException(
                                "The time format was not correct. Use 'd' for days, 'h' for hours, 'm' for minutes, 's' for seconds.");
                    }
                }
            }

            var dys = int.Parse(tab[0]);
            var hrs = int.Parse(tab[1]);
            var mns = int.Parse(tab[2]);
            var scs = int.Parse(tab[3]);

            if (scs > 59)
            {
                if (scs / 60 > 1)
                {
                    mns += scs / 60;
                    scs = scs % 60;
                }
                else
                {
                    mns += 1;
                    scs = 0;
                }
            }

            if (mns > 59)
            {
                if (mns / 60 > 1)
                {
                    hrs += mns / 60;
                    mns = mns % 60;
                }
                else
                {
                    hrs += 1;
                    mns = 0;
                }
            }

            if (hrs > 23)
            {
                if (hrs / 24 > 1)
                {
                    dys += hrs / 24;
                    hrs = hrs % 24;
                }
                else
                {
                    dys += 1;
                    hrs = 0;
                }
            }

            if (TimeSpan.TryParse($"{dys}.{hrs}:{mns}:{scs}", out var value))
            {
                return value;
            }

            throw new FormatException("The time format was not correct. Values must be positive.");
        }

        public static string Levenshtein(this string original, CommandService cmdsrvc)
        {
            if (original.Length < 3)
            {
                return null;
            }

            var commands = cmdsrvc.GetAllCommands().ToList();
            var cmds = commands.Select(cmd => cmd.Name).ToList();

            return original.Levenshtein(cmds);
        }

        public static string Levenshtein(this string original, IReadOnlyList<string> references)
        {
            //The algorithm is not enough accurate to deal with strings length less than 3
            if (original.Length < 3)
            {
                return null;
            }

            var distances = new List<int>();
            /*
            * Use Damerau-Levenshtein algorithm to find distance between given string with the given list of strings.
            * Damerau-Levenshtein sources: 
            * https://gist.github.com/wickedshimmy/449595/cb33c2d0369551d1aa5b6ff5e6a802e21ba4ad5c,
            * https://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
            */
            for (var x = 0; x < references.Count; x++)
            {
                var modified = references[x];

                if (modified is null)
                {
                    distances.Add(int.MaxValue);
                    continue;
                }

                var lenOrig = original.Length;
                var lenDiff = modified.Length;

                original = original.ToLowerInvariant();
                modified = modified.ToLowerInvariant();

                var matrix = new int[lenOrig + 1, lenDiff + 1];
                for (var i = 0; i <= lenOrig; i++)
                {
                    matrix[i, 0] = i;
                }

                for (var j = 0; j <= lenDiff; j++)
                {
                    matrix[0, j] = j;
                }

                for (var i = 1; i <= lenOrig; i++)
                {
                    for (var j = 1; j <= lenDiff; j++)
                    {
                        var cost = modified[j - 1] == original[i - 1] ? 0 : 1;
                        int[] vals =
                        {
                            matrix[i - 1, j] + 1,
                            matrix[i, j - 1] + 1,
                            matrix[i - 1, j - 1] + cost
                        };
                        matrix[i, j] = vals.Min();
                        if (i > 1 && j > 1 && original[i - 1] == modified[j - 2] && original[i - 2] == modified[j - 1])
                        {
                            matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);
                        }
                    }
                }

                distances.Add(matrix[lenOrig, lenDiff]);
            }

            //Try to find the appropriate command with following distances
            var current = 0;
            var index = 0;
            var min = int.MaxValue;
            foreach (var dist in distances)
            {
                if (dist < min)
                {
                    min = dist;
                    index = current;
                }

                current++;
            }

            var marginError = min / (double)original.Length;

            //If the minimal distance was greater than 4 or the margin error > 42%, we don't want to tell bullshit.
            return min > 4 || marginError > 0.42 ? null : references[index];
        }
    }
}
