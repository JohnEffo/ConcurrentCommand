using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dispenser
{
    public class TapeSerialBug : ITape
    {
        private int _counter;

        public int GetTicket()
        {
            Write( (_counter +1) % 5 );
            return _counter;
        }

        public int Read()
        {
            return _counter;
        }

        private void Write(int newValue)
        {
            _counter = newValue;
        }
        
    }

}
