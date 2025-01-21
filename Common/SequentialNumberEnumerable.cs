using System;
using System.Collections;

// implement a class which enumerates sequential numbers

public class SequentialNumberEnumerable : IEnumerable<int>
{
    private readonly int _min;
    private readonly int _max;
    private readonly int _step;

    public SequentialNumberEnumerable(int min, int max, int step)
    {
        if (step <= 0)
            throw new ArgumentException("Step must be greater than zero", nameof(step));
        if (min > max)
            throw new ArgumentException("Min value cannot be greater than max", nameof(min));

        _min = min;
        _max = max;
        _step = step;
    }

    public IEnumerator<int> GetEnumerator()
    {
        return new SequentialNumberEnumerator(_min, _max, _step);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class SequentialNumberEnumerator : IEnumerator<int>
    {
        public SequentialNumberEnumerator(int min = 0, int max = 10, int step = 1)
        {
            Min = min;
            Max = max;
            Step = step;

            _current = min;
        }

        public readonly int Min;
        public readonly int Max;
        public readonly int Step;

        private int _current;

        public int Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _current += Step;
            return true;
        }

        public void Reset()
        {
            _current = Min;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}