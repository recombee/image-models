using System;
using System.Collections.Generic;

namespace DeepRecommender
{
    class SparseVector
    {
        [ThreadStatic]
        private static List<(int, int)> indexBuffer;

        private static List<(int, int)> Buffer
        {
            get
            {
                if(indexBuffer == null)
                    indexBuffer = new List<(int, int)>();

                return indexBuffer;
            }
        }

        public readonly float Length;

        public readonly float[] Data;
        public readonly int[] Indices;

        public SparseVector(float[] data, int[] indices)
        {
            this.Data = data;
            this.Indices = indices;

            double sum = 0;
            for(var i = 0; i < data.Length; i++)
            {
                var val = data[i];
                checked
                {
                    sum += val * val;
                }
            }

            this.Length = (float)Math.Sqrt(sum);
        }

        //TODO: Rename
        public static float CosSimmilarity(SparseVector a, SparseVector b)
        {
            double sum = 0;
            ForCommonIndices(a, b, (x1, x2) => { sum += x1 * x2; });

            if(sum == 0)
                return 0;

            return (float)(sum / (a.Length * b.Length));
        }

        private static void ForCommonIndices(SparseVector a, SparseVector b, Action<float, float> action)
        {
            var indicesA = a.Indices;
            var indicesB = b.Indices;

            int iA = 0, iB = 0;
            var aLen = indicesA.Length;
            var bLen = indicesB.Length;

            while(iA < aLen && iB < bLen)
            {
                var valA = indicesA[iA];
                var valB = indicesB[iB];

                if(valA < valB)
                    iA++;
                else if(valB < valA)
                    iB++;
                else
                {
                    action(a.Data[iA], b.Data[iB]);

                    iA++;
                    iB++;
                }
            }
        }

        public static SparseVector FromArray(float[] data)
        {
            var indices = new List<int>();
            var buffer = new List<float>();

            for(int i = 0; i < data.Length; i++)
            {
                var val = data[i];
                if(val != 0)
                {
                    indices.Add(i);
                    buffer.Add(val);
                }
            }

            return new SparseVector(buffer.ToArray(), indices.ToArray());
        }
    }
}