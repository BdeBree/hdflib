using System;
using System.Collections;

namespace hdflib
{
    /// <summary>
    /// Utility structure for determining the data type of an object. Supported objects are matrices, vecotors, lists and scalars. 
    /// </summary>
    public struct DataType
    {

        public int[] shape;

        public Type info;

        /// <summary>
        /// Creates an array of unsigned longs describing the dimensions of this object.
        /// </summary>
        /// <returns></returns>
        public ulong[] GetShape()
        {
            if (shape == null)
            {
                // a scalar or sigle string value
                return new ulong[] { (ulong) 1 };
            }
            ulong[] ret = new ulong[shape.Length];
            for (int i = 0; i < shape.Length; i++)
            {
                ret[i] = (ulong)shape[i];
            }
            return ret;
        }

        public bool IsScalar()
        {
            return shape == null;
        }

        public bool IsText()
        {
            return typeof(String).Equals(info);
        }

        public bool IsNumeric()
        {
            return !IsText();
        }



        public DataType(int[] shape, Type tinfo)
        {
            this.shape = shape;
            this.info = tinfo;
        }

        /// <summary>
        /// Determines the datatype for an object. Supported objects are matrices, vecotors, lists and scalars.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataType GetDataType(object data)
        {
            Type type = data.GetType();
            if (type.IsArray)
            {
                var elType = type.GetElementType();
                int rank = ((Array)data).Rank;
                int[] size = new int[rank];
                for (int i = 0; i < rank; i++)
                {
                    size[i] = ((Array)data).GetLength(i);
                }
                return new DataType(size, elType);
            }
            if (data is IList)
            {
                IList tmp = (IList)data;
                int len = tmp.Count;
                Type listType = type.GenericTypeArguments[0];
                return new DataType(new int[] { len }, listType);
            }
            return new DataType(null, type);
        }
    }

}
