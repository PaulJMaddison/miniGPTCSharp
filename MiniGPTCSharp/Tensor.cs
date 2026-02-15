namespace MiniGPTCSharp;

public class Tensor
{
    public Tensor(int rows, int columns)
    {
        Data = new float[rows, columns];
    }

    public Tensor(float[,] data)
    {
        Data = data;
    }

    public float[,] Data { get; }

    public int Rows => Data.GetLength(0);

    public int Columns => Data.GetLength(1);

    public string Shape => $"[{Rows}, {Columns}]";

    public float this[int row, int column]
    {
        get => Data[row, column];
        set => Data[row, column] = value;
    }

    public Tensor Clone()
    {
        var copy = new float[Rows, Columns];
        Array.Copy(Data, copy, Data.Length);
        return new Tensor(copy);
    }

    public override string ToString() => Shape;
}
