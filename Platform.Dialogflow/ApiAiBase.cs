using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow
{
    public class ApiAiBase
    {
        protected float[] TrimSilence(float[] samples)
        {
            if (samples == null)
            {
                return null;
            }

            const float min = 0.000001f;

            var startIndex = 0;
            var endIndex = samples.Length;

            for (var i = 0; i < samples.Length; i++)
            {

                if (Math.Abs(samples[i]) > min)
                {
                    startIndex = i;
                    break;
                }
            }

            for (var i = samples.Length - 1; i > 0; i--)
            {
                if (Math.Abs(samples[i]) > min)
                {
                    endIndex = i;
                    break;
                }
            }

            if (endIndex <= startIndex)
            {
                return null;
            }

            var result = new float[endIndex - startIndex];
            Array.Copy(samples, startIndex, result, 0, endIndex - startIndex);
            return result;

        }

        protected static byte[] ConvertArrayShortToBytes(short[] array)
        {
            var numArray = new byte[array.Length * 2];
            Buffer.BlockCopy(array, 0, numArray, 0, numArray.Length);
            return numArray;
        }

        protected static short[] ConvertIeeeToPcm16(float[] source)
        {
            var resultBuffer = new short[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var f = source[i] * 32768f;

                if (f > (double)short.MaxValue)
                    f = short.MaxValue;
                else if (f < (double)short.MinValue)
                    f = short.MinValue;
                resultBuffer[i] = Convert.ToInt16(f);
            }

            return resultBuffer;
        }
    }
}
