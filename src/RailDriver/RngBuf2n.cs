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
    internal class RingBuffer
    {
        private readonly int N;
        private readonly int M;
        private int i;
        private int j;
        private int fl;
        private int f;
        private readonly byte[] pB;

        private void Jlock()
        {
            Monitor.Enter(pB);
            return;
        }

        private void Junlock()
        {
            Monitor.Exit(pB);
            return;
        }

        public RingBuffer(int nn, int mm)
        {

            N = nn; 
            M = mm;
            i = 0; 
            j = 0;
            f = 0; 
            fl = 0;
            pB = new byte[N * M];
            Monitor.Enter(pB);
            Monitor.Exit(pB);
        }


        public int PutIfCan(byte[] pData)
        {
            Jlock();
            if (fl == 1) 
            { 
                Junlock(); 
                return 3; 
            }
            i++; 
            if (i == N) 
                i = 0;
            if (i == j) 
                fl = 1;
            Array.Copy(pData, 0, pB, M * i, M);
            f = 1;
            Junlock();
            return 0;
        }

        public void Put(byte[] pData)
        {
            Jlock();
            i++; 
            if (i == N) 
                i = 0;
            if (fl == 1) 
            { 
                j++; 
                if (j == N) 
                    j = 0; 
            }
            if (i == j) 
                fl = 1;
            Array.Copy(pData, 0, pB, M * i, M);
            f = 1;
            Junlock();
        }
        public int PutIfDiff(byte[] pData)
        {
            int ret = 1;
            Jlock();
            byte[] temp = new byte[M];
            Array.Copy(pData, 0, temp, 0, M);
            bool Diff = false;
            for (int j = 0; j < M; j++)
            {
                if (temp[j] != pB[j + i * M])
                {
                    Diff = true;
                    break;
                }
            }
            if (Diff)
            {
                i++; 
                if (i == N) 
                    i = 0;
                if (fl == 1) 
                { 
                    j++; 
                    if (j == N) 
                        j = 0; 
                }
                if (i == j) 
                    fl = 1;
                Array.Copy(pData, 0, pB, M * i, M);
                f = 1;
                ret = 0;
            }
            Junlock();
            return ret;
        }
        public int Get(byte[] pS)
        {
            Jlock();
            if ((fl == 0) && (j == i)) 
            { 
                Junlock(); 
                return 1; 
            }
            fl = 0;
            j++; 
            if (j == N) 
                j = 0;
            Array.Copy(pB, j * M, pS, 0, M);
            Junlock();
            return 0;
        }

        public int Getlast(byte[] pS)
        {
            Jlock();
            if (f == 0) 
            { 
                Junlock(); 
                return 2; 
            }
            //  memcpy(pS, pB + i * M, M);
            Array.Copy(pB, i * M, pS, 0, M);
            Junlock();
            return 0;
        }

        public void Clear()
        {
            Jlock();
            i = 0; //offset to put element
            j = 0; //offset to get element
            f = 0; //flag for first time
            fl = 0; //flag for overflow
            Array.Clear(pB, 0, M * N);
            Junlock();
        }
        public bool IsEmpty()
        {
            Jlock();
            bool a;
            a = ((fl == 0) && (j == i));
            Junlock();
            return a;
        }
    }
}


