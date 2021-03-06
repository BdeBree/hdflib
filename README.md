# hdflib: Writing hdf5 datasets in .NET
[![Nuget](https://img.shields.io/nuget/v/hdflib)](https://www.nuget.org/packages/hdflib/)

hdflib is a lightweight library for writing HDF5 datasets in .NET, using the official [HDF.PInvoke](https://github.com/HDFGroup/HDF.PInvoke) library.

## Installation
Install the hdflib library using [nuget](https://www.nuget.org/packages/hdflib/).
The library comes with a reference to HDF.PInvoke and you're good to go.


## Usage

```csharp

            double scalar = 12.0;
            double [] vector = new double [] { 1, 2 };
            double [,] matrix = new double [,] { { 3, 4 }, { 5, 6 } };

            string [,] textMat = new string [,] { { "text", "is" }, { "also", "supported" } };
            using (HDFWriter w = new HDFWriter("test.h5"))
            {
                // put all examples in the group "example"
                w.CreateDataset("/example/scalar", scalar); // a single value
                w.CreateDataset("/example/vector", vector); // an array or vector
                w.CreateDataset("/example/matrix", matrix); // a 2d matrix 
                w.CreateDataset("/example/txt_mat", textMat); // a 2d text matrix
            }

```

Now you can read the file called "test.h5" using your favourite hdf package.

## Reading the data in another language (Python)

```python
import numpy as np
import h5py

if __name__ == '__main__':
  exp = ["/example/scalar", "/example/vector", "/example/matrix", "/example/txt_mat"]
  with h5py.File('test.h5', 'r') as f:
    for k in exp:
      print((k, np.array(f[k]))

```

## Reading it with Matlab
```Malab
fname = 'test.h5';
h5disp(fname);
h5read(fname, '/example/matrix')
```

That's all!
