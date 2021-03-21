using System.Threading;
using System.Threading.Tasks;

namespace Dispenser
{
    public class TapeConcurrentBug : ITape
    {
        private int _counter;

        public int GetTicket()
        {
            var result = _counter;
             Write(result + 1);
            return result + 1;
        }


        public int Read()
        {
            //Thread.Sleep(0);
            return _counter;
        }

        private void Write(int newValue)
        {
            //Thread.Sleep(0);
            _counter = newValue;
        }
    }

}
