namespace Dispenser
{
    public class TapeConcurrentLocking : ITape
    {
        private int _counter;
        private object _lock = new object();
        public int GetTicket()
        {
            lock (_lock)
            {
                var result = _counter;
                Write(result + 1);
                return result + 1;
            }
        }


        public int Read()
        {
            lock (_lock)
            {
                return _counter;
            }
        }

        private void Write(int newValue)
        {
            _counter = newValue;
        }

    }

}
