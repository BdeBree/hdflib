using System;
using System.Runtime.InteropServices;
using System.Text;

using System.Collections;
using System.Collections.Generic;

using HDF.PInvoke;


#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif

namespace hdflib
{
    /// <summary>
    /// Class for writing datasets to HDF5 files. 
    /// </summary>
    public class HDFWriter : IDisposable
    {
        private readonly string filename;
        private long h5FileId = -1;

        public const char PATH_SEP = '/';

        
        /// <summary>
        /// Constructor for creating a HDFWriter based on a filename.
        /// </summary>
        /// <param name="filename"></param>
        public HDFWriter(string filename)
        {
            this.filename = filename;
            // open handle
            this.h5FileId = H5F.create(filename, H5F.ACC_TRUNC);
        }

        /// <summary>
        /// Closes the HDF file handle if still open and resets the handle.
        /// </summary>
        public void Close()
        {
            if (this.h5FileId > -1)
            {
                this.h5FileId = H5F.close(this.h5FileId);
                this.h5FileId = -1;
            }
        }

        /// <summary>
        /// Disposable supprt, for automatically closing the file handle when out of scope
        /// </summary>
        public void Dispose()
        {
            Close();
        }


        /// <summary>
        /// Returns the groupId for a given path. Groups are created where needed.
        /// </summary>
        /// <param name="path">Path string to a variable. seperated with / </param>
        /// <returns></returns>
        private long PathToGroupId(string path)
        {
            string[] groups = path.Split(PATH_SEP);
            long groupId = this.h5FileId;
            long parentId = this.h5FileId;
            for (int i = 0; i < groups.Length - 1; i++)
            {
                groupId = CreateGroup(groupId, groups[i]);
                CloseGroup(parentId); // free handle to parent
                parentId = groupId;

            }
            return groupId;
        }

        /// <summary>
        /// frees a group handle, if the group is different from the file handle.
        /// </summary>
        /// <param name="groupId"></param>
        private void CloseGroup(long groupId)
        {
            if (groupId != this.h5FileId)
            {
                H5G.close(groupId);
            }
        }

        /// <summary>
        /// Creates a group if it does not exists and returns the handle id.
        /// </summary>
        /// <param name="groupId">parent group id. Note, fileId is the root group.</param>
        /// <param name="name">name of the group to create</param>
        /// <returns></returns>
        private long CreateGroup(long groupId, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return groupId;
            }
            // check if group exists
            if (H5L.exists(groupId, name) > 0)
            {
                return H5G.open(groupId, name);
            }
            return H5G.create(groupId, name);
        }

        /// <summary>
        /// checks if a type is supported via the TypeMap table.
        /// </summary>
        /// <param name="dataType">type to check</param>
        /// <returns></returns>
        public static bool IsSupported(Type dataType)
        {
            return TypeMap.ContainsKey(Type.GetTypeCode(dataType));
        }

        /// <summary>
        /// Type mapping from .Net types to HDF5 native types.
        /// </summary>
        public static Dictionary<TypeCode, long> TypeMap = new Dictionary<TypeCode, long> {
                                            {TypeCode.Int16, H5T.NATIVE_INT16 },
                                            {TypeCode.Int32,  H5T.NATIVE_INT32},
                                            {TypeCode.Int64,  H5T.NATIVE_INT64},
                                            {TypeCode.UInt16, H5T.NATIVE_UINT16},
                                            {TypeCode.UInt32, H5T.NATIVE_UINT32},
                                            {TypeCode.UInt64, H5T.NATIVE_UINT64},
                                            {TypeCode.Single, H5T.NATIVE_FLOAT},
                                            {TypeCode.Double, H5T.NATIVE_DOUBLE},
                                            {TypeCode.Boolean, H5T.NATIVE_HBOOL},
                                            {TypeCode.Char, H5T.C_S1},
                                            {TypeCode.String, GetStringType()}};

        /// <summary>
        /// Creates the type for UTF8 strings.
        /// </summary>
        /// <returns></returns>
        private static long GetStringType()
        {

            long dtype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(dtype, H5T.cset_t.UTF8);
            H5T.set_strpad(dtype, H5T.str_t.SPACEPAD);
            return dtype;
        }

        /// <summary>
        /// Writes a piece of data to the open hdf file based on the specified path.
        /// 
        /// i.e. the following method calls will result in a scalar, vector and matrix being written to the group "examples".
        /// <code>
        ///     CreateDataset("/examples/scalar", 5.2);
        ///     CreateDataset("/examples/vector", new double[]{1, 2, 3});
        ///     CreateDataset("/examples/matrix", new double[,]{{1, 2}, {3, 4}});
        /// </code>
        /// </summary>
        /// <param name="path">Path to the variable, separated with / </param>
        /// <param name="data">data to be written.</param>
        public void CreateDataset(string path, object data)
        {
            // types to support:
            // scalar, vector, matrix, list; double, int, long, string
            DataType dataType = DataType.GetDataType(data);

            if (!IsSupported(dataType.info))
            {
                throw new NotImplementedException($"Data type {dataType.info} is not supported by this library");
            }

            string[] groups = path.Split(PATH_SEP);
            string name = groups[groups.Length - 1];
            long groupId = PathToGroupId(path);



            /* Define memory space */
            ulong[] shape = dataType.GetShape();
            if (dataType.IsScalar() && dataType.IsText())
                data = new string[] { (string)data }; // wrap single string into an iterable object

            long dspace = H5S.create_simple(shape.Length, shape, null);
            // lookup the HD5 type code.
            long ctype = TypeMap[Type.GetTypeCode(dataType.info)];

            long dtype = H5T.copy(ctype);
            // geen properties
            long dset = H5D.create(groupId, name, dtype, dspace);

            var filespaceId = H5D.get_space(dset);
            /* Write the data to the extended portion of dataset  */

            GCHandle hnd;
            if (dataType.IsNumeric())
            {
                hnd = GCHandle.Alloc(data, GCHandleType.Pinned);
            }
            else
            {
                GCHandle[] handles = ToMem((IList)data);
                IntPtr[] wdata = HandleToAddress(handles);
                hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
                ReleaseHandles(handles);
            }

            // GCHandle hnd = GCHandle.Alloc(data, GCHandleType.Pinned);
            H5D.write(dset, dtype, dspace, filespaceId, H5P.DEFAULT, hnd.AddrOfPinnedObject());


            // cleanup mem and handles
            hnd.Free();
            CloseGroup(groupId);
            H5D.close(dset);
            H5S.close(dspace);
            H5T.close(dtype);

        }


      
        /// <summary>
        /// Utility function for allocating memory for a string array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private GCHandle[] ToMem(IList data)
        {
            GCHandle[] hnds = new GCHandle[data.Count];
            int i = 0;
            foreach (String el in data)
            {
                if (el != null)
                {
                    hnds[i] = GCHandle.Alloc(Encoding.UTF8.GetBytes(el), GCHandleType.Pinned);
                }
                i++;
            }
            return hnds;
        }
        /// <summary>
        /// Utility function to collect the memory addresses for a set of GC handles.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private IntPtr[] HandleToAddress(GCHandle[] data)
        {
            IntPtr[] addr = new IntPtr[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].IsAllocated)
                {
                    addr[i] = data[i].AddrOfPinnedObject();
                }
            }
            return addr;
        }

        /// <summary>
        /// Utility function to free memory
        /// </summary>
        /// <param name="data"></param>
        private void ReleaseHandles(GCHandle[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].IsAllocated)
                {
                    data[i].Free();
                }
            }
        }       
    }
}
