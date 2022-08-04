using Godot;
using System;
using System.Collections.Generic;

namespace Fifteen.Models
{
    public class FifteenModel
    {
        public readonly int Width, Height;
        public readonly int Length;
        private int[,] _numbers;

        public FifteenModel(int width, int height)
        {
            Width = width;
            Height = height;
            Length = width * height;
            _numbers = new int[height, width];
            Populate();
        }

        public FifteenModel(int[,] numbers)
        {
            Width = numbers.GetLength(1);
            Height = numbers.GetLength(0);
            Length = Width * Height;
            _numbers = numbers.Clone() as int[,];
        }

        public int GetNumber(int i, int j)
        {
            return _numbers[i, j];
        }

        public int[,] AsArray()
        {
            return _numbers.Clone() as int[,];
        }

        private void Populate()
        {
            List<int> numbers = new List<int>();
            Random random = new Random();
            int[] plainArray = new int[Length];
            int emptyValueRow = 0, lastCount = Length;

            for (int i = 1; i <= Length; i++) numbers.Add(i);

            for (int i = 0, count = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++, count++)
                {
                    var randIndex = random.Next(numbers.Count);
                    if (numbers[randIndex] == lastCount)
                    {
                        emptyValueRow = i;
                        _numbers[i, j] = numbers[randIndex];
                        plainArray[count] = 0;

                    }
                    else plainArray[count] = _numbers[i, j] = numbers[randIndex];

                    numbers.RemoveAt(randIndex);
                }
            }

            int inversions = 0;

            for (int i = 0; i < plainArray.Length; i++)
            {
                if (plainArray[i] == 0) continue;
                for (int j = 0; j < i; j++)
                {
                    if (plainArray[j] > plainArray[i]) inversions++;
                }
            }

            if (Width % 2 == 0)
            {
                inversions += emptyValueRow + 1;
                if (Height % 2 != 0) inversions++;
            }

            if (inversions % 2 != 0)
            {
                var row = emptyValueRow > 0 ? emptyValueRow - 1 : emptyValueRow + 1;
                var column = random.Next(Width);
                var nextColumn = column > 0 ? column - 1 : column + 1;

                (_numbers[row, column], _numbers[row, nextColumn]) = (_numbers[row, nextColumn], _numbers[row, column]);
            }
        }

        public Vector2 TryToMove(int i, int j)
        {
            if (i + 1 < _numbers.GetLength(0) && _numbers[i + 1, j] == Length)
            {
                (_numbers[i + 1, j], _numbers[i, j]) = (_numbers[i, j], _numbers[i + 1, j]);
                return Vector2.Down;
            }
            else if (i - 1 >= 0 && _numbers[i - 1, j] == Length)
            {
                (_numbers[i - 1, j], _numbers[i, j]) = (_numbers[i, j], _numbers[i - 1, j]);
                return Vector2.Up;
            }
            else if (j + 1 < _numbers.GetLength(1) && _numbers[i, j + 1] == Length)
            {
                (_numbers[i, j + 1], _numbers[i, j]) = (_numbers[i, j], _numbers[i, j + 1]);
                return Vector2.Right;
            }
            else if (j - 1 >= 0 && _numbers[i, j - 1] == Length)
            {
                (_numbers[i, j - 1], _numbers[i, j]) = (_numbers[i, j], _numbers[i, j - 1]);
                return Vector2.Left;
            }
            else return Vector2.Zero;
        }

        public bool IsOrderCorrect()
        {
            for (int i = 0, count = 1; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    if (_numbers[i, j] != count++)
                        return false;
                }
            }

            return true;
        }
    }
}
