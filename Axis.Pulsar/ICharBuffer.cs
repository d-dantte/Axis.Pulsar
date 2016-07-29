using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar
{
    public interface IChar
    {
        char Character { get; }

        /// <summary>
        /// Returns the character this IChar represents as a string
        /// </summary>
        /// <returns>The string value of the character this object represents</returns>
        string ToString();
    }

    public static class ICharUtil
    {
        public static string AsString(this IEnumerable<IChar> ichars)
        {
            if (ichars == null) return null;
            else
            {
                var sb = new StringBuilder();
                foreach (var ichar in ichars) sb.Append(ichar);
                return sb.ToString();
            }
        }
    }

    public interface ICharBuffer
    {
        #region Properties
        IEnumerator<IChar> Source { get; set; }
        //bool IsLastBlockLoaded { get; }
        int BufferSize { get; set; }
        #endregion

        IBookmarkedStream Bookmark(int position = 0);
    }

    public interface IBookmarkedStream : IEnumerable<IChar>, IEnumerator<IChar>
    {
        ICharBuffer Buffer { get; set; }

        bool IsEndOfStream { get; }

        int OriginalPosition { get; set; }
        int AbsolutePosition { get; }

        IBookmarkedStream Bookmark(int relativePosition = 0);
    }
}