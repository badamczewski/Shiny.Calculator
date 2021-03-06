using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiny.Calculator.Evaluation
{
    public unsafe class Heap
    {
        public Heap(int size)
        {
            Memory = new byte[size];
        }

        public int Read(int offset)
        {
            fixed (byte* i = &Memory[offset])
            {
                return *(int*)i;
            }
        }

        public void Write(int value, int offset)
        {
            fixed (byte* i = &Memory[offset])
            {
                var target = (int*)i;
                *target = value;
            }
        }

        public byte[] Memory { get; set; }
    }
}
