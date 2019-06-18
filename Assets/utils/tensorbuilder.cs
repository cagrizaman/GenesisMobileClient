using System;
using System.Drawing;
using System.IO;
using UnityEngine;
using Tensorflow;

public class TensorBuilder
{
    public static TensorProto CreateTensor(float value)
    {
        var tensor = new TensorProto();
        tensor.FloatVal.Add(value);
        tensor.TensorShape = new TensorShapeProto();
        tensor.Dtype = DataType.DtFloat;
        var dim = new TensorShapeProto.Types.Dim();
        dim.Size = 1;
        tensor.TensorShape.Dim.Add(dim);

        return tensor;
    }

    public static TensorProto CreateTensorFromImage(Color32[] pixels, int height, int width, float revertsBits = 1.0f)
    {

        return CreateTensorFromImage(pixels, revertsBits, height, width, 3);

    }

    public static TensorProto CreateTensorFromImage(Color32[] pixels, float revertsBits, int height, int width, int channels)
    {
        var imageFeatureShape = new TensorShapeProto();

        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = 1 });
        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = height });
        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = width });
        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = channels });

        var imageTensorBuilder = new TensorProto();
        imageTensorBuilder.Dtype = DataType.DtFloat;
        imageTensorBuilder.TensorShape = imageFeatureShape;

        var px = 0;
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                var color = pixels[px++];
                imageTensorBuilder.FloatVal.Add(color.r/revertsBits);
                imageTensorBuilder.FloatVal.Add(color.g / revertsBits);
                imageTensorBuilder.FloatVal.Add(color.b / revertsBits);
            }
        }

        return imageTensorBuilder;
    }


    public static TensorProto CreateTensorFromImage(int[][][] dimArray, float revertsBits, int height, int width, int channels)
    {
        var imageFeatureShape = new TensorShapeProto();

        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = 1 });
        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = height });
        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = width });
        imageFeatureShape.Dim.Add(new TensorShapeProto.Types.Dim() { Size = channels });

        var imageTensorBuilder = new TensorProto();
        imageTensorBuilder.Dtype = DataType.DtFloat;
        imageTensorBuilder.TensorShape = imageFeatureShape;

        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                for (int c = 0; c < channels; c++)
                {
                    imageTensorBuilder.FloatVal.Add(dimArray[i][j][c] / revertsBits);
                }
            }
        }

        return imageTensorBuilder;
    }

    //public static Bitmap CreateImageBitmapFromTensor(TensorProto imageTensor, float revertsBits = 1.0f)
    //{
    //    var imageData = CreateImageFromTensor(imageTensor, revertsBits);
    //    return ImageUtils.ConvertDimArraysToImageBitmap(imageData, imageTensor.FloatVal.Count, (int)imageTensor.TensorShape.Dim[1].Size, (int)imageTensor.TensorShape.Dim[2].Size);
    //}

    //public static int[][][] CreateImageFromTensor(TensorProto imageTensor, float revertsBits)
    //{
    //    var t = 0;
    //    var imageFeatureShape = imageTensor.TensorShape;
    //    var imageData = new int[imageFeatureShape.Dim[1].Size][][];
    //    for (int i = 0; i < imageFeatureShape.Dim[1].Size; i++)
    //    {
    //        imageData[i] = new int[imageFeatureShape.Dim[2].Size][];
    //        for (int j = 0; j < imageFeatureShape.Dim[2].Size; j++)
    //        {
    //            imageData[i][j] = new int[imageFeatureShape.Dim[3].Size];
    //            for (int c = 0; c < imageFeatureShape.Dim[3].Size; c++)
    //            {
    //                imageData[i][j][c] = (int)(imageTensor.FloatVal[t] * revertsBits);
    //                t++;
    //            }
    //        }
    //    }
    //    // Console.WriteLine("Output shape: {0} / {1}", imageFeatureShape, t);
    //    return imageData;
    //}
}
