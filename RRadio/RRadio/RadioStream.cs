using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Ronnrein.RRadio {

    /// <summary>
    /// Modified stream class for our purpose
    /// </summary>
    class RadioStream : Stream {

        public string MetaData;
        public int MetaInt;

        private readonly Stream sourceStream;
        private long pos;
        private readonly byte[] readAheadBuffer;
        private int readAheadLength;
        private int readAheadOffset;
        private int metadataLength;
        private int byteCount;
        private string metadataHeader = "";
        private string oldMetadataHeader = null;

        public RadioStream(Stream sourceStream) {
            this.sourceStream = sourceStream;
            readAheadBuffer = new byte[4096];
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = 0;
            while (bytesRead < count) {
                int readAheadAvailableBytes = readAheadLength - readAheadOffset;
                int bytesRequired = count - bytesRead;
                if (readAheadAvailableBytes > 0) {
                    int toCopy = Math.Min(readAheadAvailableBytes, bytesRequired);
                    Array.Copy(readAheadBuffer, readAheadOffset, buffer, offset + bytesRead, toCopy);
                    bytesRead += toCopy;
                    readAheadOffset += toCopy;
                }
                else {
                    readAheadOffset = 0;
                    readAheadLength = sourceStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);
                    
                    if (readAheadLength == 0) {
                        break;
                    }
                }
            }
            pos += bytesRead;
            return bytesRead;
        }

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return false; }
        }

        public override long Length {
            get { return pos; }
        }

        public override long Position {
            get { return pos; }
            set { throw new NotImplementedException(); }
        }

        public override void Flush() {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

    }
}