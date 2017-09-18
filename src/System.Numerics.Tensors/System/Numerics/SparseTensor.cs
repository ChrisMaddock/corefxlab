﻿using System.Collections.Generic;
using System.Diagnostics;

namespace System.Numerics
{
    public class SparseTensor<T> : Tensor<T>
    {
        internal readonly Dictionary<int, T> values;

        public SparseTensor(int[] dimensions, bool reverseStride = false, int capacity = 0) : base(dimensions, reverseStride)
        {
            values = new Dictionary<int, T>(capacity);
        }

        internal SparseTensor(Dictionary<int, T> values, int[] dimensions, bool reverseStride = false) : base(dimensions, reverseStride)
        {
            this.values = values;
        }

        internal SparseTensor(Array fromArray, bool reverseStride = false) : base(GetDimensionsFromArray(fromArray), reverseStride)
        {
            values = new Dictionary<int, T>(fromArray.Length);

            int index = 0;
            if (reverseStride)
            {
                // Array is always row-major
                var sourceStrides = ArrayUtilities.GetStrides(dimensions);

                foreach (T item in fromArray)
                {
                    if (!item.Equals(arithmetic.Zero))
                    {
                        var destIndex = ArrayUtilities.TransformIndexByStrides(index, sourceStrides, false, strides);
                        values[destIndex] = item;
                    }

                    index++;
                }
            }
            else
            {
                foreach (T item in fromArray)
                {
                    if (!item.Equals(arithmetic.Zero))
                    {
                        values[index] = item;
                    }

                    index++;
                }
            }
        }

        public override T this[Span<int> indices]
        {
            get
            {
                var index = ArrayUtilities.GetIndex(strides, indices);

                T value;

                if (!values.TryGetValue(index, out value))
                {
                    value = arithmetic.Zero;
                }

                return value;
            }

            set
            {
                var index = ArrayUtilities.GetIndex(strides, indices);

                if (value.Equals(arithmetic.Zero))
                {
                    values.Remove(index);
                }
                else
                {
                    values[index] = value;
                }
            }
        }

        public int NonZeroCount => values.Count;

        private static int[] GetDimensionsFromArray(Array fromArray)
        {
            if (fromArray == null)
            {
                throw new ArgumentNullException(nameof(fromArray));
            }

            var dimensions = new int[fromArray.Rank];
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = fromArray.GetLength(i);
            }

            return dimensions;
        }

        public override Tensor<T> Clone()
        {
            var valueCopy = new Dictionary<int, T>(values);
            return new SparseTensor<T>(valueCopy, dimensions, IsReversedStride);
        }

        public override Tensor<TResult> CloneEmpty<TResult>(int[] dimensions)
        {
            return new SparseTensor<TResult>(dimensions, IsReversedStride);
        }

        public override Tensor<T> Reshape(params int[] dimensions)
        {
            return new SparseTensor<T>(values, dimensions, IsReversedStride);
        }

        public override DenseTensor<T> ToDenseTensor()
        {
            var denseTensor = new DenseTensor<T>(dimensions, reverseStride: IsReversedStride);
            
            // only set non-zero values
            foreach (var pair in values)
            {
                Debug.Assert(pair.Key < denseTensor.Buffer.Length);
                denseTensor.Buffer[pair.Key] = pair.Value;
            }

            return denseTensor;
        }

        public override SparseTensor<T> ToSparseTensor()
        {
            var valueCopy = new Dictionary<int, T>(values);
            return new SparseTensor<T>(valueCopy, dimensions, IsReversedStride);
        }

        public override CompressedSparseTensor<T> ToCompressedSparseTensor()
        {
            var compressedSparseTensor = new CompressedSparseTensor<T>(dimensions, capacity: NonZeroCount, reverseStride: IsReversedStride);

            Span<int> indices = new Span<int>(new int[Rank]);
            foreach (var pair in values)
            {
                ArrayUtilities.GetIndices(strides, IsReversedStride, pair.Key, indices);
                compressedSparseTensor[indices] = pair.Value;
            }
            return compressedSparseTensor;
        }
    }
}
