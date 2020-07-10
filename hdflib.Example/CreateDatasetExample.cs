namespace hdflib.Example
{
    class CreateDatasetExample
    {
        static void Main(string[] args)
        {
            double scalar = 12.0;
            double[] vector = new double[] { 1, 2 };
            double [,] matrix = new double[,] { { 3, 4 }, { 5, 6 } };

            string[,] textMat = new string[,] { { "text", "is" }, { "also", "supported" } };
            using (HDFWriter w = new HDFWriter("test.h5"))
            {
                // put all examples in the group "example"
                w.CreateDataset("/example/scalar", scalar); // a single value
                w.CreateDataset("/example/vector", vector); // an array or vector
                w.CreateDataset("/example/matrix", matrix); // a 2d matrix 
                w.CreateDataset("/example/txt_mat", textMat); // a 2d text matrix
            }
        }
    }
}
