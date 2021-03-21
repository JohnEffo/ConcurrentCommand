using Dispenser;
using FsCheck;
using System;
using System.Collections.Generic;
using System.Text;

namespace DispenserModel
{
    public class SerialSpec : ICommandGenerator<ITape, int>
    {
        private readonly Func<ITape> newTape;

        public SerialSpec(Func<ITape> newTape)
        {
            this.newTape = newTape;
        }
        public ITape InitialActual => newTape();
        public int InitialModel => 0;

        public Gen<Command<ITape, int>> Next(int value)
        {
            return Gen.Elements<Command<ITape, int>>(new GetTicket(), new Read());
        }

        public class GetTicket : Command<ITape, int>
        {
            private int _actual;
            public override ITape RunActual(ITape sut)
            {
                _actual = sut.GetTicket();
                return sut;
            }

            public override int RunModel(int model)
            {
                return model + 1; 
            }

            public override Property Post(ITape item, int model)
            {
                return (_actual == model).ToProperty().Label($"Model({model}) <> actual see below");
            }

            public override string ToString() => $"GetTicket ({_actual}))";


        }

        public class Read : Command<ITape, int>
        {
            private int _actual;
            public override ITape RunActual(ITape sut)
            {
                _actual = sut.Read();
                return sut;
            }

            public override int RunModel(int model)
            {
                return model;
            }

            public override Property Post(ITape sut, int model)
            {
                var local = _actual;
                return (_actual == model).ToProperty().Label($"Model({model}) <> actual see below");
            }

            public override string ToString() => $"Read ({_actual})";
        }

    }
}
