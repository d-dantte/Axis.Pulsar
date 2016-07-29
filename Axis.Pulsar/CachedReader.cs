using Axis.Pulsar.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar
{
    public class Char : IChar
    {
        public char Character { get; set; }

        public override string ToString()
        {
            return this.Character.ToString();
        }
    }

    public class CachedReader : ICharBuffer
    {
        public CachedReader()
            : this(null)
        { }

        public CachedReader(StringSource source): this((IEnumerator<IChar>)source)
        { }

        public CachedReader(IEnumerator<IChar> source)
        {
            this.IsLastBlockLoaded = false;
            this.Source = source == null ? new List<IChar>().GetEnumerator() : source;
        }

        #region Properties and fields
        internal List<IChar> charBuffer = new List<IChar>();
        public IEnumerator<IChar> Source { get; set; }
        public bool IsLastBlockLoaded { get; private set; }
        public int BufferSize
        {
            get { return this._bufferSize; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                this._bufferSize = value;
            }
        }

        private static int DefaultBufferSize = 1; //for some arcane reason, this performs better than larger values
        private int _bufferSize = DefaultBufferSize;
        #endregion
        
        internal bool LoadBlock()
        {
            if (IsLastBlockLoaded) return false;
            else
            {
                int count = charBuffer.Count;
                for (int cnt = 0; cnt < this._bufferSize && (!(IsLastBlockLoaded = !Source.MoveNext())); cnt++)
                    charBuffer.Add(Source.Current);

                return charBuffer.Count > count;
            }
        }
        internal bool loadOne()
        {
            if (this.IsLastBlockLoaded = !Source.MoveNext()) return false;

            charBuffer.Add(Source.Current);
            return true;
        }

        public IBookmarkedStream Bookmark(int position = 0)
        {
            if (position < 0) throw new ArgumentException();
            else return new BookmarkedStream { Buffer = this, OriginalPosition = position };
        }
    }

    public class BookmarkedStream : IBookmarkedStream
    {
        public ICharBuffer Buffer { get; set; }

        public bool IsEndOfStream => (Buffer as CachedReader).charBuffer.Count <= AbsolutePosition;

        public int OriginalPosition { get; set; }
        private int currentPos = -1;
        public int AbsolutePosition { get { return this.currentPos + this.OriginalPosition; } }

        public IBookmarkedStream Bookmark(int relativePosition = 0)
        {
            //+1 because the new bookmark's original position should be same as this bookmark
            return this.Buffer.Bookmark(this.AbsolutePosition + 1 + relativePosition);
        }


        #region IEnumerable Members
        public IEnumerator<IChar> GetEnumerator() => this;

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region IEnumerator Members
        public IChar Current
        {
            get
            {
                var creader = this.Buffer as CachedReader;
                if (this.currentPos < 0) throw new InvalidOperationException("Invalid Current-Position");
                else return creader.charBuffer[this.currentPos + this.OriginalPosition];
            }
        }
        object System.Collections.IEnumerator.Current => Current;

        public void Dispose()
        {
            this.Reset();
        }

        public bool MoveNext()
        {
            var creader = this.Buffer as CachedReader;

            if (currentPos + 1 + this.OriginalPosition == creader.charBuffer.Count)
            {
                if (this.Buffer.BufferSize > 1 && !creader.LoadBlock())
                    return false;
                else if (!creader.loadOne())
                    return false;
            }

            this.currentPos++;
            return true;
        }

        public void Reset()
        {
            this.currentPos = -1;
        }
        #endregion
    }

    public class StringSource : IEnumerator<IChar>
    {
        public string source { get; set; }
        private int pos = -1;

        public StringSource(string source)
        {
            this.source = source;
        }
        public StringSource()
        { }

        #region operator overloads
        public static implicit operator StringSource(string source)
        {
            return new StringSource(source);
        }
        #endregion

        #region IEnumerator members
        public IChar Current => new Char { Character = this.source[this.pos] };

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (this.pos == this.source.Length) return false;
            else return (++this.pos) < this.source.Length;
        }

        public void Reset()
        {
            this.pos = -1;
        }
        public void Dispose()
        {
            this.Reset();
        }
        #endregion
    }
}
