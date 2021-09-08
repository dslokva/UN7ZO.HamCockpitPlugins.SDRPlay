# HamCockpit SDRPlay RSP2 Adapter plugin

First pre-release.

You need SDRPlay API 3.06 or 3.07 installed first.

Supported modes:  Zero-IF   8 Msps         1.536 Mhz IF BW 
                  Low-IF    2, 6, 8 Msps   0.2, 0.6, 1.536 Mhz IF BW
                  
From Settings or UI panel you can switcha antenna ports, notch filter, set LNA Gain reduce parameter.
Overload event detection is in place and have separate label.

Tuner AGC is hardcoded and set to "always on".

![image](https://user-images.githubusercontent.com/13137490/132553310-50504966-2d38-4942-8929-21cde9df26c9.png)


![image](https://user-images.githubusercontent.com/13137490/132552550-23b1aab7-681a-4773-8e92-f89b78decf1e.png)



***preileminary information!****

If you use old version of DSPFun dll code, you are probably missing writeShort method in RingBuffer.cs
Just create it. Almost the same as WriteInt16 :)

```
        public void WriteShort(short[] buffer, int byteCount) {
            int sampleCount = byteCount;
            if (this.buffer == null || this.buffer.Length < sampleCount)
                this.buffer = new float[sampleCount];

            fixed (short* pInBuffer = buffer)
            fixed (float* pOutBuffer = this.buffer)
                sp.ippsConvert_16s32f_Sfs(pInBuffer, pOutBuffer, sampleCount, 15);

            Write(this.buffer, 0, sampleCount);
        }
```
