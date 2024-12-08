/*------------------------------------------------
RngBuf2                            class

 Built by Jack Hetherington -1/21/03

Instantiating this class sets up a ring buffer into
which data of an arbitrary size (determined at time of
instantiation) can be stored (using the  put()
function) and retrieved (using the get() or getlast()
functions.)

All calls into this class are now protected as Critical Sections.--2/11/11
Private functions lock() and unlock() added 2/11

comstructor:
  RngBuf2 a(N,M);
  a  --instance name for the buffer
  N  --size of the buffer ring(ie N is the number of
       elements of size M which can be retained in the
       buffer before deletion occurs.)
  M  --number of bytes in each element to be stored.
 Example1:
    RngBuf2 ringbuffer(40,13);
    A ring buffer holding 40 elements of size 13.

public functions:
----------------------------------------------------
(void)put(char* x)
Puts a value of type c into the ring buffer. If the buffer
is full(ie the get() has not been called for too long) then
the oldest data is deleted.


  x --name of the structure being stored

   Example1:
      RngBuf2 ringbuffer(16,4);
      float x;
      ringbuffer.put((char *)&x);
   Example2:
    RngBuf2 ringbuffer(16,20);
      struct cc{char b[20];};
      cc B;
      ringbuffer.put((char *)&B);
---------------------------------------------------
(int )putIfCan(char* x)
Puts a value of type c into the ring buffer. If the buffer
is full(ie the get() has not been called for too long) an
error message is returned and the data is not entered into
the buffer.


  x --name of the structure being stored

   result:  0 -success
            3 -buffer full

   Example1:
      RngBuf2 ringbuffer(16,4);
      float x;
      err=ringbuffer.putIfCan((char *)&x);

---------------------------------------------------
(int )putIfDiff(char* x)  //-------- added 2/11
Puts a value of type c into the ring buffer if different 
than the previous entry. 

  x --name of the structure being stored

   result:  0 -success
            1 -not different

   Example:
      RngBuf2 ringbuffer(16,4);
      float x;
      err=ringbuffer.putIfDiff((char *)&x);

----------------------------------------
(int)get(char *x)
Gets a value from the ring buffer.

  x --name of the structure being retrieved
  result:  0 -success
           1 -no new data available
    Example:

      float x;
      RngBuf2 ringbuffer(16,sizeof(float));
      int result;
      result=ringbuffer.get((char*)&x);
--------------------------------------------------------
(int)getlast(char *x)
Retrieves the most recently entered value
from the ring buffer without modifing the status
of the buffer. "getlast()" is not affected by the use
of get() and will retrive the same value over again
if put() has not been used in the intervening period.

  x --name of the structure being retrieved
  result:  0 -success
           2 -no data yet placed by put
    Example:
      float x;
      RngBuf2 ringbuffer(16,sizeof(float));
      int result;
      result=ringbuffer.getlast((char *)&x);
---------------------------------------------------
(void)clear()
Clears and initializes the ring buffer. After a call
to clear and before any calls to put() the get last
will return 2 (no data yet) and get will return 1 (no
data).

   Example:
     ringbuffer.clear();
------------------------------------------------------------------
 bool IsEmpty() //---------- added 2/11
 Returns true if calling get() would result in the return of no data.

 Example:
  if( ringbuffer.IsEmpty())return 1;
------------------------------------------------------
Comments:

-------------------------------------------------*/
using System;
using System.Threading;

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

        private void Lock()
        {
            Monitor.Enter(elementCount);
            return;
        }

        private void Unlock()
        {
            Monitor.Exit(elementCount);
            return;
        }

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
            Monitor.Enter(elementCount);
            Monitor.Exit(elementCount);
        }

        /// <summary>
        /// Puts a new data element into the ring buffer. If the buffer
        /// is full(ie the get() has not been called for too long) the result
        /// will be false and the data is not entered into
        /// the buffer.
        /// </summary>
        public bool TryPut(byte[] data)
        {
            Lock();
            if (overflow)
            {
                Unlock();
                return false;
            }
            writePosition++;
            if (writePosition == elementCount)
                writePosition = 0;
            if (writePosition == readPosition)
                overflow = true;
            data.CopyTo(ringBuffer, elementSize * writePosition);
            noData = false;
            Unlock();
            return true;
        }

        /// <summary>
        /// Puts a new data element into the ring buffer. If the buffer
        /// is full(ie the get() has not been called for too long) then
        /// the oldest data is overwritten.
        /// </summary>
        public void Put(byte[] data)
        {
            Lock();
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
            Unlock();
        }

        /// <summary>
        /// Puts a new data element into the ring buffera value 
        /// if this element is different than the previous entry.
        /// </summary>
        public bool TryPutChanged(byte[] data)
        {
            Lock();
            bool different = false;
            for (int j = 0; j < elementSize; j++)
            {
                if (data[j] != ringBuffer[j + writePosition * elementSize])
                {
                    different = true;
                    break;
                }
            }
            if (different)
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
                Unlock();
                return true;
            }
            Unlock();
            return false;
        }

        /// <summary>
        /// Gets the current element from the ring buffer.
        /// </summary>
        public bool Get(byte[] data)
        {
            Lock();
            if ((!overflow) && (readPosition == writePosition))
            {
                Unlock();
                return false;
            }
            overflow = false;
            readPosition++;
            if (readPosition == elementCount)
                readPosition = 0;
            Array.Copy(ringBuffer, readPosition * elementSize, data, 0, elementSize);
            Unlock();
            return true;
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
            Lock();
            if (noData)
            {
                Unlock();
                return 2;
            }
            Array.Copy(ringBuffer, writePosition * elementSize, data, 0, elementSize);
            Unlock();
            return 0;
        }

        /// <summary>
        /// Clears and initializes the ring buffer. After a call
        /// to clear and before any calls to put() the GetLast
        /// will return 2 (no data yet) and Get will return 1 (no data).
        /// </summary>
        public void Clear()
        {
            Lock();
            writePosition = 0; //offset to put element
            readPosition = 0; //offset to get element
            noData = true; //flag for first time
            overflow = false; //flag for overflow
            Array.Clear(ringBuffer, 0, elementSize * elementCount);
            Unlock();
        }

        /// <summary>
        ///  Returns true if calling get() would result in the return of no data.
        /// </summary>
        public bool IsEmpty()
        {
            Lock();
            bool result = ((!overflow) && (readPosition == writePosition));
            Unlock();
            return result;
        }
    }
}


