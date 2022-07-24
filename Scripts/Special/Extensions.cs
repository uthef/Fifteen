using Godot.Collections;

namespace Fifteen.Scripts.Special 
{
    public static class Conversion 
    {
        public static Array<Array<int>> ToGodotArray(this IBlock[,] array) {
            Array<Array<int>> result = new Array<Array<int>>();
            for (int i = 0; i < array.GetLength(0); i++) 
            {
                result.Add(new Array<int>());
                for (int j = 0; j < array.GetLength(1); j++) 
                {
                    result[i].Add(array[i, j].NumberValue);
                }
            }

            return result;
        }
    }
}