using System;

namespace RailDriver
{
    internal sealed class RingBuffer
    {
        private readonly int elementCount;
        private readonly int elementSize;
        private int writePosition;
        private int readPosition;
        private bool overflow;
        private bool noData;
        private readonly byte[] ringBuffer;
        private readonly object syncRoot = new object();

        /// <summary>
        /// Creating a new RingBuffer instance for elements Elements of size elementSize (in byte)
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="elementSize"></param>
        public RingBuffer(int elements, int elementSize)
        {
            elementCount = elements;
            this.elementSize = elementSize;
            ringBuffer = new byte[elementCount * this.elementSize];
        }

        /// <summary>
        /// Puts a new data element into the ring buffer. If the buffer
        /// is full(ie the get() has not been called for too long) the result
        /// will be false and the data is not entered into
        /// the buffer.
        /// </summary>
        public bool TryPut(byte[] data)
        {
            lock (syncRoot)
            {
                if (overflow)
                    return false;
                writePosition++;
                if (writePosition == elementCount)
                    writePosition = 0;
                if (writePosition == readPosition)
                    overflow = true;
                data.CopyTo(ringBuffer, elementSize * writePosition);
                noData = false;
                return true;
            }
        }

        /// <summary>
        /// Puts a new data element into the ring buffer. If the buffer
        /// is full(ie the get() has not been called for too long) then
        /// the oldest data is overwritten.
        /// </summary>
        public void Put(byte[] data)
        {
            lock (syncRoot)
            {
                writePosition++;
                if (writePosition == elementCount)
                    writePosition = 0;
                if (overflow)
                {
                    readPosition++;
                    if (readPosition == elementCount)
                        readPosition = 0;
                }
                if (writePosition == readPosition)
                    overflow = true;
                data.CopyTo(ringBuffer, elementSize * writePosition);
                noData = false;
            }
        }

        /// <summary>
        /// Puts a new data element into the ring buffera value 
        /// if this element is different than the previous entry.
        /// </summary>
        public bool TryPutChanged(byte[] data)
        {
            lock (syncRoot)
            {
                bool different = false;
                for (int j = 0; j < elementSize; j++)
                {
                    if (data[j] != ringBuffer[j + writePosition * elementSize])
                    {
                        different = true;
                        break;
                    }
                }
                if (!different)
                    return false;
                writePosition++;
                if (writePosition == elementCount)
                    writePosition = 0;
                if (overflow)
                {
                    readPosition++;
                    if (readPosition == elementCount)
                        readPosition = 0;
                }
                if (writePosition == readPosition)
                    overflow = true;
                data.CopyTo(ringBuffer, elementSize * writePosition);
                noData = false;
                return true;
            }
        }

        /// <summary>
        /// Gets the current element from the ring buffer.
        /// </summary>
        public bool Get(byte[] data)
        {
            lock (syncRoot)
            {
                if ((!overflow) && (readPosition == writePosition))
                    return false;
                overflow = false;
                readPosition++;
                if (readPosition == elementCount)
                    readPosition = 0;
                Array.Copy(ringBuffer, readPosition * elementSize, data, 0, elementSize);
                return true;
            }
        }

        /// <summary>
        /// Retrieves the most recently entered value
        /// from the ring buffer without modifing the status
        /// of the buffer. "getlast()" is not affected by the use
        /// of get() and will retrive the same value over again
        /// if put() has not been used in the intervening period.
        /// </summary>
        public int GetLast(byte[] data)
        {
            lock (syncRoot)
            {
                if (noData)
                    return 2;
                Array.Copy(ringBuffer, writePosition * elementSize, data, 0, elementSize);
                return 0;
            }
        }

        /// <summary>
        /// Clears and initializes the ring buffer. After a call
        /// to clear and before any calls to put() the GetLast
        /// will return 2 (no data yet) and Get will return 1 (no data).
        /// </summary>
        public void Clear()
        {
            lock (syncRoot)
            {
                writePosition = 0; //offset to put element
                readPosition = 0; //offset to get element
                noData = true; //flag for first time
                overflow = false; //flag for overflow
                Array.Clear(ringBuffer, 0, elementSize * elementCount);
            }
        }

        /// <summary>
        ///  Returns true if calling get() would result in the return of no data.
        /// </summary>
        public bool IsEmpty()
        {
            lock (syncRoot)
            {
                return (!overflow) && (readPosition == writePosition);
            }
        }
    }
}


