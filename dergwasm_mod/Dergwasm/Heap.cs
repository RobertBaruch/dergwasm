﻿using System;
using System.Collections.Generic;

namespace Derg
{
    // The heap is Memory 0. This class contains utilities for interacting with it.
    public class Heap
    {
        Machine machine;

        public Heap(Machine machine)
        {
            this.machine = machine;
        }

        public int IntAt(int offset)
        {
            return BitConverter.ToInt32(machine.Heap, offset);
        }

        public void SetIntAt(int offset, int val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Array.Copy(bytes, 0, machine.Heap, offset, bytes.Length);
        }

        public byte ByteAt(int offset)
        {
            return machine.Heap[offset];
        }

        public void SetByteAt(int offset, byte val) => machine.Heap[offset] = val;
    }
}
