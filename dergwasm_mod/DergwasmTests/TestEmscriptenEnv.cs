﻿using Derg;

namespace DergwasmTests
{
    public class TestEmscriptenEnv : EmscriptenEnv
    {
        int ptr = 4;

        public TestEmscriptenEnv()
            : base(new TestMachine()) { }

        public void ResetMalloc()
        {
            ptr = 4;
        }

        public override int Malloc(Frame frame, int size)
        {
            int result = ptr;
            ptr += size;
            return result;
        }
    }
}
